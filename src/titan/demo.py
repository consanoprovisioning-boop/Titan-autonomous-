from __future__ import annotations

from datetime import date, datetime, timedelta

from titan.crm.mock import MockCrm
from titan.models import (
    Activity,
    ActivityKind,
    CustomerProfile,
    OpportunityType,
    Permissions,
    TradeInfo,
)

ME = "Bordine, Glenn"
FULL_PERMS = Permissions(email_valid=True, phone_valid=True, sms_opt_in=True)


def seed(crm: MockCrm, now: datetime) -> None:
    """Fictional customers covering the main PDF rules (qualified, held, excluded, manual verification)."""
    crm.add(CustomerProfile(
        "C100", "O100", "28206", "Dana Whitfield", "Dana", ME, OpportunityType.NEW_LEAD,
        email="dana@example.com", phone="+12295550100", permissions=FULL_PERMS,
        vehicle_of_interest="2026 Nissan Rogue SV", vehicle_condition="new",
        trade=TradeInfo(2019, "Honda", "CR-V", 61000, appraisal_requested=True),
        history_loaded=True, lead_assigned_at=now - timedelta(seconds=20),
    ), new_lead=True)
    crm.add(CustomerProfile(
        "C200", "O200", "28546", "Marcus Lee", "Marcus", ME, OpportunityType.NEW_LEAD,
        email="marcus@example.com", phone="+12295550200",
        permissions=Permissions(email_valid=True, phone_valid=True, sms_opt_in=False),
        vehicle_of_interest="2026 Mitsubishi Outlander SE", vehicle_condition="new",
        history_loaded=True, lead_assigned_at=now - timedelta(seconds=5),
    ), new_lead=True)
    crm.add(CustomerProfile(
        "C300", "O300", "28206", "Renee Carter", "Renee", ME, OpportunityType.PAST_OWNER,
        email="renee@example.com", phone="+12295550300", permissions=FULL_PERMS,
        owned_vehicle="2022 Nissan Altima", last_purchase_date=date(now.year - 3, 5, 14),
        activities=[Activity(ActivityKind.EMAIL_OUT, now - timedelta(days=30), ME, "Service reminder")],
        history_loaded=True,
    ), past=True)
    crm.add(CustomerProfile(
        "C400", "O400", "28206", "Tom Briggs", "Tom", ME, OpportunityType.PAST_OWNER,
        email="tom@example.com", phone="+12295550400", permissions=FULL_PERMS,
        owned_vehicle="2025 Nissan Frontier", last_purchase_date=(now - timedelta(days=200)).date(),
        history_loaded=True,
    ), past=True)
    crm.add(CustomerProfile(
        "C500", "O500", "28546", "Alicia Moore", "Alicia", "Smith, Pat", OpportunityType.EQUITY,
        email="alicia@example.com", phone="+12295550500", permissions=FULL_PERMS,
        owned_vehicle="2021 Mitsubishi Eclipse Cross", last_purchase_date=date(now.year - 4, 2, 1),
        history_loaded=True,
    ), past=True)
    crm.add(CustomerProfile(
        "C600", "O600", "28546", "Jordan Price", "Jordan", ME, OpportunityType.DORMANT_LEAD,
        email="jordan@example.com", phone="+12295550600",
        permissions=Permissions(email_valid=True, phone_valid=True, email_opt_out=True, sms_stop=True),
        history_loaded=True,
    ), past=True)
    crm.add(CustomerProfile(
        "C700", "O700", "28206", "Sam Ortiz", "Sam", ME, OpportunityType.BIRTHDAY,
        email="sam@example.com", phone="+12295550700", permissions=FULL_PERMS,
        owned_vehicle="2020 Nissan Kicks", history_loaded=True,
    ), past=True)
