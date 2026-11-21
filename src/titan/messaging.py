from __future__ import annotations

import re
from dataclasses import dataclass
from datetime import datetime, timedelta
from typing import Protocol

from titan.config import MessagingConfig
from titan.models import CustomerProfile, OpportunityType
from titan.policy.send_window import SendWindow

INCENTIVE_LANGUAGE = "all possible qualifying incentives, rebates, and special manufacturer APR programs"

FORBIDDEN_PATTERNS: tuple[tuple[str, str], ...] = (
    (r"\bguarantee[ds]?\b", "guarantee"),
    (r"\byou(?:'re| are)? (?:pre-?)?approved\b", "approval promise"),
    (r"\byou qualify\b", "eligibility promise"),
    (r"\$\s?\d", "specific dollar amount"),
    (r"\d+(?:\.\d+)?\s?%", "specific rate/percentage"),
    (r"\bin stock\b|\bon (?:the|our) lot\b|\bwe have (?:it|one|the)\b", "availability claim"),
    (r"\bper month\b|\b/mo\b|\bmonthly payment of\b", "payment promise"),
    (r"\bworth\b.*\btrade\b|\btrade\b.*\bworth\b", "appraisal value promise"),
)

PURPOSE = {
    OpportunityType.DIRECT_REPLY: "visit",
    OpportunityType.NEW_LEAD: "vehicle-selection visit",
    OpportunityType.TRADE_APPRAISAL: "complimentary vehicle appraisal",
    OpportunityType.LEASE_MATURITY: "complimentary lease-end and ownership review",
    OpportunityType.MISSED_APPOINTMENT: "rescheduled visit",
    OpportunityType.RECENT_INQUIRY: "vehicle-selection visit",
    OpportunityType.DORMANT_LEAD: "vehicle-selection visit",
    OpportunityType.PAST_OWNER: "complimentary appraisal and upgrade review",
    OpportunityType.EQUITY: "complimentary equity and appraisal review",
    OpportunityType.BIRTHDAY: "complimentary appraisal and upgrade review",
    OpportunityType.VEHICLE_ANNIVERSARY: "complimentary ownership and appraisal review",
}


@dataclass(frozen=True)
class Draft:
    email_subject: str
    email_body: str
    sms_body: str
    slots: tuple[datetime, datetime]
    purpose: str


@dataclass(frozen=True)
class LintResult:
    ok: bool
    problems: tuple[str, ...]


def propose_slots(window: SendWindow, now: datetime) -> tuple[datetime, datetime]:
    """Two future appointment choices on different business days, inside dealership hours."""
    first_day = window.add_working_days(now, 1)
    second_day = window.add_working_days(first_day, 1)
    a = window.clamp_to_business_hours(first_day.replace(hour=11, minute=0, second=0, microsecond=0))
    hours = window.cfg.dealership_hours[second_day.weekday()]
    late_hour = min(16, hours[1].hour - 1)
    b = window.clamp_to_business_hours(second_day.replace(hour=late_hour, minute=30, second=0, microsecond=0))
    return a, b


def fmt_slot(dt: datetime) -> str:
    return f"{dt:%A} {dt:%B} {dt.day} at {dt.strftime('%I:%M %p').lstrip('0')}"


class Composer(Protocol):
    def compose(self, profile: CustomerProfile, rooftop_name: str, sender: str,
                slots: tuple[datetime, datetime]) -> Draft: ...


class TemplateComposer:
    """Deterministic, history-aware composer. An LLM composer can replace it; output still goes through lint()."""

    def compose(self, profile: CustomerProfile, rooftop_name: str, sender: str,
                slots: tuple[datetime, datetime]) -> Draft:
        purpose = PURPOSE[profile.opportunity_type]
        first = profile.first_name.strip().title() or "there"
        sender_first = sender.split(",")[-1].strip() if "," in sender else sender
        s1, s2 = fmt_slot(slots[0]), fmt_slot(slots[1])
        vehicle = profile.vehicle_of_interest
        owned = profile.owned_vehicle
        trade = profile.trade.describe()

        opener = self._opener(profile, first, rooftop_name, vehicle, owned)
        detail = self._detail(profile, vehicle, owned, trade)
        incentives = (
            f" We will also review {INCENTIVE_LANGUAGE} with you." if vehicle else ""
        )
        ask = f"Would {s1} or {s2} work better for a {purpose}?"

        email_body = (
            f"Hi {first},\n\n{opener} {detail}{incentives}\n\n{ask} "
            f"Reply with the time that suits you and I will have everything ready.\n\n"
            f"{sender_first}\n{rooftop_name}"
        )
        subject = self._subject(profile, vehicle, owned, rooftop_name)
        sms_core = f"Hi {first}, it's {sender_first} at {rooftop_name}. "
        sms_topic = self._sms_topic(profile, vehicle, owned)
        sms_body = f"{sms_core}{sms_topic} Would {s1} or {s2} work for a quick {purpose}? Reply STOP to opt out."
        return Draft(subject, email_body, sms_body, slots, purpose)

    @staticmethod
    def _opener(p: CustomerProfile, first: str, rooftop: str, vehicle: str | None, owned: str | None) -> str:
        t = p.opportunity_type
        if t is OpportunityType.DIRECT_REPLY and p.latest_customer_message:
            return "Thank you for getting back to me. [ANSWER CUSTOMER'S QUESTION HERE]"
        if t is OpportunityType.NEW_LEAD:
            return f"Thank you for reaching out to {rooftop}" + (f" about the {vehicle}." if vehicle else ".")
        if t is OpportunityType.MISSED_APPOINTMENT:
            return "I am sorry we missed you on your last visit and would be glad to set a new time."
        if t is OpportunityType.LEASE_MATURITY:
            return "Your lease is approaching its maturity window, and it is a good time to review your options."
        if t in (OpportunityType.PAST_OWNER, OpportunityType.EQUITY):
            return f"I hope your {owned or 'vehicle'} has been treating you well."
        if t is OpportunityType.BIRTHDAY:
            return "Happy birthday from all of us!"
        if t is OpportunityType.VEHICLE_ANNIVERSARY:
            return f"Happy ownership anniversary with your {owned or 'vehicle'}!"
        if t is OpportunityType.TRADE_APPRAISAL:
            return "Thank you for your interest in a trade appraisal."
        return "I wanted to follow up on your recent interest" + (f" in the {vehicle}." if vehicle else ".")

    @staticmethod
    def _detail(p: CustomerProfile, vehicle: str | None, owned: str | None, trade: str | None) -> str:
        if trade:
            return f"I can set up a complimentary appraisal of your {trade} while you are here."
        if p.opportunity_type in (OpportunityType.PAST_OWNER, OpportunityType.EQUITY,
                                  OpportunityType.BIRTHDAY, OpportunityType.VEHICLE_ANNIVERSARY):
            return "A complimentary appraisal shows where you stand if an upgrade makes sense."
        if vehicle:
            return "We have multiple options across our stores, and I can line up the best fits before you arrive."
        return "I can line up options before you arrive so your visit is quick."

    @staticmethod
    def _subject(p: CustomerProfile, vehicle: str | None, owned: str | None, rooftop: str) -> str:
        t = p.opportunity_type
        if t is OpportunityType.NEW_LEAD and vehicle:
            return f"Your {vehicle} inquiry at {rooftop}"
        if t in (OpportunityType.PAST_OWNER, OpportunityType.EQUITY, OpportunityType.VEHICLE_ANNIVERSARY):
            return f"Complimentary appraisal for your {owned or 'vehicle'}"
        if t is OpportunityType.BIRTHDAY:
            return "Happy birthday from " + rooftop
        if t is OpportunityType.DIRECT_REPLY:
            return "Re: your question"
        return f"Following up from {rooftop}"

    @staticmethod
    def _sms_topic(p: CustomerProfile, vehicle: str | None, owned: str | None) -> str:
        if p.opportunity_type is OpportunityType.DIRECT_REPLY:
            return "Thanks for your reply. [ANSWER]"
        if p.trade.describe():
            return f"I can do a free appraisal on your {p.trade.describe()}."
        if p.opportunity_type in (OpportunityType.PAST_OWNER, OpportunityType.EQUITY,
                                  OpportunityType.BIRTHDAY, OpportunityType.VEHICLE_ANNIVERSARY):
            return f"I can do a free appraisal on your {owned or 'vehicle'}."
        if vehicle:
            return f"Thanks for your interest in the {vehicle}."
        return "Following up on your recent interest."


class MessageLinter:
    def __init__(self, cfg: MessagingConfig):
        self.cfg = cfg

    def lint_email(self, subject: str, body: str, slots: tuple[datetime, datetime],
                   vehicle_specific: bool) -> LintResult:
        problems = self._common(body, slots)
        words = len(re.findall(r"\b\w[\w'-]*\b", body))
        if words > self.cfg.email_max_words:
            problems.append(f"Email is {words} words (limit {self.cfg.email_max_words})")
        if not subject.strip():
            problems.append("Missing subject")
        if vehicle_specific and INCENTIVE_LANGUAGE not in body:
            problems.append("Vehicle-specific email must use the approved incentives sentence")
        return LintResult(not problems, tuple(problems))

    def lint_sms(self, body: str, slots: tuple[datetime, datetime]) -> LintResult:
        problems = self._common(body, slots)
        if len(body) > self.cfg.sms_max_chars:
            problems.append(f"SMS is {len(body)} chars (limit {self.cfg.sms_max_chars})")
        if "stop" not in body.lower():
            problems.append("SMS must include opt-out language")
        return LintResult(not problems, tuple(problems))

    @staticmethod
    def _common(body: str, slots: tuple[datetime, datetime]) -> list[str]:
        problems: list[str] = []
        scrubbed = body.replace(INCENTIVE_LANGUAGE, "")
        for slot in slots:
            scrubbed = scrubbed.replace(fmt_slot(slot), "")
        for pattern, label in FORBIDDEN_PATTERNS:
            if re.search(pattern, scrubbed, re.IGNORECASE):
                problems.append(f"Unsupported promise: {label}")
        missing = [fmt_slot(s) for s in slots if fmt_slot(s) not in body]
        if missing:
            problems.append("Must offer two specific appointment choices")
        if body.count("?") != 1:
            problems.append("Must contain exactly one appointment ask")
        if "[ANSWER" in body:
            problems.append("Needs Glenn's answer to the customer's question before approval")
        return problems


def follow_up_gap(window: SendWindow, now: datetime, active: bool) -> datetime:
    """Next-day follow-up for active opportunities; 4 working days for older inactive ones."""
    days = 1 if active else 4
    due = window.add_working_days(now, days).replace(hour=10, minute=0, second=0, microsecond=0)
    return window.clamp_to_business_hours(due)


def confirmation_time(window: SendWindow, appointment_at: datetime, now: datetime, lead_hours: int) -> datetime:
    target = appointment_at - timedelta(hours=lead_hours)
    if target <= now:
        target = max(now + timedelta(minutes=15), appointment_at - timedelta(hours=2))
        if target >= appointment_at:
            target = now + timedelta(minutes=5)
        return target
    return window.clamp_to_business_hours(target) if window.clamp_to_business_hours(target) < appointment_at else target
