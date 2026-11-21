from __future__ import annotations

from dataclasses import replace
from datetime import datetime, timedelta

from titan.config import CadenceConfig
from titan.models import (
    Activity,
    ActivityKind,
    Channel,
    CustomerProfile,
    Disposition,
    EligibilityDecision,
    OpportunityType,
)
from titan.policy.identity import IdentityGuard

FUNDING_PHRASE = "unable to obtain funding"

OUTBOUND_KINDS = (ActivityKind.EMAIL_OUT, ActivityKind.SMS_OUT, ActivityKind.CALL_OUT)
INBOUND_KINDS = (ActivityKind.EMAIL_IN, ActivityKind.SMS_IN, ActivityKind.CALL_IN)
PAST_PURCHASE_TYPES = {
    OpportunityType.PAST_OWNER,
    OpportunityType.EQUITY,
    OpportunityType.BIRTHDAY,
    OpportunityType.VEHICLE_ANNIVERSARY,
}
REENGAGEMENT_TYPES = PAST_PURCHASE_TYPES | {OpportunityType.DORMANT_LEAD}


def _months_between(earlier: datetime, later: datetime) -> int:
    months = (later.year - earlier.year) * 12 + (later.month - earlier.month)
    if later.day < earlier.day:
        months -= 1
    return months


def permitted_channels(profile: CustomerProfile) -> tuple[frozenset[Channel], list[str]]:
    p = profile.permissions
    channels: set[Channel] = set()
    notes: list[str] = []
    if p.do_not_contact:
        return frozenset(), ["Do-not-contact flag set"]
    if profile.email and p.email_valid and not p.email_opt_out and not p.email_hard_bounce:
        channels.add(Channel.EMAIL)
    elif profile.email:
        notes.append("Email blocked: " + ", ".join(
            r for r, flag in (("invalid", not p.email_valid), ("opted out", p.email_opt_out),
                              ("hard bounce", p.email_hard_bounce)) if flag
        ))
    if profile.phone and p.phone_valid and p.sms_opt_in and not p.sms_stop and not p.do_not_text:
        channels.add(Channel.SMS)
    elif profile.phone:
        notes.append("SMS blocked: " + ", ".join(
            r for r, flag in (("invalid phone", not p.phone_valid), ("no SMS consent", not p.sms_opt_in),
                              ("STOP received", p.sms_stop), ("do-not-text", p.do_not_text)) if flag
        ))
    if profile.phone and p.phone_valid and not p.do_not_call:
        channels.add(Channel.PHONE)
    return frozenset(channels), notes


def _funding_bypass(profile: CustomerProfile) -> bool:
    funding_notes: list[Activity] = [
        a for a in profile.activities if a.kind is ActivityKind.NOTE and FUNDING_PHRASE in a.summary.lower()
    ]
    undated = any(FUNDING_PHRASE in n.lower() for n in profile.notes)
    if not funding_notes and not undated:
        return False
    latest_funding = max((a.at for a in funding_notes), default=None)
    later_inbound = profile.last_activity(*INBOUND_KINDS)
    if profile.recent_inquiry_at and (latest_funding is None or profile.recent_inquiry_at > latest_funding):
        return False
    # An undated note has no timestamp. Any inbound message means we cannot claim
    # there was no later contact, so the customer stays eligible (spec §6.4).
    if later_inbound and (latest_funding is None or later_inbound.at > latest_funding):
        return False
    return True


class EligibilityEngine:
    """Implements PDF sections 1-2: inspection-before-contact, holds, exclusions, permissions."""

    def __init__(self, cadence: CadenceConfig, guard: IdentityGuard):
        self.cadence = cadence
        self.guard = guard

    def evaluate(self, profile: CustomerProfile, now: datetime,
                 ignore_own_outbound_since: datetime | None = None) -> EligibilityDecision:
        """`ignore_own_outbound_since` lets the send-time re-check skip companion sends from the same approved draft set."""
        self.guard.check_rooftop(profile.rooftop_id)
        reasons: list[str] = []
        if ignore_own_outbound_since is not None:
            profile = replace(profile, activities=[
                a for a in profile.activities
                if not (a.kind in OUTBOUND_KINDS and a.at >= ignore_own_outbound_since and self.guard.is_own(a.by))
            ])

        if not profile.history_loaded:
            return EligibilityDecision(Disposition.MANUAL_VERIFICATION, ("Activity history did not fully load",))

        if profile.removal_requested or profile.permissions.do_not_contact:
            return EligibilityDecision(Disposition.EXCLUDED, ("Customer requested no further contact",))
        if profile.sold_elsewhere:
            return EligibilityDecision(Disposition.EXCLUDED, ("Bought elsewhere and ended search",))
        if profile.not_in_market:
            return EligibilityDecision(Disposition.EXCLUDED, ("Explicitly not in market",))
        if _funding_bypass(profile):
            return EligibilityDecision(
                Disposition.EXCLUDED, ("Notes say 'unable to obtain funding' with no later inquiry",)
            )

        channels, channel_notes = permitted_channels(profile)
        reasons.extend(channel_notes)
        messaging_channels = channels & {Channel.EMAIL, Channel.SMS}
        if not channels:
            return EligibilityDecision(Disposition.EXCLUDED, tuple(reasons + ["No valid permitted contact method"]))

        recent_inquiry = bool(
            profile.recent_inquiry_at
            and now - profile.recent_inquiry_at <= timedelta(days=self.cadence.recent_inquiry_days)
        )
        last_out = profile.last_activity(*OUTBOUND_KINDS)
        last_in = profile.last_activity(*INBOUND_KINDS)
        awaiting_our_reply = bool(last_in and (last_out is None or last_in.at > last_out.at))

        if not self.guard.is_own(profile.assigned_salesperson):
            if not self.guard.cfg.storewide_authorized:
                return EligibilityDecision(
                    Disposition.HELD, (f"Assigned to {profile.assigned_salesperson}; store-wide work not authorized",)
                )
            hold_since = now - timedelta(hours=self.cadence.other_consultant_hold_hours)
            recent = [a for a in profile.activities if a.kind.is_qualifying_contact and a.at >= hold_since]
            if recent:
                latest = max(recent, key=lambda a: a.at)
                return EligibilityDecision(
                    Disposition.HELD,
                    (f"72-hour hold: {latest.kind.value} by {latest.by} at {latest.at:%Y-%m-%d %H:%M}",),
                )
        elif last_out and not awaiting_our_reply:
            spacing = timedelta(hours=self.cadence.own_customer_min_interval_hours)
            if now - last_out.at < spacing:
                return EligibilityDecision(
                    Disposition.HELD,
                    (f"Last outreach {last_out.kind.value} at {last_out.at:%Y-%m-%d %H:%M} is inside the "
                     f"{self.cadence.own_customer_min_interval_hours}h minimum interval",),
                )

        if profile.opportunity_type in PAST_PURCHASE_TYPES and not recent_inquiry:
            if profile.last_purchase_date is None:
                return EligibilityDecision(
                    Disposition.MANUAL_VERIFICATION,
                    ("Purchase date unknown; model year alone never proves ownership duration",),
                )
            purchase_dt = datetime.combine(profile.last_purchase_date, datetime.min.time(), tzinfo=now.tzinfo)
            months = _months_between(purchase_dt, now)
            if months < self.cadence.past_purchase_min_months:
                return EligibilityDecision(
                    Disposition.EXCLUDED,
                    (f"Purchased {months} months ago (< {self.cadence.past_purchase_min_months}-month screen)",),
                )

        if profile.opportunity_type in REENGAGEMENT_TYPES and last_out and not awaiting_our_reply:
            interval = timedelta(hours=self.cadence.reengagement_interval_hours)
            if now - last_out.at < interval:
                return EligibilityDecision(
                    Disposition.HELD,
                    (f"Re-engagement cycle: next attempt after {(last_out.at + interval):%Y-%m-%d %H:%M}",),
                )

        if not messaging_channels:
            reasons.append("Phone-only: schedule call task, no email/SMS")
        if recent_inquiry:
            reasons.append("Recent inquiry overrides purchase-timing screen")
        if awaiting_our_reply:
            reasons.append("Customer is awaiting a reply")
        return EligibilityDecision(Disposition.QUALIFIED, tuple(reasons) or ("All checks passed",), channels)
