from __future__ import annotations

from titan.agents.context import AgentContext
from titan.agents.drafting import has_inbound_since, matching_outbound
from titan.crm.base import CrmTask, DuplicateTaskWarning, ProfileSession
from titan.models import Channel, MessageStatus, OutboundMessage, TaskKind
from titan.resources import ProfileSessionManager

COMPLETION_NOTES = {
    Channel.EMAIL: "Manual email completed via system.",
    Channel.SMS: "SMS text completed via system.",
}
FOLLOW_UP_KIND = {Channel.EMAIL: TaskKind.FOLLOW_UP_EMAIL, Channel.SMS: TaskKind.FOLLOW_UP_SMS}


class SendExecutor:
    """PDF section 4 safe-send sequence. Sends once, verifies in CRM history, completes a separate task per channel."""

    def __init__(self, ctx: AgentContext, sessions: ProfileSessionManager):
        self.ctx = ctx
        self.sessions = sessions

    def _set(self, m: OutboundMessage, status: MessageStatus, event: str, **detail) -> MessageStatus:
        m.status = status
        if "error" in detail:
            m.error = detail["error"]
        self.ctx.store.save_message(m)
        self.ctx.audit(event, rooftop_id=m.rooftop_id, customer_id=m.customer_id, message_id=m.id,
                       channel=m.channel.value, status=status.value, **detail)
        return status

    def execute(self, m: OutboundMessage) -> MessageStatus:
        self.ctx.guard.check_rooftop(m.rooftop_id)
        if m.status is not MessageStatus.APPROVED:
            return m.status
        if not m.approval_is_valid:
            return self._set(m, MessageStatus.BLOCKED, "send_blocked", error="Approval missing or content changed")
        now = self.ctx.clock.now()
        is_open, why = self.ctx.window.is_open(m.channel, now)
        if not is_open:
            return m.status

        adapter = self.sessions.adapter
        with self.sessions.profile(m.rooftop_id, m.customer_id, m.opportunity_id) as session:
            user, rooftop = adapter.current_identity()
            self.ctx.guard.verify_session(expected_rooftop=m.rooftop_id, user_name=user, rooftop_id=rooftop)
            profile = session.read_profile()
            decision = self.ctx.eligibility.evaluate(profile, now, ignore_own_outbound_since=m.created_at)
            if m.channel not in decision.channels:
                return self._set(m, MessageStatus.BLOCKED, "send_blocked",
                                 error="Channel no longer permitted: " + "; ".join(decision.reasons))
            if not decision.qualified:
                return self._set(m, MessageStatus.BLOCKED, "send_blocked", error="; ".join(decision.reasons))
            if m.created_at and has_inbound_since(profile, m.created_at):
                return self._set(m, MessageStatus.BLOCKED, "send_blocked",
                                 error="Customer replied after this draft was written; redraft required")
            current_recipient = profile.email if m.channel is Channel.EMAIL else profile.phone
            if current_recipient != m.recipient:
                return self._set(m, MessageStatus.BLOCKED, "send_blocked", error="Recipient changed in CRM")

            already = matching_outbound(profile.activities, m.channel, m.approved_at, self.ctx.me)
            if already is None:
                self._set(m, MessageStatus.SENDING, "send_started")
                if m.channel is Channel.EMAIL:
                    receipt = session.send_email(m.subject or "", m.body, m.recipient)
                else:
                    receipt = session.send_sms(m.body, m.recipient)
                activities = session.refresh_activities()
                already = matching_outbound(activities, m.channel, m.approved_at, self.ctx.me)
                if already is None:
                    return self._set(m, MessageStatus.SEND_UNCERTAIN, "send_uncertain",
                                     error=f"No matching CRM activity after send (UI: {receipt.ui_message!r}); "
                                           "verify manually, do not resend")
            m.sent_at = already.at
            m.verified_at = self.ctx.clock.now()
            m.crm_activity_id = already.crm_activity_id
            self._set(m, MessageStatus.SENT_VERIFIED, "send_verified", crm_activity_id=already.crm_activity_id)
            return self._complete(m, session)

    def _complete(self, m: OutboundMessage, session: ProfileSession) -> MessageStatus:
        task = self._task_for(m, session)
        if task.status != "completed":
            session.complete_task(task.crm_task_id, COMPLETION_NOTES[m.channel])
        result = session.task_status(task.crm_task_id)
        if result.status != "completed" or result.assigned_to.lower() != self.ctx.me.lower():
            m.crm_task_id = task.crm_task_id
            return self._set(m, MessageStatus.SENT_VERIFIED, "task_completion_unverified",
                             error=f"Task {task.crm_task_id} shows {result.status} for {result.assigned_to}")
        m.crm_task_id = task.crm_task_id
        m.completed_at = self.ctx.clock.now()
        m.error = None
        return self._set(m, MessageStatus.COMPLETED, "task_completed", crm_task_id=task.crm_task_id)

    def _task_for(self, m: OutboundMessage, session: ProfileSession) -> CrmTask:
        if m.crm_task_id:
            return session.task_status(m.crm_task_id)
        wanted = {m.channel.value, FOLLOW_UP_KIND[m.channel].value}
        for t in session.open_tasks():
            if t.kind in wanted and t.assigned_to.lower() == self.ctx.me.lower():
                return t
        try:
            return session.create_task(m.channel, self.ctx.clock.now(), COMPLETION_NOTES[m.channel])
        except DuplicateTaskWarning as dup:
            return dup.existing

    def reconcile_in_flight(self, rooftop_id: str) -> list[OutboundMessage]:
        """After a crash/restart: never resend a message that was mid-send; verify history instead."""
        results = []
        pending_completion = self.ctx.store.messages(MessageStatus.SENT_VERIFIED, rooftop_id=rooftop_id)
        for m in self.ctx.store.messages(MessageStatus.SENDING, rooftop_id=rooftop_id):
            with self.sessions.profile(m.rooftop_id, m.customer_id, m.opportunity_id) as session:
                found = matching_outbound(session.refresh_activities(), m.channel, m.approved_at, self.ctx.me)
                if found is None:
                    self._set(m, MessageStatus.SEND_UNCERTAIN, "send_uncertain",
                              error="Interrupted mid-send; no CRM activity found. Verify manually.")
                else:
                    m.sent_at, m.crm_activity_id = found.at, found.crm_activity_id
                    m.verified_at = self.ctx.clock.now()
                    self._set(m, MessageStatus.SENT_VERIFIED, "send_verified", recovered=True)
                    self._complete(m, session)
            results.append(m)
        for m in pending_completion:
            with self.sessions.profile(m.rooftop_id, m.customer_id, m.opportunity_id) as session:
                self._complete(m, session)
            results.append(m)
        return results
