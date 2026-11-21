from __future__ import annotations

from datetime import datetime

from titan.agents.context import AgentContext
from titan.messaging import propose_slots
from titan.models import (
    ActivityKind,
    Channel,
    CustomerProfile,
    Disposition,
    EligibilityDecision,
    MessageSource,
    MessageStatus,
    OpportunityType,
    OutboundMessage,
)
from titan.policy.eligibility import INBOUND_KINDS, OUTBOUND_KINDS


def prepare_outreach(ctx: AgentContext, profile: CustomerProfile, decision: EligibilityDecision,
                     source: MessageSource, detected_at: datetime | None = None) -> list[OutboundMessage]:
    """Create approval-ready drafts (one per permitted messaging channel). Never sends."""
    if not decision.qualified:
        return []
    now = ctx.clock.now()
    slots = propose_slots(ctx.window, now)
    draft = ctx.composer.compose(profile, ctx.guard.rooftop_name(profile.rooftop_id), ctx.me, slots)
    created: list[OutboundMessage] = []
    vehicle_specific = bool(profile.vehicle_of_interest)

    for channel in (Channel.EMAIL, Channel.SMS):
        if channel not in decision.channels:
            continue
        if ctx.store.open_message_for(profile.customer_id, channel):
            ctx.audit("draft_skipped_duplicate", rooftop_id=profile.rooftop_id,
                      customer_id=profile.customer_id, channel=channel.value)
            continue
        if channel is Channel.EMAIL:
            lint = ctx.linter.lint_email(draft.email_subject, draft.email_body, slots, vehicle_specific)
            msg = OutboundMessage(profile.customer_id, profile.opportunity_id, profile.rooftop_id, channel,
                                  profile.email or "", draft.email_body, source, subject=draft.email_subject)
        else:
            lint = ctx.linter.lint_sms(draft.sms_body, slots)
            msg = OutboundMessage(profile.customer_id, profile.opportunity_id, profile.rooftop_id, channel,
                                  profile.phone or "", draft.sms_body, source)
        msg.created_at = now
        msg.detected_at = detected_at
        msg.customer_name = profile.full_name
        msg.status = MessageStatus.PENDING_APPROVAL
        msg.error = "; ".join(lint.problems) or None
        ctx.store.save_message(msg)
        ctx.audit("draft_created", rooftop_id=profile.rooftop_id, customer_id=profile.customer_id,
                  message_id=msg.id, channel=channel.value, source=source.value, lint=list(lint.problems))
        created.append(msg)
    return created


def summarize(profile: CustomerProfile, decision: EligibilityDecision, rooftop_name: str) -> str:
    last_out = profile.last_activity(*OUTBOUND_KINDS)
    last_in = profile.last_activity(*INBOUND_KINDS)
    lines = [
        f"{profile.full_name} | {rooftop_name} | {profile.opportunity_type.value} | assigned {profile.assigned_salesperson}",
    ]
    if profile.owned_vehicle:
        purchased = f", purchased {profile.last_purchase_date:%Y-%m-%d}" if profile.last_purchase_date else ""
        lines.append(f"Owns: {profile.owned_vehicle}{purchased}")
    if profile.vehicle_of_interest:
        lines.append(f"Interested in: {profile.vehicle_of_interest} ({profile.vehicle_condition or 'n/a'})")
    if profile.trade.describe():
        t = profile.trade
        extra = ", ".join(x for x in (f"{t.mileage:,} mi" if t.mileage else "", "appraisal requested"
                                      if t.appraisal_requested else "") if x)
        lines.append(f"Trade: {t.describe()}" + (f" ({extra})" if extra else ""))
    if last_out:
        lines.append(f"Last outbound: {last_out.kind.value} {last_out.at:%Y-%m-%d %H:%M} by {last_out.by}")
    if last_in:
        lines.append(f"Last inbound: {last_in.kind.value} {last_in.at:%Y-%m-%d %H:%M}")
    if profile.recent_inquiry_at:
        lines.append(f"Recent inquiry: {profile.recent_inquiry_at:%Y-%m-%d}")
    channels = ", ".join(sorted(c.value for c in decision.channels)) or "none"
    lines.append(f"Permitted channels: {channels}")
    lines.append(f"Status: {decision.disposition.value} - {'; '.join(decision.reasons)}")
    return "\n".join(lines)


def next_best_action(profile: CustomerProfile, decision: EligibilityDecision) -> str:
    if decision.disposition is Disposition.EXCLUDED:
        return "Do not contact. " + decision.reasons[0]
    if decision.disposition is Disposition.MANUAL_VERIFICATION:
        return "Manual verification required: " + decision.reasons[0]
    if decision.disposition is Disposition.HELD:
        return "Hold: " + decision.reasons[0]
    last_in = profile.last_activity(*INBOUND_KINDS)
    last_out = profile.last_activity(*OUTBOUND_KINDS)
    if last_in and (not last_out or last_in.at > last_out.at):
        return "Answer the customer's latest message first, then offer two appointment times."
    t = profile.opportunity_type
    if profile.trade.appraisal_requested or t is OpportunityType.TRADE_APPRAISAL:
        return "Invite for complimentary appraisal of the trade; confirm payoff and decision-makers."
    if t in (OpportunityType.PAST_OWNER, OpportunityType.EQUITY, OpportunityType.VEHICLE_ANNIVERSARY,
             OpportunityType.BIRTHDAY):
        return "Ownership check-in with complimentary appraisal/upgrade review invitation."
    if t is OpportunityType.LEASE_MATURITY:
        return "Lease-end review: options before maturity, appraisal of current vehicle."
    if t is OpportunityType.MISSED_APPOINTMENT:
        return "Acknowledge the missed visit and offer two new times."
    if Channel.SMS not in decision.channels and Channel.EMAIL not in decision.channels:
        return "Phone call only (no email/SMS permission)."
    return "Vehicle-selection visit with two specific appointment choices."


def has_inbound_since(profile: CustomerProfile, since: datetime) -> bool:
    return any(a.kind in INBOUND_KINDS and a.at > since for a in profile.activities)


def matching_outbound(profile_activities, channel: Channel, since: datetime, by: str):
    kind = ActivityKind.EMAIL_OUT if channel is Channel.EMAIL else ActivityKind.SMS_OUT
    hits = [a for a in profile_activities if a.kind is kind and a.at >= since and a.by.lower() == by.lower()]
    return max(hits, key=lambda a: a.at) if hits else None
