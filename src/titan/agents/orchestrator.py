from __future__ import annotations

import logging
import threading
from dataclasses import dataclass
from typing import Callable

from titan.agents.appointments import AppointmentAgent
from titan.agents.context import AgentContext
from titan.agents.drafting import prepare_outreach
from titan.agents.executor import SendExecutor
from titan.agents.new_leads import NewLeadAgent
from titan.agents.planner import DailyPlanner
from titan.agents.reengagement import ReengagementAgent
from titan.crm.base import EleadsAdapter, SessionExpired
from titan.models import AppointmentStatus, Disposition, MessageSource, MessageStatus, TaskStatus
from titan.policy.identity import ScopeViolation
from titan.resources import ProfileSessionManager, ResourceMonitor

log = logging.getLogger(__name__)

SOURCE_PRIORITY = {MessageSource.REPLY: 0, MessageSource.NEW_LEAD: 1, MessageSource.FOLLOW_UP: 2,
                   MessageSource.REENGAGEMENT: 3}


@dataclass
class TickResult:
    rooftop_id: str
    drafted: int = 0
    sent: int = 0
    appointments_saved: int = 0
    reengagement_drafted: int = 0
    ram: str = "ok"


class RooftopWorker:
    """Owns one browser/adapter for one rooftop. Two workers run in parallel (Nissan + Mitsubishi)."""

    def __init__(self, ctx: AgentContext, rooftop_id: str, adapter: EleadsAdapter, monitor: ResourceMonitor):
        ctx.guard.check_rooftop(rooftop_id)
        self.ctx = ctx
        self.rooftop_id = rooftop_id
        self.adapter = adapter
        self.monitor = monitor
        self.sessions = ProfileSessionManager(
            adapter, ctx.settings.resources, monitor,
            on_close=lambda cid, secs: ctx.audit("profile_closed", rooftop_id=rooftop_id, customer_id=cid,
                                                 open_seconds=round(secs, 2)),
        )
        self.planner = DailyPlanner(ctx)
        self.new_leads = NewLeadAgent(ctx, adapter, self.sessions, self.planner)
        self.reengagement = ReengagementAgent(ctx, adapter, self.sessions)
        self.executor = SendExecutor(ctx, self.sessions)
        self.appointments = AppointmentAgent(ctx)
        self._ready = False

    def ensure_session(self) -> None:
        if not self._ready:
            self.adapter.login()
            self.adapter.select_rooftop(self.rooftop_id)
        user, rooftop = self.adapter.current_identity()
        self.ctx.guard.verify_session(expected_rooftop=self.rooftop_id, user_name=user, rooftop_id=rooftop)
        if not self._ready:
            self.ctx.audit("session_verified", rooftop_id=self.rooftop_id, user=user)
            self.executor.reconcile_in_flight(self.rooftop_id)
            self._ready = True

    def tick(self) -> TickResult:
        result = TickResult(self.rooftop_id)
        self.ensure_session()
        now = self.ctx.clock.now()

        result.drafted += len(self.new_leads.poll(self.rooftop_id))

        plan_key = f"planner:{self.rooftop_id}:{now:%Y-%m-%d}"
        if self.ctx.window.is_business_day(now) and self.ctx.store.get_cursor(plan_key) is None:
            tasks = self.planner.regenerate(self.rooftop_id, now)
            self.planner.push_to_crm(self.sessions, tasks)
            self.ctx.store.set_cursor(plan_key, now.isoformat())

        result.drafted += self._draft_due_followups()

        approved = sorted(self.ctx.store.messages(MessageStatus.APPROVED, rooftop_id=self.rooftop_id),
                          key=lambda m: (SOURCE_PRIORITY[m.source], m.created_at))
        for m in approved:
            if self.executor.execute(m) is MessageStatus.COMPLETED:
                result.sent += 1

        for a in self.ctx.store.appointments(AppointmentStatus.APPROVED):
            if a.rooftop_id == self.rooftop_id:
                self.appointments.save(a, self.sessions)
                result.appointments_saved += 1

        if result.drafted == 0:
            result.reengagement_drafted = len(self.reengagement.run_batch(self.rooftop_id))

        result.ram = self.monitor.check()
        if result.ram == "hard":
            log.warning("Recycling browser for %s after RAM hard limit", self.rooftop_id)
            self.adapter.shutdown()
            self._ready = False
        return result

    def _draft_due_followups(self) -> int:
        count = 0
        for task in self.planner.due_email_followups(self.rooftop_id):
            with self.sessions.profile(task.rooftop_id, task.customer_id, task.opportunity_id) as session:
                profile = session.read_profile()
            decision = self.ctx.eligibility.evaluate(profile, self.ctx.clock.now())
            if decision.disposition is Disposition.EXCLUDED:
                task.status = TaskStatus.CANCELLED
            elif decision.qualified:
                count += len(prepare_outreach(self.ctx, profile, decision, MessageSource.FOLLOW_UP))
                task.status = TaskStatus.DONE
            else:
                continue
            self.ctx.store.save_task(task)
        return count


class Orchestrator:
    """Primary agent: runs one RooftopWorker thread per authorized rooftop until stopped."""

    def __init__(self, ctx: AgentContext, adapter_factory: Callable[[str], EleadsAdapter],
                 monitor: ResourceMonitor | None = None):
        self.ctx = ctx
        self.monitor = monitor or ResourceMonitor(ctx.settings.resources)
        self.workers = {
            rid: RooftopWorker(ctx, rid, adapter_factory(rid), self.monitor)
            for rid in ctx.settings.identity.allowed_rooftops
        }
        self._stop = threading.Event()
        self._threads: list[threading.Thread] = []

    def run_once(self) -> list[TickResult]:
        return [w.tick() for w in self.workers.values()]

    def _loop(self, worker: RooftopWorker) -> None:
        interval = self.ctx.settings.resources.poll_interval_seconds
        backoff = interval
        while not self._stop.is_set():
            try:
                worker.tick()
                backoff = interval
            except SessionExpired:
                log.warning("Session expired for %s; re-login on next tick", worker.rooftop_id)
                worker._ready = False
            except ScopeViolation as exc:
                self.ctx.audit("scope_violation", rooftop_id=worker.rooftop_id, error=str(exc))
                self.ctx.notifier.notify("Titan paused", f"{worker.rooftop_id}: {exc}")
                worker._ready = False
                backoff = min(backoff * 2, 900)
            except Exception as exc:
                log.exception("Worker %s tick failed", worker.rooftop_id)
                self.ctx.audit("worker_error", rooftop_id=worker.rooftop_id, error=repr(exc))
                worker._ready = False
                backoff = min(backoff * 2, 900)
            self._stop.wait(backoff)
        worker.adapter.shutdown()

    def start(self) -> None:
        for worker in self.workers.values():
            t = threading.Thread(target=self._loop, args=(worker,), name=f"titan-{worker.rooftop_id}", daemon=True)
            t.start()
            self._threads.append(t)

    def stop(self, timeout: float = 30) -> None:
        self._stop.set()
        for t in self._threads:
            t.join(timeout)

    def wait(self) -> None:
        try:
            while any(t.is_alive() for t in self._threads):
                for t in self._threads:
                    t.join(1)
        except KeyboardInterrupt:
            self.stop()
