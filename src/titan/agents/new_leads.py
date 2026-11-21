from __future__ import annotations

import time
from datetime import timedelta

from titan.agents.context import AgentContext
from titan.agents.drafting import prepare_outreach
from titan.agents.planner import DailyPlanner
from titan.crm.base import EleadsAdapter
from titan.models import MessageSource, OutboundMessage
from titan.resources import ProfileSessionManager


class NewLeadAgent:
    """Watches Glenn-assigned new leads and produces approval-ready email + SMS within the draft SLA."""

    def __init__(self, ctx: AgentContext, adapter: EleadsAdapter, sessions: ProfileSessionManager,
                 planner: DailyPlanner):
        self.ctx = ctx
        self.adapter = adapter
        self.sessions = sessions
        self.planner = planner

    def poll(self, rooftop_id: str) -> list[OutboundMessage]:
        now = self.ctx.clock.now()
        since = now - timedelta(hours=self.ctx.settings.cadence.new_lead_lookback_hours)
        drafted: list[OutboundMessage] = []
        for lead in self.adapter.new_leads(rooftop_id, self.ctx.me, since):
            if not self.ctx.guard.is_own(lead.assigned_to):
                continue
            if self.ctx.store.has_seen_lead(rooftop_id, lead.opportunity_id):
                continue
            drafted.extend(self._handle(lead.rooftop_id, lead.customer_id, lead.opportunity_id))
            # Only after a successful handle. A timeout before this must leave the lead unseen
            # so the next poll retries it; mark-before-handle dropped the lead forever.
            self.ctx.store.mark_lead_seen(rooftop_id, lead.opportunity_id, now)
        return drafted

    def _handle(self, rooftop_id: str, customer_id: str, opportunity_id: str) -> list[OutboundMessage]:
        detected = self.ctx.clock.now()
        started = time.monotonic()
        with self.sessions.profile(rooftop_id, customer_id, opportunity_id) as session:
            profile = session.read_profile()
        decision = self.ctx.eligibility.evaluate(profile, detected)
        messages = prepare_outreach(self.ctx, profile, decision, MessageSource.NEW_LEAD, detected_at=detected)
        if decision.qualified:
            self.planner.schedule_after_contact(profile, decision.channels, active=True)
        draft_seconds = time.monotonic() - started
        sla = self.ctx.settings.sla.new_lead_draft_seconds
        self.ctx.audit("new_lead_processed", rooftop_id=rooftop_id, customer_id=customer_id,
                       disposition=decision.disposition.value, reasons=list(decision.reasons),
                       drafts=[m.id for m in messages], draft_seconds=round(draft_seconds, 2),
                       draft_sla_met=draft_seconds <= sla)
        name = profile.full_name
        if messages:
            self.ctx.notifier.notify(
                "New lead - approval needed",
                f"{name} ({self.ctx.guard.rooftop_name(rooftop_id)}): {len(messages)} draft(s) ready. "
                f"Run: titan approvals",
            )
        else:
            self.ctx.notifier.notify(
                "New lead - review needed",
                f"{name}: {decision.disposition.value} - {'; '.join(decision.reasons)}",
            )
        return messages
