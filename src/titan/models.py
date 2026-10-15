from __future__ import annotations

import hashlib
import uuid
from dataclasses import dataclass, field
from datetime import date, datetime
from enum import Enum


def new_id() -> str:
    return uuid.uuid4().hex


class Channel(str, Enum):
    EMAIL = "email"
    SMS = "sms"
    PHONE = "phone"


class OpportunityType(str, Enum):
    """Ordered by the PDF priority table (lower rank = handled first)."""

    DIRECT_REPLY = "direct_reply"
    NEW_LEAD = "new_lead"
    TRADE_APPRAISAL = "trade_appraisal"
    LEASE_MATURITY = "lease_maturity"
    MISSED_APPOINTMENT = "missed_appointment"
    RECENT_INQUIRY = "recent_inquiry"
    DORMANT_LEAD = "dormant_lead"
    PAST_OWNER = "past_owner"
    EQUITY = "equity"
    BIRTHDAY = "birthday"
    VEHICLE_ANNIVERSARY = "vehicle_anniversary"

    @property
    def rank(self) -> int:
        return {
            OpportunityType.DIRECT_REPLY: 1,
            OpportunityType.NEW_LEAD: 2,
            OpportunityType.TRADE_APPRAISAL: 3,
            OpportunityType.LEASE_MATURITY: 3,
            OpportunityType.MISSED_APPOINTMENT: 3,
            OpportunityType.RECENT_INQUIRY: 3,
            OpportunityType.DORMANT_LEAD: 4,
            OpportunityType.PAST_OWNER: 5,
            OpportunityType.EQUITY: 5,
            OpportunityType.BIRTHDAY: 6,
            OpportunityType.VEHICLE_ANNIVERSARY: 6,
        }[self]


class ActivityKind(str, Enum):
    EMAIL_OUT = "email_out"
    EMAIL_IN = "email_in"
    EMAIL_BOUNCE = "email_bounce"
    SMS_OUT = "sms_out"
    SMS_IN = "sms_in"
    CALL_OUT = "call_out"
    CALL_IN = "call_in"
    NOTE = "note"
    APPOINTMENT = "appointment"

    @property
    def is_qualifying_contact(self) -> bool:
        """Activities that count toward the 72-hour cross-consultant hold."""
        return self is not ActivityKind.EMAIL_BOUNCE


@dataclass(frozen=True)
class Activity:
    kind: ActivityKind
    at: datetime
    by: str
    summary: str = ""
    crm_activity_id: str | None = None


@dataclass(frozen=True)
class Permissions:
    email_valid: bool = False
    phone_valid: bool = False
    email_opt_out: bool = False
    email_hard_bounce: bool = False
    sms_opt_in: bool = False
    sms_stop: bool = False
    do_not_call: bool = False
    do_not_text: bool = False
    do_not_contact: bool = False


@dataclass(frozen=True)
class TradeInfo:
    year: int | None = None
    make: str | None = None
    model: str | None = None
    mileage: int | None = None
    payoff: float | None = None
    appraisal_requested: bool = False

    def describe(self) -> str | None:
        parts = [str(p) for p in (self.year, self.make, self.model) if p]
        return " ".join(parts) or None


@dataclass
class CustomerProfile:
    """Snapshot read from the CRM for one unit of work. Never cached beyond that unit."""

    customer_id: str
    opportunity_id: str
    rooftop_id: str
    full_name: str
    first_name: str
    assigned_salesperson: str
    opportunity_type: OpportunityType
    email: str | None = None
    phone: str | None = None
    permissions: Permissions = field(default_factory=Permissions)
    vehicle_of_interest: str | None = None
    vehicle_condition: str | None = None
    owned_vehicle: str | None = None
    last_purchase_date: date | None = None
    trade: TradeInfo = field(default_factory=TradeInfo)
    recent_inquiry_at: datetime | None = None
    latest_customer_message: str | None = None
    activities: list[Activity] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)
    history_loaded: bool = False
    sold_elsewhere: bool = False
    not_in_market: bool = False
    removal_requested: bool = False
    lead_assigned_at: datetime | None = None

    def last_activity(self, *kinds: ActivityKind) -> Activity | None:
        matches = [a for a in self.activities if not kinds or a.kind in kinds]
        return max(matches, key=lambda a: a.at) if matches else None


class Disposition(str, Enum):
    QUALIFIED = "qualified"
    HELD = "held"
    EXCLUDED = "excluded"
    MANUAL_VERIFICATION = "manual_verification_required"


@dataclass(frozen=True)
class EligibilityDecision:
    disposition: Disposition
    reasons: tuple[str, ...]
    channels: frozenset[Channel] = frozenset()

    @property
    def qualified(self) -> bool:
        return self.disposition is Disposition.QUALIFIED and bool(self.channels)


class MessageStatus(str, Enum):
    PENDING_APPROVAL = "pending_approval"
    APPROVED = "approved"
    REJECTED = "rejected"
    SENDING = "sending"
    SEND_UNCERTAIN = "send_uncertain"
    SENT_VERIFIED = "sent_verified"
    COMPLETED = "completed"
    BLOCKED = "blocked"
    FAILED = "failed"
    CANCELLED = "cancelled"


class MessageSource(str, Enum):
    NEW_LEAD = "new_lead"
    REENGAGEMENT = "reengagement"
    FOLLOW_UP = "follow_up"
    REPLY = "reply"


@dataclass
class OutboundMessage:
    customer_id: str
    opportunity_id: str
    rooftop_id: str
    channel: Channel
    recipient: str
    body: str
    source: MessageSource
    subject: str | None = None
    id: str = field(default_factory=new_id)
    status: MessageStatus = MessageStatus.PENDING_APPROVAL
    created_at: datetime | None = None
    detected_at: datetime | None = None
    approved_at: datetime | None = None
    approved_by: str | None = None
    approved_hash: str | None = None
    batch_id: str | None = None
    sent_at: datetime | None = None
    verified_at: datetime | None = None
    completed_at: datetime | None = None
    crm_activity_id: str | None = None
    crm_task_id: str | None = None
    error: str | None = None
    customer_name: str = ""

    @property
    def content_hash(self) -> str:
        payload = "\x1f".join(
            [self.rooftop_id, self.customer_id, self.channel.value, self.recipient, self.subject or "", self.body]
        )
        return hashlib.sha256(payload.encode()).hexdigest()

    @property
    def approval_is_valid(self) -> bool:
        return self.approved_hash is not None and self.approved_hash == self.content_hash


class TaskKind(str, Enum):
    FOLLOW_UP_EMAIL = "follow_up_email"
    FOLLOW_UP_CALL = "follow_up_call"
    FOLLOW_UP_SMS = "follow_up_sms"
    CONFIRMATION_CALL = "confirmation_call"
    REENGAGEMENT = "reengagement"


class TaskStatus(str, Enum):
    SCHEDULED = "scheduled"
    CRM_CREATED = "crm_created"
    DONE = "done"
    CANCELLED = "cancelled"


@dataclass
class PlannedTask:
    customer_id: str
    opportunity_id: str
    rooftop_id: str
    kind: TaskKind
    due_at: datetime
    priority: int
    reason: str
    id: str = field(default_factory=new_id)
    status: TaskStatus = TaskStatus.SCHEDULED
    crm_task_id: str | None = None


@dataclass
class CustomerDigest:
    """Durable per-customer summary + next-best strategy + pre-generated templates."""

    customer_id: str
    rooftop_id: str
    summary: str
    next_best_action: str
    sms_template: str | None
    email_subject: str | None
    email_template: str | None
    disposition: Disposition
    disposition_reasons: tuple[str, ...]
    updated_at: datetime
    last_outreach_at: datetime | None = None


class AppointmentStatus(str, Enum):
    PENDING_APPROVAL = "pending_approval"
    APPROVED = "approved"
    SAVED = "saved"
    REJECTED = "rejected"


@dataclass
class AppointmentProposal:
    customer_id: str
    opportunity_id: str
    rooftop_id: str
    customer_name: str
    starts_at: datetime
    purpose: str
    arrival_notes: str
    confirmation_call_at: datetime
    id: str = field(default_factory=new_id)
    status: AppointmentStatus = AppointmentStatus.PENDING_APPROVAL
    crm_appointment_id: str | None = None
    crm_confirmation_task_id: str | None = None
