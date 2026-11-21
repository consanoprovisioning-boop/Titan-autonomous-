from __future__ import annotations

from datetime import timedelta

from titan.agents.context import AgentContext
from titan.agents.drafting import next_best_action, prepare_outreach, summarize
from titan.crm.base import EleadsAdapter, WorkItem
from titan.messaging import propose_slots
from titan.models import Channel, CustomerDigest, MessageSource, OutboundMessage
from titan.policy.eligibility import OUTBOUND_KINDS
from titan.resources import ProfileSessionManager

DIGEST_REFRESH = timedelta(hours=24)


class ReengagementAgent:
    """Walks the past-customer database in small batches, keeps a digest per customer, drafts 48h-cycle outreach."""

    def __init__(self, ctx: AgentContext, adapter: EleadsAdapter, sessions: ProfileSessionManager):
        self.ctx = ctx
        self.adapter = adapter
        self.sessions = sessions

    def run_batch(self, rooftop_id: str) -> list[OutboundMessage]:
        cursor_key = f"past:{rooftop_id}"
        cursor = self.ctx.store.get_cursor(cursor_key)
        items, nxt = self.adapter.past_customers(
            rooftop_id, cursor, self.ctx.settings.resources.history_scan_batch_size
        )
        self.ctx.store.set_cursor(cursor_key, nxt)
        drafted: list[OutboundMessage] = []
        for item in items:
            drafted.extend(self.process(item))
        return drafted

    def process(self, item: WorkItem) -> list[OutboundMessage]:
        now = self.ctx.clock.now()
        existing = self.ctx.store.get_digest(item.customer_id, item.rooftop_id)
        if existing and now - existing.updated_at < DIGEST_REFRESH:
            return []
        rooftop_name = self.ctx.guard.rooftop_name(item.rooftop_id)
        messages: list[OutboundMessage] = []
        with self.sessions.profile(item.rooftop_id, item.customer_id, item.opportunity_id) as session:
            profile = session.read_profile()
            decision = self.ctx.eligibility.evaluate(profile, now)
            summary = summarize(profile, decision, rooftop_name)
            nba = next_best_action(profile, decision)
            draft = self.ctx.composer.compose(profile, rooftop_name, self.ctx.me, propose_slots(self.ctx.window, now))
            last_out = profile.last_activity(*OUTBOUND_KINDS)
            digest = CustomerDigest(
                customer_id=profile.customer_id,
                rooftop_id=profile.rooftop_id,
                summary=summary,
                next_best_action=nba,
                sms_template=draft.sms_body if Channel.SMS in decision.channels else None,
                email_subject=draft.email_subject if Channel.EMAIL in decision.channels else None,
                email_template=draft.email_body if Channel.EMAIL in decision.channels else None,
                disposition=decision.disposition,
                disposition_reasons=decision.reasons,
                updated_at=now,
                last_outreach_at=last_out.at if last_out else None,
            )
            changed = existing is None or (existing.summary, existing.next_best_action) != (summary, nba)
            if self.ctx.settings.crm.write_strategy_notes and changed:
                session.add_note(f"[Titan summary {now:%Y-%m-%d}]\n{summary}\nNext best: {nba}")
        self.ctx.store.save_digest(digest)
        self.ctx.audit("digest_updated", rooftop_id=item.rooftop_id, customer_id=item.customer_id,
                       disposition=decision.disposition.value)
        if decision.qualified:
            messages = prepare_outreach(self.ctx, profile, decision, MessageSource.REENGAGEMENT)
        return messages
