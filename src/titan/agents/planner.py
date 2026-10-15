from __future__ import annotations

from datetime import datetime, timedelta

from titan.agents.context import AgentContext
from titan.crm.base import DuplicateTaskWarning
from titan.messaging import follow_up_gap
from titan.models import Channel, CustomerProfile, PlannedTask, TaskKind, TaskStatus
from titan.resources import ProfileSessionManager


class DailyPlanner:
    """Next-day follow-ups after first contact, and a daily regenerated, prioritized, spaced task plan."""

    def __init__(self, ctx: AgentContext):
        self.ctx = ctx

    def schedule_after_contact(self, profile: CustomerProfile, channels: frozenset[Channel],
                               active: bool = True) -> list[PlannedTask]:
        now = self.ctx.clock.now()
        due = follow_up_gap(self.ctx.window, now, active)
        spacing = timedelta(minutes=self.ctx.settings.cadence.task_spacing_minutes)
        planned: list[PlannedTask] = []
        wanted: list[tuple[TaskKind, str]] = []
        if Channel.EMAIL in channels:
            wanted.append((TaskKind.FOLLOW_UP_EMAIL, "Follow-up email after initial contact"))
        if Channel.PHONE in channels:
            wanted.append((TaskKind.FOLLOW_UP_CALL, "Follow-up call after initial contact"))
        for kind, reason in wanted:
            if self.ctx.store.has_open_task(profile.customer_id, kind):
                continue
            task = PlannedTask(profile.customer_id, profile.opportunity_id, profile.rooftop_id, kind,
                               due, profile.opportunity_type.rank, reason)
            self.ctx.store.save_task(task)
            planned.append(task)
            due += spacing
        return planned

    def regenerate(self, rooftop_id: str, day: datetime) -> list[PlannedTask]:
        """Re-prioritize every open task due today or overdue, then space them >= N minutes apart."""
        end_of_day = day.replace(hour=23, minute=59, second=59)
        open_tasks = [t for t in self.ctx.store.tasks(TaskStatus.SCHEDULED, TaskStatus.CRM_CREATED)
                      if t.rooftop_id == rooftop_id and t.due_at <= end_of_day]
        open_tasks.sort(key=lambda t: (t.priority, t.due_at))
        spacing = timedelta(minutes=self.ctx.settings.cadence.task_spacing_minutes)
        slot = self.ctx.window.clamp_to_business_hours(max(day, self.ctx.clock.now()))
        for t in open_tasks:
            if t.status is TaskStatus.SCHEDULED:
                t.due_at = slot
                self.ctx.store.save_task(t)
                slot = self.ctx.window.clamp_to_business_hours(slot + spacing)
        self.ctx.audit("daily_plan_regenerated", rooftop_id=rooftop_id, tasks=len(open_tasks))
        return open_tasks

    def push_to_crm(self, sessions: ProfileSessionManager, tasks: list[PlannedTask]) -> None:
        for t in tasks:
            if t.status is not TaskStatus.SCHEDULED:
                continue
            with sessions.profile(t.rooftop_id, t.customer_id, t.opportunity_id) as session:
                try:
                    crm_task = session.create_task(t.kind, t.due_at, t.reason)
                except DuplicateTaskWarning as dup:
                    crm_task = dup.existing
                    self.ctx.audit("duplicate_task_avoided", rooftop_id=t.rooftop_id, customer_id=t.customer_id,
                                   existing=crm_task.crm_task_id, kind=t.kind.value)
            t.crm_task_id = crm_task.crm_task_id
            t.status = TaskStatus.CRM_CREATED
            self.ctx.store.save_task(t)

    def due_email_followups(self, rooftop_id: str) -> list[PlannedTask]:
        now = self.ctx.clock.now()
        return [t for t in self.ctx.store.tasks(TaskStatus.SCHEDULED, TaskStatus.CRM_CREATED)
                if t.rooftop_id == rooftop_id and t.kind is TaskKind.FOLLOW_UP_EMAIL and t.due_at <= now]
