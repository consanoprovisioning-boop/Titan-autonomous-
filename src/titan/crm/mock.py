from __future__ import annotations

import copy
import itertools
import threading
from dataclasses import dataclass, field, replace
from datetime import date, datetime
from typing import Callable

from titan.crm.base import (
    CrmTask,
    DashboardRow,
    DuplicateTaskWarning,
    EleadsAdapter,
    ProfileSession,
    SendReceipt,
    WorkItem,
)
from titan.models import Activity, ActivityKind, Channel, CustomerProfile, TaskKind

_ids = itertools.count(1)


def _next(prefix: str) -> str:
    return f"{prefix}-{next(_ids)}"


@dataclass
class MockRecord:
    profile: CustomerProfile
    tasks: dict[str, CrmTask] = field(default_factory=dict)
    progress: str = "Open"
    appointments: list[tuple[datetime, str, str]] = field(default_factory=list)
    printed: int = 0
    is_new_lead: bool = False
    is_past_customer: bool = False


class MockCrm:
    """Shared in-memory 'Eleads' used for dry runs, training and tests."""

    def __init__(self, user_name: str = "Bordine, Glenn", now: Callable[[], datetime] | None = None):
        self.user_name = user_name
        self.records: dict[tuple[str, str], MockRecord] = {}
        self.dashboard: list[DashboardRow] = []
        self.sent: list[tuple[str, Channel, str]] = []
        self.lock = threading.RLock()
        self.now = now or datetime.now
        self.drop_send_activity: set[str] = set()
        self.drop_dashboard: set[str] = set()
        self.max_concurrent_tabs = 0

    def add(self, profile: CustomerProfile, *, new_lead: bool = False, past: bool = False,
            tasks: list[CrmTask] | None = None) -> MockRecord:
        rec = MockRecord(profile, is_new_lead=new_lead, is_past_customer=past)
        for t in tasks or []:
            rec.tasks[t.crm_task_id] = t
        self.records[(profile.rooftop_id, profile.customer_id)] = rec
        return rec

    def record(self, rooftop_id: str, customer_id: str) -> MockRecord:
        return self.records[(rooftop_id, customer_id)]


class MockProfileSession(ProfileSession):
    def __init__(self, adapter: "MockEleadsAdapter", rec: MockRecord):
        self.adapter = adapter
        self.crm = adapter.crm
        self.rec = rec
        self.closed = False

    def _guard(self) -> None:
        if self.closed:
            raise RuntimeError("Profile session already closed")

    def read_profile(self) -> CustomerProfile:
        self._guard()
        return copy.deepcopy(self.rec.profile)

    def refresh_activities(self) -> list[Activity]:
        self._guard()
        return list(self.rec.profile.activities)

    def _send(self, channel: Channel, recipient: str, summary: str) -> SendReceipt:
        self._guard()
        with self.crm.lock:
            self.crm.sent.append((self.rec.profile.customer_id, channel, recipient))
            if self.rec.profile.customer_id in self.crm.drop_send_activity:
                return SendReceipt(True, "Processing...")
            kind = ActivityKind.EMAIL_OUT if channel is Channel.EMAIL else ActivityKind.SMS_OUT
            self.rec.profile.activities.append(
                Activity(kind, self.crm.now(), self.crm.user_name, summary, _next("act"))
            )
        return SendReceipt(True, "Sent")

    def send_email(self, subject: str, body: str, recipient: str) -> SendReceipt:
        return self._send(Channel.EMAIL, recipient, subject)

    def send_sms(self, body: str, recipient: str) -> SendReceipt:
        return self._send(Channel.SMS, recipient, body[:60])

    def open_tasks(self) -> list[CrmTask]:
        self._guard()
        return [t for t in self.rec.tasks.values() if t.status == "open"]

    def create_task(self, kind: TaskKind | Channel, due_at: datetime, note: str) -> CrmTask:
        self._guard()
        for t in self.open_tasks():
            if t.kind == kind.value:
                raise DuplicateTaskWarning(t)
        task = CrmTask(_next("task"), kind.value, "open", self.crm.user_name, due_at, note)
        self.rec.tasks[task.crm_task_id] = task
        return task

    def complete_task(self, crm_task_id: str, outcome_note: str) -> None:
        self._guard()
        with self.crm.lock:
            task = self.rec.tasks[crm_task_id]
            self.rec.tasks[crm_task_id] = replace(task, status="completed", note=outcome_note)
            self.rec.profile.activities.append(
                Activity(ActivityKind.NOTE, self.crm.now(), self.crm.user_name, outcome_note, _next("act"))
            )
            if self.rec.profile.customer_id not in self.crm.drop_dashboard:
                self.crm.dashboard.append(DashboardRow(
                    self.rec.profile.rooftop_id, self.crm.user_name, task.kind,
                    self.rec.profile.customer_id, crm_task_id, self.crm.now(),
                ))

    def task_status(self, crm_task_id: str) -> CrmTask:
        self._guard()
        return self.rec.tasks[crm_task_id]

    def add_note(self, text: str) -> None:
        self._guard()
        self.rec.profile.notes.append(text)

    def create_sales_appointment(self, starts_at: datetime, assigned_to: str, notes: str) -> str:
        self._guard()
        self.rec.appointments.append((starts_at, assigned_to, notes))
        self.rec.profile.activities.append(
            Activity(ActivityKind.APPOINTMENT, self.crm.now(), assigned_to, f"Sales appointment {starts_at:%c}")
        )
        return _next("appt")

    def set_progress(self, status: str) -> None:
        self._guard()
        self.rec.progress = status

    def progress(self) -> str:
        self._guard()
        return self.rec.progress

    def print_profile_page1(self) -> None:
        self._guard()
        self.rec.printed += 1

    def close(self) -> None:
        if not self.closed:
            self.closed = True
            self.adapter._open.discard(id(self))


class MockEleadsAdapter(EleadsAdapter):
    def __init__(self, crm: MockCrm):
        self.crm = crm
        self.rooftop: str | None = None
        self.logged_in = False
        self._open: set[int] = set()

    def login(self) -> None:
        self.logged_in = True

    def select_rooftop(self, rooftop_id: str) -> None:
        self.rooftop = rooftop_id

    def current_identity(self) -> tuple[str, str]:
        return self.crm.user_name, self.rooftop or ""

    def daily_organizer(self, rooftop_id: str) -> list[WorkItem]:
        return [self._item(r) for (rt, _), r in self.crm.records.items()
                if rt == rooftop_id and not r.is_past_customer]

    def new_leads(self, rooftop_id: str, salesperson: str, since: datetime) -> list[WorkItem]:
        return [self._item(r) for (rt, _), r in self.crm.records.items()
                if rt == rooftop_id and r.is_new_lead and r.profile.assigned_salesperson == salesperson
                and (r.profile.lead_assigned_at is None or r.profile.lead_assigned_at >= since)]

    def past_customers(self, rooftop_id: str, cursor: str | None, limit: int) -> tuple[list[WorkItem], str | None]:
        rows = sorted((r for (rt, _), r in self.crm.records.items() if rt == rooftop_id and r.is_past_customer),
                      key=lambda r: r.profile.customer_id)
        start = int(cursor or 0)
        page = rows[start:start + limit]
        nxt = str(start + limit) if start + limit < len(rows) else None
        return [self._item(r) for r in page], nxt

    def open_profile(self, rooftop_id: str, customer_id: str, opportunity_id: str) -> ProfileSession:
        session = MockProfileSession(self, self.crm.record(rooftop_id, customer_id))
        self._open.add(id(session))
        self.crm.max_concurrent_tabs = max(self.crm.max_concurrent_tabs, len(self._open))
        return session

    def daily_activity(self, rooftop_id: str, salesperson: str, day: date) -> list[DashboardRow]:
        return [r for r in self.crm.dashboard
                if r.rooftop_id == rooftop_id and r.salesperson == salesperson and r.completed_at.date() == day]

    def open_tab_count(self) -> int:
        return len(self._open)

    def shutdown(self) -> None:
        self._open.clear()

    @staticmethod
    def _item(r: MockRecord) -> WorkItem:
        p = r.profile
        return WorkItem(p.rooftop_id, p.customer_id, p.opportunity_id, p.full_name, p.opportunity_type,
                        p.assigned_salesperson, vehicle=p.vehicle_of_interest, assigned_at=p.lead_assigned_at)
