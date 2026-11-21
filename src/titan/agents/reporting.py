from __future__ import annotations

from collections import Counter
from dataclasses import dataclass, field
from datetime import date, datetime

from titan.agents.context import AgentContext
from titan.crm.base import EleadsAdapter
from titan.models import AppointmentStatus, Channel, MessageStatus


@dataclass
class RooftopReport:
    rooftop_id: str
    rooftop_name: str
    open_tasks: int = 0
    distinct_opportunities: int = 0
    own_opportunities: int = 0
    other_opportunities: int = 0
    dispositions: Counter = field(default_factory=Counter)
    disposition_examples: list[str] = field(default_factory=list)
    email_completed: int = 0
    sms_completed: int = 0
    email_pending_approval: int = 0
    sms_pending_approval: int = 0
    approved_queued: int = 0
    dashboard_rows: int | None = None
    dashboard_missing: list[str] = field(default_factory=list)
    uncertain_sends: list[str] = field(default_factory=list)
    blocked_sends: list[str] = field(default_factory=list)
    appointments_pending: int = 0
    appointments_saved: int = 0
    sla_misses: list[str] = field(default_factory=list)


class Reporter:
    """PDF section 6 management report. Counts only CRM-confirmed work; mismatches are listed, never patched."""

    def __init__(self, ctx: AgentContext):
        self.ctx = ctx

    def build(self, day: date, adapters: dict[str, EleadsAdapter] | None = None) -> list[RooftopReport]:
        reports = []
        for rooftop_id, name in self.ctx.settings.identity.allowed_rooftops.items():
            r = RooftopReport(rooftop_id, name)
            adapter = (adapters or {}).get(rooftop_id)
            if adapter is not None:
                items = adapter.daily_organizer(rooftop_id)
                r.open_tasks = len(items)
                opps = {(i.customer_id, i.opportunity_id): i for i in items}
                r.distinct_opportunities = len(opps)
                r.own_opportunities = sum(1 for i in opps.values() if self.ctx.guard.is_own(i.assigned_to))
                r.other_opportunities = r.distinct_opportunities - r.own_opportunities

            for d in self.ctx.store.digests():
                if d.rooftop_id == rooftop_id and d.updated_at.date() == day:
                    r.dispositions[d.disposition.value] += 1
                    if d.disposition.value != "qualified" and len(r.disposition_examples) < 20:
                        r.disposition_examples.append(f"{d.customer_id}: {d.disposition.value} - "
                                                      f"{'; '.join(d.disposition_reasons)}")

            completed_ids: list[str] = []
            for m in self.ctx.store.messages(rooftop_id=rooftop_id):
                label = f"{m.customer_name or m.customer_id} [{m.channel.value}]"
                if m.status is MessageStatus.COMPLETED and m.completed_at and m.completed_at.date() == day:
                    if m.channel is Channel.EMAIL:
                        r.email_completed += 1
                    else:
                        r.sms_completed += 1
                    if m.crm_task_id:
                        completed_ids.append(m.crm_task_id)
                    if m.detected_at and m.sent_at:
                        took = (m.sent_at - m.detected_at).total_seconds()
                        if took > self.ctx.settings.sla.new_lead_total_seconds:
                            r.sla_misses.append(f"{label}: {took:.0f}s detection-to-send")
                elif m.status is MessageStatus.PENDING_APPROVAL:
                    if m.channel is Channel.EMAIL:
                        r.email_pending_approval += 1
                    else:
                        r.sms_pending_approval += 1
                elif m.status is MessageStatus.APPROVED:
                    r.approved_queued += 1
                elif m.status is MessageStatus.SEND_UNCERTAIN:
                    r.uncertain_sends.append(f"{label}: {m.error}")
                elif m.status is MessageStatus.BLOCKED:
                    r.blocked_sends.append(f"{label}: {m.error}")

            if adapter is not None:
                rows = adapter.daily_activity(rooftop_id, self.ctx.me, day)
                r.dashboard_rows = len(rows)
                on_dashboard = {row.crm_task_id for row in rows}
                r.dashboard_missing = [tid for tid in completed_ids if tid not in on_dashboard]
                for tid in r.dashboard_missing:
                    self.ctx.audit("dashboard_mismatch", rooftop_id=rooftop_id, crm_task_id=tid,
                                   action="escalate to CRM support; do not create substitute records")

            for a in self.ctx.store.appointments():
                if a.rooftop_id != rooftop_id:
                    continue
                if a.status in (AppointmentStatus.PENDING_APPROVAL, AppointmentStatus.APPROVED):
                    r.appointments_pending += 1
                elif a.status is AppointmentStatus.SAVED:
                    r.appointments_saved += 1
            reports.append(r)
        return reports

    @staticmethod
    def render(reports: list[RooftopReport], day: date, generated: datetime) -> str:
        out = [f"TITAN MANAGEMENT REPORT - {day:%A %B %d, %Y} (generated {generated:%H:%M %Z})", ""]
        for r in reports:
            out += [
                f"== {r.rooftop_name} ({r.rooftop_id}) ==",
                f"Open tasks: {r.open_tasks} | distinct opportunities: {r.distinct_opportunities} "
                f"(Glenn: {r.own_opportunities}, other consultants: {r.other_opportunities})",
                "Profiles reviewed: " + (", ".join(f"{k}={v}" for k, v in sorted(r.dispositions.items())) or "none"),
                f"CRM-confirmed EMAIL completed: {r.email_completed} | pending approval: {r.email_pending_approval}",
                f"CRM-confirmed SMS completed:   {r.sms_completed} | pending approval: {r.sms_pending_approval}",
                f"Approved and queued for the next send window: {r.approved_queued}",
                f"Dashboard rows (Sales Performance/Daily Activity): "
                f"{'not checked' if r.dashboard_rows is None else r.dashboard_rows}",
                f"Appointments: {r.appointments_saved} saved, {r.appointments_pending} awaiting approval/save",
            ]
            for title, items in (("Dashboard mismatches (escalate to CRM support)", r.dashboard_missing),
                                 ("Uncertain sends (verify, do not resend)", r.uncertain_sends),
                                 ("Blocked sends", r.blocked_sends),
                                 ("New-lead target misses", r.sla_misses),
                                 ("Held/excluded examples", r.disposition_examples)):
                if items:
                    out.append(f"{title}:")
                    out += [f"  - {i}" for i in items]
            out.append("")
        return "\n".join(out)
