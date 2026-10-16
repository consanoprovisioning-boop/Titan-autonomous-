from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass
from datetime import date, datetime

from titan.models import Activity, Channel, CustomerProfile, OpportunityType, TaskKind


class CrmError(RuntimeError):
    pass


class SessionExpired(CrmError):
    pass


@dataclass(frozen=True)
class WorkItem:
    """One row from the Daily Organizer / lead list. Never sufficient on its own to qualify a customer."""

    rooftop_id: str
    customer_id: str
    opportunity_id: str
    customer_name: str
    opportunity_type: OpportunityType
    assigned_to: str
    due_at: datetime | None = None
    task_type: str | None = None
    vehicle: str | None = None
    comments: str | None = None
    assigned_at: datetime | None = None


@dataclass(frozen=True)
class SendReceipt:
    """What the UI reported after clicking Send once. Not proof of delivery."""

    clicked: bool
    ui_message: str = ""


@dataclass(frozen=True)
class CrmTask:
    crm_task_id: str
    kind: str
    status: str
    assigned_to: str
    due_at: datetime | None
    note: str = ""


@dataclass(frozen=True)
class DashboardRow:
    """A row from Sales Performance / Daily Activity reporting."""

    rooftop_id: str
    salesperson: str
    activity_type: str
    customer_id: str
    crm_task_id: str | None
    completed_at: datetime


class ProfileSession(ABC):
    """An open customer profile tab. Must be closed via the ProfileSessionManager context."""

    @abstractmethod
    def read_profile(self) -> CustomerProfile: ...

    @abstractmethod
    def refresh_activities(self) -> list[Activity]: ...

    @abstractmethod
    def send_email(self, subject: str, body: str, recipient: str) -> SendReceipt: ...

    @abstractmethod
    def send_sms(self, body: str, recipient: str) -> SendReceipt: ...

    @abstractmethod
    def open_tasks(self) -> list[CrmTask]: ...

    @abstractmethod
    def create_task(self, kind: TaskKind | Channel, due_at: datetime, note: str) -> CrmTask:
        """Raises DuplicateTaskWarning when Eleads warns that a same-type task exists."""

    @abstractmethod
    def complete_task(self, crm_task_id: str, outcome_note: str) -> None: ...

    @abstractmethod
    def task_status(self, crm_task_id: str) -> CrmTask: ...

    @abstractmethod
    def add_note(self, text: str) -> None: ...

    @abstractmethod
    def create_sales_appointment(self, starts_at: datetime, assigned_to: str, notes: str) -> str: ...

    @abstractmethod
    def set_progress(self, status: str) -> None: ...

    @abstractmethod
    def progress(self) -> str: ...

    @abstractmethod
    def print_profile_page1(self) -> None: ...

    @abstractmethod
    def close(self) -> None: ...


class DuplicateTaskWarning(CrmError):
    def __init__(self, existing: CrmTask):
        super().__init__(f"Same-type task already exists: {existing.crm_task_id}")
        self.existing = existing


class EleadsAdapter(ABC):
    """One adapter instance per rooftop worker; implementations need not be thread-safe."""

    @abstractmethod
    def login(self) -> None: ...

    @abstractmethod
    def select_rooftop(self, rooftop_id: str) -> None: ...

    @abstractmethod
    def current_identity(self) -> tuple[str, str]:
        """Returns (user display name, active rooftop id) as shown by the CRM UI."""

    @abstractmethod
    def daily_organizer(self, rooftop_id: str) -> list[WorkItem]:
        """Daily Organizer, status Open, contact type All."""

    @abstractmethod
    def new_leads(self, rooftop_id: str, salesperson: str, since: datetime) -> list[WorkItem]: ...

    @abstractmethod
    def past_customers(self, rooftop_id: str, cursor: str | None, limit: int) -> tuple[list[WorkItem], str | None]: ...

    @abstractmethod
    def open_profile(self, rooftop_id: str, customer_id: str, opportunity_id: str) -> ProfileSession: ...

    @abstractmethod
    def daily_activity(self, rooftop_id: str, salesperson: str, day: date) -> list[DashboardRow]: ...

    @abstractmethod
    def open_tab_count(self) -> int: ...

    @abstractmethod
    def shutdown(self) -> None: ...
