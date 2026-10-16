from __future__ import annotations

from datetime import date, timedelta

import pytest

from conftest import ME, MITSU, NISSAN, at, profile
from titan.config import Settings
from titan.models import Activity, ActivityKind, Channel, Disposition, OpportunityType, Permissions
from titan.policy.eligibility import EligibilityEngine
from titan.policy.identity import IdentityGuard, ScopeViolation
from titan.policy.send_window import SendWindow


@pytest.fixture
def window():
    return SendWindow(Settings().time)


@pytest.fixture
def engine():
    s = Settings()
    return EligibilityEngine(s.cadence, IdentityGuard(s.identity))


class TestSendWindow:
    def test_weekday_bounds(self, window):
        assert window.is_open(Channel.EMAIL, at(2026, 10, 15, 9, 0))[0]
        assert window.is_open(Channel.SMS, at(2026, 10, 15, 18, 59))[0]
        assert not window.is_open(Channel.SMS, at(2026, 10, 15, 19, 0))[0]
        assert not window.is_open(Channel.EMAIL, at(2026, 10, 15, 8, 59))[0]

    def test_saturday_closes_at_six(self, window):
        assert window.is_open(Channel.EMAIL, at(2026, 10, 17, 17, 59))[0]
        assert not window.is_open(Channel.EMAIL, at(2026, 10, 17, 18, 0))[0]

    def test_sunday_closed(self, window):
        assert not window.is_open(Channel.EMAIL, at(2026, 10, 18, 12, 0))[0]

    def test_after_hours_releases_next_morning(self, window):
        assert window.next_opening(Channel.SMS, at(2026, 10, 15, 21, 30)) == at(2026, 10, 16, 9, 0)
        assert window.next_opening(Channel.EMAIL, at(2026, 10, 17, 19, 0)) == at(2026, 10, 19, 9, 0)
        assert window.next_opening(Channel.EMAIL, at(2026, 10, 15, 7, 0)) == at(2026, 10, 15, 9, 0)

    def test_sms_quiet_hours_still_enforced_if_send_window_widened(self):
        s = Settings()
        s.time.send_window_start = s.time.sms_window_start.replace(hour=6)
        s.time.dealership_hours = {d: (s.time.send_window_start, s.time.sms_window_end) for d in range(7)}
        w = SendWindow(s.time)
        assert w.is_open(Channel.EMAIL, at(2026, 10, 15, 6, 30))[0]
        assert not w.is_open(Channel.SMS, at(2026, 10, 15, 6, 30))[0]


class TestIdentity:
    def test_tko_autogroup_blocked(self):
        g = IdentityGuard(Settings().identity)
        with pytest.raises(ScopeViolation):
            g.check_rooftop("6220")
        with pytest.raises(ScopeViolation):
            g.check_rooftop("99999")
        g.check_rooftop(NISSAN)
        g.check_rooftop(MITSU)

    def test_wrong_user_or_rooftop(self):
        g = IdentityGuard(Settings().identity)
        with pytest.raises(ScopeViolation):
            g.verify_session(expected_rooftop=NISSAN, user_name="Smith, Pat", rooftop_id=NISSAN)
        with pytest.raises(ScopeViolation):
            g.verify_session(expected_rooftop=NISSAN, user_name=ME, rooftop_id=MITSU)
        g.verify_session(expected_rooftop=NISSAN, user_name=ME, rooftop_id=NISSAN)

    def test_config_rejects_overlap(self):
        s = Settings()
        s.identity.allowed_rooftops["6220"] = "TKO"
        with pytest.raises(ValueError):
            s.validate()


NOW = at(2026, 10, 15, 10, 0)


class TestEligibility:
    def test_qualified_new_lead_gets_all_channels(self, engine):
        d = engine.evaluate(profile(), NOW)
        assert d.qualified
        assert d.channels == {Channel.EMAIL, Channel.SMS, Channel.PHONE}

    def test_history_not_loaded_requires_manual_verification(self, engine):
        d = engine.evaluate(profile(history_loaded=False), NOW)
        assert d.disposition is Disposition.MANUAL_VERIFICATION

    @pytest.mark.parametrize("perms,expected", [
        (Permissions(email_valid=True, phone_valid=True, sms_opt_in=True, sms_stop=True), {Channel.EMAIL, Channel.PHONE}),
        (Permissions(email_valid=True, phone_valid=True, sms_opt_in=True, do_not_text=True), {Channel.EMAIL, Channel.PHONE}),
        (Permissions(email_valid=True, phone_valid=True, sms_opt_in=True, email_opt_out=True), {Channel.SMS, Channel.PHONE}),
        (Permissions(email_valid=True, phone_valid=True, sms_opt_in=True, email_hard_bounce=True,
                     do_not_call=True), {Channel.SMS}),
        (Permissions(email_valid=True, phone_valid=True, sms_opt_in=False), {Channel.EMAIL, Channel.PHONE}),
    ])
    def test_channel_permissions(self, engine, perms, expected):
        assert engine.evaluate(profile(permissions=perms), NOW).channels == expected

    def test_no_permitted_channel_excluded(self, engine):
        perms = Permissions(email_valid=True, phone_valid=True, email_opt_out=True, sms_stop=True, do_not_call=True)
        d = engine.evaluate(profile(permissions=perms), NOW)
        assert d.disposition is Disposition.EXCLUDED

    @pytest.mark.parametrize("flag", ["removal_requested", "sold_elsewhere", "not_in_market"])
    def test_do_not_pursue(self, engine, flag):
        assert engine.evaluate(profile(**{flag: True}), NOW).disposition is Disposition.EXCLUDED

    def test_other_consultant_held_without_storewide_authorization(self, engine):
        d = engine.evaluate(profile(assigned_salesperson="Smith, Pat"), NOW)
        assert d.disposition is Disposition.HELD

    def test_72_hour_rule_uses_exact_timestamp(self):
        s = Settings()
        s.identity.storewide_authorized = True
        e = EligibilityEngine(s.cadence, IdentityGuard(s.identity))
        inside = Activity(ActivityKind.SMS_IN, NOW - timedelta(hours=71, minutes=59), "customer")
        outside = Activity(ActivityKind.SMS_IN, NOW - timedelta(hours=72, minutes=1), "customer")
        assert e.evaluate(profile(assigned_salesperson="Smith, Pat", activities=[inside]), NOW).disposition \
            is Disposition.HELD
        assert e.evaluate(profile(assigned_salesperson="Smith, Pat", activities=[outside]), NOW).qualified

    def test_own_customer_min_interval(self, engine):
        recent = Activity(ActivityKind.EMAIL_OUT, NOW - timedelta(hours=3), ME)
        assert engine.evaluate(profile(activities=[recent]), NOW).disposition is Disposition.HELD

    def test_customer_reply_overrides_interval(self, engine):
        acts = [Activity(ActivityKind.EMAIL_OUT, NOW - timedelta(hours=3), ME),
                Activity(ActivityKind.SMS_IN, NOW - timedelta(hours=1), "customer")]
        d = engine.evaluate(profile(activities=acts, otype=OpportunityType.DIRECT_REPLY), NOW)
        assert d.qualified

    def test_14_month_past_purchase_screen(self, engine):
        recent = profile(otype=OpportunityType.PAST_OWNER, last_purchase_date=date(2025, 12, 1))
        old = profile(otype=OpportunityType.PAST_OWNER, last_purchase_date=date(2025, 8, 1))
        assert engine.evaluate(recent, NOW).disposition is Disposition.EXCLUDED
        assert engine.evaluate(old, NOW).qualified

    def test_recent_inquiry_overrides_purchase_timing(self, engine):
        p = profile(otype=OpportunityType.PAST_OWNER, last_purchase_date=date(2026, 6, 1),
                    recent_inquiry_at=NOW - timedelta(days=2))
        assert engine.evaluate(p, NOW).qualified

    def test_unknown_purchase_date_is_not_assumed(self, engine):
        p = profile(otype=OpportunityType.PAST_OWNER, owned_vehicle="2020 Nissan Kicks")
        assert engine.evaluate(p, NOW).disposition is Disposition.MANUAL_VERIFICATION

    def test_funding_note_bypass_unless_later_inquiry(self, engine):
        note = Activity(ActivityKind.NOTE, NOW - timedelta(days=20), ME, "Unable to obtain funding")
        assert engine.evaluate(profile(otype=OpportunityType.DORMANT_LEAD, activities=[note]), NOW).disposition \
            is Disposition.EXCLUDED
        later = profile(otype=OpportunityType.DORMANT_LEAD, activities=[note],
                        recent_inquiry_at=NOW - timedelta(days=1))
        assert engine.evaluate(later, NOW).qualified
        inbound = Activity(ActivityKind.EMAIL_IN, NOW - timedelta(days=1), "customer", "Ready to look again")
        undated = profile(otype=OpportunityType.DORMANT_LEAD, notes=["Unable to obtain funding"],
                          activities=[inbound])
        assert engine.evaluate(undated, NOW).qualified
        still_out = profile(otype=OpportunityType.DORMANT_LEAD, notes=["Unable to obtain funding"])
        assert engine.evaluate(still_out, NOW).disposition is Disposition.EXCLUDED

    def test_reengagement_48h_cycle(self, engine):
        s = Settings()
        s.cadence.own_customer_min_interval_hours = 1
        e = EligibilityEngine(s.cadence, IdentityGuard(s.identity))
        p = profile(otype=OpportunityType.DORMANT_LEAD,
                    activities=[Activity(ActivityKind.SMS_OUT, NOW - timedelta(hours=30), ME)])
        assert e.evaluate(p, NOW).disposition is Disposition.HELD
        p.activities = [Activity(ActivityKind.SMS_OUT, NOW - timedelta(hours=49), ME)]
        assert e.evaluate(p, NOW).qualified
