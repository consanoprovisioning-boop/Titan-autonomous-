from __future__ import annotations

import re

from titan.agents.context import AgentContext
from titan.messaging import FORBIDDEN_PATTERNS, INCENTIVE_LANGUAGE
from titan.models import Channel, MessageStatus, OutboundMessage, new_id


class ApprovalError(RuntimeError):
    pass


class ApprovalService:
    """Glenn's approval gate. Approval binds to a hash of rooftop+recipient+channel+content."""

    def __init__(self, ctx: AgentContext):
        self.ctx = ctx

    def pending(self) -> list[OutboundMessage]:
        return self.ctx.store.messages(MessageStatus.PENDING_APPROVAL)

    def blocking_problems(self, m: OutboundMessage) -> list[str]:
        problems = []
        if "[ANSWER" in m.body:
            problems.append("Draft still contains an [ANSWER] placeholder")
        scrubbed = m.body.replace(INCENTIVE_LANGUAGE, "")
        for pattern, label in FORBIDDEN_PATTERNS:
            if re.search(pattern, scrubbed, re.IGNORECASE):
                problems.append(f"Unsupported promise: {label}")
        if m.channel is Channel.EMAIL:
            words = len(re.findall(r"\b\w[\w'-]*\b", m.body))
            if words > self.ctx.settings.messaging.email_max_words:
                problems.append(f"Email is {words} words")
            if not (m.subject or "").strip():
                problems.append("Missing subject")
        else:
            if len(m.body) > self.ctx.settings.messaging.sms_max_chars:
                problems.append("SMS too long")
            if "stop" not in m.body.lower():
                problems.append("SMS missing opt-out language")
        if not m.recipient:
            problems.append("No recipient")
        return problems

    def approve(self, message_ids: list[str], approver: str) -> list[OutboundMessage]:
        if not message_ids:
            raise ApprovalError("No messages selected")
        if self.ctx.settings.approval.mode == "every_send" and len(message_ids) > 1:
            raise ApprovalError("approval.mode is 'every_send': approve one exact message at a time")
        batch_id = new_id() if len(message_ids) > 1 else None
        now = self.ctx.clock.now()
        approved = []
        for mid in message_ids:
            m = self.ctx.store.get_message(mid)
            if m is None or m.status is not MessageStatus.PENDING_APPROVAL:
                raise ApprovalError(f"Message {mid} is not awaiting approval")
            problems = self.blocking_problems(m)
            if problems:
                raise ApprovalError(f"Message {mid} cannot be approved: {'; '.join(problems)}")
            m.approved_hash = m.content_hash
            m.approved_at = now
            m.approved_by = approver
            m.batch_id = batch_id
            m.status = MessageStatus.APPROVED
            m.error = None
            self.ctx.store.save_message(m)
            self.ctx.audit("message_approved", rooftop_id=m.rooftop_id, customer_id=m.customer_id,
                           message_id=m.id, approver=approver, batch_id=batch_id, content_hash=m.approved_hash)
            approved.append(m)
        return approved

    def approve_batch(self, approver: str, rooftop_id: str | None = None) -> list[OutboundMessage]:
        if self.ctx.settings.approval.mode != "batch":
            raise ApprovalError("Batch approval is disabled (approval.mode = 'every_send')")
        ids = [m.id for m in self.pending() if (rooftop_id is None or m.rooftop_id == rooftop_id)
               and not self.blocking_problems(m)]
        return self.approve(ids, approver) if ids else []

    def edit(self, message_id: str, *, body: str | None = None, subject: str | None = None) -> OutboundMessage:
        m = self.ctx.store.get_message(message_id)
        if m is None or m.status not in (MessageStatus.PENDING_APPROVAL, MessageStatus.APPROVED):
            raise ApprovalError(f"Message {message_id} cannot be edited")
        if body is not None:
            m.body = body
        if subject is not None:
            m.subject = subject
        m.approved_hash = None
        m.approved_at = None
        m.approved_by = None
        m.status = MessageStatus.PENDING_APPROVAL
        self.ctx.store.save_message(m)
        self.ctx.audit("message_edited", rooftop_id=m.rooftop_id, customer_id=m.customer_id, message_id=m.id)
        return m

    def reject(self, message_id: str, reason: str) -> OutboundMessage:
        m = self.ctx.store.get_message(message_id)
        if m is None or m.status not in (MessageStatus.PENDING_APPROVAL, MessageStatus.APPROVED):
            raise ApprovalError(f"Message {message_id} cannot be rejected")
        m.status = MessageStatus.REJECTED
        m.error = reason
        self.ctx.store.save_message(m)
        self.ctx.audit("message_rejected", rooftop_id=m.rooftop_id, customer_id=m.customer_id,
                       message_id=m.id, reason=reason)
        return m
