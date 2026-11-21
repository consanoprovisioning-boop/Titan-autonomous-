from __future__ import annotations

from conftest import at, profile
from titan.config import Settings
from titan.messaging import INCENTIVE_LANGUAGE, MessageLinter, TemplateComposer, fmt_slot, propose_slots
from titan.models import OpportunityType, TradeInfo
from titan.policy.send_window import SendWindow

NOW = at(2026, 10, 15, 10, 0)


def _draft(p):
    w = SendWindow(Settings().time)
    slots = propose_slots(w, NOW)
    return TemplateComposer().compose(p, "Valdosta Nissan", "Bordine, Glenn", slots), slots


def test_slots_are_two_future_business_times():
    w = SendWindow(Settings().time)
    a, b = propose_slots(w, NOW)
    assert NOW < a < b
    assert a.date() != b.date()
    for s in (a, b):
        assert w.clamp_to_business_hours(s) == s


def test_new_lead_draft_passes_lint():
    p = profile(trade=TradeInfo(2019, "Honda", "CR-V", appraisal_requested=True))
    d, slots = _draft(p)
    lint = MessageLinter(Settings().messaging)
    assert lint.lint_email(d.email_subject, d.email_body, slots, True).ok
    assert lint.lint_sms(d.sms_body, slots).ok
    assert INCENTIVE_LANGUAGE in d.email_body
    assert fmt_slot(slots[0]) in d.sms_body and fmt_slot(slots[1]) in d.sms_body
    assert "2019 Honda CR-V" in d.email_body
    assert "Glenn" in d.email_body and "Bordine" not in d.email_body


def test_every_opportunity_type_produces_clean_draft():
    lint = MessageLinter(Settings().messaging)
    for otype in OpportunityType:
        if otype is OpportunityType.DIRECT_REPLY:
            continue
        d, slots = _draft(profile(otype=otype, owned_vehicle="2021 Nissan Altima", vehicle_of_interest=None))
        assert lint.lint_email(d.email_subject, d.email_body, slots, False).ok, otype
        assert lint.lint_sms(d.sms_body, slots).ok, otype


def test_direct_reply_requires_glenns_answer():
    d, slots = _draft(profile(otype=OpportunityType.DIRECT_REPLY, latest_customer_message="Is it AWD?"))
    result = MessageLinter(Settings().messaging).lint_email(d.email_subject, d.email_body, slots, True)
    assert not result.ok
    assert any("answer" in p.lower() for p in result.problems)


def test_lint_catches_promises_and_length():
    _, slots = _draft(profile())
    lint = MessageLinter(Settings().messaging)
    ask = f"Would {fmt_slot(slots[0])} or {fmt_slot(slots[1])} work?"
    for bad in ("You are approved for financing.", "Your payment will be $299 per month.",
                "We have it in stock.", "We guarantee 0.9% APR."):
        r = lint.lint_email("s", f"{bad} {ask}", slots, False)
        assert not r.ok, bad
    long_body = ("word " * 130) + ask
    assert not lint.lint_email("s", long_body, slots, False).ok
    assert not lint.lint_sms(f"Hi. {ask}", slots).ok  # missing STOP
