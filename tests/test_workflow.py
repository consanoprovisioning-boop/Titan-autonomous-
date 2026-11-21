from __future__ import annotations

from datetime import date, timedelta

import pytest

from conftest import ME, MITSU, NISSAN, at, profile
from titan.agents.appointments import AppointmentAgent, AppointmentError
from titan.agents.approvals import ApprovalError, ApprovalService
from titan.agents.reporting import Reporter
from titan.crm.base import CrmTask
from titan.crm.mock import MockProfileSession
from titan.models import (
    Activity,
    ActivityKind,
    AppointmentStatus,
    Channel,
    MessageStatus,
    OpportunityType,
    TaskKind,
)


def approve_all(ctx):
    svc = ApprovalService(ctx)
    for m in svc.pending():
        svc.approve([m.id], "Glenn Bordine")


def new_lead(crm, clock, cid="C1", rooftop=NISSAN, **kw):
    return crm.add(profile(cid, rooftop, lead_assigned_at=clock.now() - timedelta(seconds=10), **kw), new_lead=True)


class TestNewLead:
    def test_drafts_email_and_sms_but_never_sends_without_approval(self, ctx, crm, clock, orch):
        new_lead(crm, clock)
        orch.run_once()
        pending = ctx.store.messages(MessageStatus.PENDING_APPROVAL)
        assert {m.channel for m in pending} == {Channel.EMAIL, Channel.SMS}
        clock.advance(minutes=5)
        orch.run_once()
        assert crm.sent == []
        assert ctx.notifier.sent and "approval needed" in ctx.notifier.sent[0][0]

    def test_lead_processed_once_across_ticks(self, ctx, crm, clock, orch):
        new_lead(crm, clock)
        orch.run_once()
        orch.run_once()
        assert len(ctx.store.messages()) == 2

    def test_approved_sends_once_and_completes_separate_tasks(self, ctx, crm, clock, orch):
        rec = new_lead(crm, clock)
        orch.run_once()
        approve_all(ctx)
        clock.advance(seconds=30)
        orch.run_once()
        done = ctx.store.messages(MessageStatus.COMPLETED)
        assert {m.channel for m in done} == {Channel.EMAIL, Channel.SMS}
        assert len(crm.sent) == 2
        completed = [t for t in rec.tasks.values() if t.status == "completed"]
        notes = sorted(t.note for t in completed)
        assert notes == ["Manual email completed via system.", "SMS text completed via system."]
        assert all(t.assigned_to == ME for t in completed)
        assert len(crm.dashboard) == 2
        orch.run_once()
        assert len(crm.sent) == 2

    def test_follow_ups_scheduled_next_business_day_spaced(self, ctx, crm, clock, orch):
        new_lead(crm, clock)
        orch.run_once()
        tasks = ctx.store.tasks()
        kinds = {t.kind for t in tasks}
        assert kinds == {TaskKind.FOLLOW_UP_EMAIL, TaskKind.FOLLOW_UP_CALL}
        dues = sorted(t.due_at for t in tasks)
        assert dues[0].date() == date(2026, 10, 16)
        assert dues[1] - dues[0] >= timedelta(minutes=5)

    def test_profile_tabs_always_closed(self, ctx, crm, clock, orch):
        for i in range(4):
            new_lead(crm, clock, cid=f"C{i}")
        orch.run_once()
        approve_all(ctx)
        orch.run_once()
        assert crm.max_concurrent_tabs <= ctx.settings.resources.max_open_profiles
        assert all(a.open_tab_count() == 0 for a in orch.adapters.values())

    def test_other_salesperson_lead_ignored(self, ctx, crm, clock, orch):
        new_lead(crm, clock, assigned_salesperson="Smith, Pat")
        orch.run_once()
        assert ctx.store.messages() == []

    def test_failed_handle_does_not_consume_the_lead(self, ctx, crm, clock, orch):
        new_lead(crm, clock)
        worker = orch.workers[NISSAN]
        real = worker.new_leads._handle

        def fail(*_args, **_kwargs):
            raise RuntimeError("profile timeout")

        worker.new_leads._handle = fail
        with pytest.raises(RuntimeError, match="profile timeout"):
            orch.run_once()
        assert ctx.store.messages() == []
        assert not ctx.store.has_seen_lead(NISSAN, "OC1")

        worker.new_leads._handle = real
        orch.run_once()
        assert len(ctx.store.messages(MessageStatus.PENDING_APPROVAL)) == 2
        assert ctx.store.has_seen_lead(NISSAN, "OC1")
        orch.run_once()
        assert len(ctx.store.messages()) == 2

class TestSendSafety:
    def test_after_hours_queues_until_9am(self, ctx, crm, clock, orch):
        clock.set(at(2026, 10, 15, 21, 30))
        new_lead(crm, clock)
        orch.run_once()
        approve_all(ctx)
        orch.run_once()
        assert crm.sent == []
        assert len(ctx.store.messages(MessageStatus.APPROVED)) == 2
        clock.set(at(2026, 10, 16, 9, 0))
        orch.run_once()
        assert len(ctx.store.messages(MessageStatus.COMPLETED)) == 2

    def test_edit_after_approval_invalidates_it(self, ctx, crm, clock, orch):
        new_lead(crm, clock)
        orch.run_once()
        svc = ApprovalService(ctx)
        email = next(m for m in svc.pending() if m.channel is Channel.EMAIL)
        svc.approve([email.id], "Glenn Bordine")
        stored = ctx.store.get_message(email.id)
        stored.body += " Extra line."
        ctx.store.save_message(stored)
        orch.run_once()
        assert ctx.store.get_message(email.id).status is MessageStatus.BLOCKED
        assert crm.sent == []

    def test_every_send_mode_rejects_multi_approval(self, ctx, crm, clock, orch):
        new_lead(crm, clock)
        orch.run_once()
        ids = [m.id for m in ApprovalService(ctx).pending()]
        with pytest.raises(ApprovalError):
            ApprovalService(ctx).approve(ids, "Glenn Bordine")
        with pytest.raises(ApprovalError):
            ApprovalService(ctx).approve_batch("Glenn Bordine")

    def test_batch_mode(self, ctx, crm, clock, orch):
        ctx.settings.approval.mode = "batch"
        new_lead(crm, clock)
        new_lead(crm, clock, cid="C2", rooftop=MITSU)
        orch.run_once()
        approved = ApprovalService(ctx).approve_batch("Glenn Bordine")
        assert len(approved) == 4
        assert len({m.batch_id for m in approved}) == 1

    def test_customer_reply_after_draft_blocks_send(self, ctx, crm, clock, orch):
        rec = new_lead(crm, clock)
        orch.run_once()
        approve_all(ctx)
        clock.advance(minutes=1)
        rec.profile.activities.append(Activity(ActivityKind.SMS_IN, clock.now(), "customer", "Is it AWD?"))
        orch.run_once()
        assert crm.sent == []
        assert all(m.status is MessageStatus.BLOCKED for m in ctx.store.messages())

    def test_stop_received_after_approval_blocks_sms_only(self, ctx, crm, clock, orch):
        from dataclasses import replace

        rec = new_lead(crm, clock)
        orch.run_once()
        approve_all(ctx)
        rec.profile.permissions = replace(rec.profile.permissions, sms_stop=True)
        orch.run_once()
        statuses = {m.channel: m.status for m in ctx.store.messages()}
        assert statuses == {Channel.EMAIL: MessageStatus.COMPLETED, Channel.SMS: MessageStatus.BLOCKED}

    def test_uncertain_send_is_never_retried_or_completed(self, ctx, crm, clock, orch):
        rec = new_lead(crm, clock)
        crm.drop_send_activity.add(rec.profile.customer_id)
        orch.run_once()
        approve_all(ctx)
        orch.run_once()
        orch.run_once()
        assert len(crm.sent) == 2
        assert all(m.status is MessageStatus.SEND_UNCERTAIN for m in ctx.store.messages())
        assert not [t for t in rec.tasks.values() if t.status == "completed"]

    def test_existing_open_task_is_used_not_duplicated(self, ctx, crm, clock, orch):
        rec = new_lead(crm, clock)
        rec.tasks["T-existing"] = CrmTask("T-existing", "email", "open", ME, clock.now())
        orch.run_once()
        approve_all(ctx)
        orch.run_once()
        email_tasks = [t for t in rec.tasks.values() if t.kind == "email"]
        assert len(email_tasks) == 1 and email_tasks[0].status == "completed"

    def test_crash_mid_send_does_not_resend(self, ctx, crm, clock, orch):
        new_lead(crm, clock)
        orch.run_once()
        approve_all(ctx)
        m = next(m for m in ctx.store.messages() if m.channel is Channel.EMAIL)
        m.status = MessageStatus.SENDING
        ctx.store.save_message(m)
        for w in orch.workers.values():
            w._ready = False
        orch.run_once()
        assert ctx.store.get_message(m.id).status is MessageStatus.SEND_UNCERTAIN
        assert [s for s in crm.sent if s[1] is Channel.EMAIL] == []

    def test_wrong_identity_stops_work(self, ctx, crm, clock, orch):
        from titan.policy.identity import ScopeViolation

        crm.user_name = "Smith, Pat"
        new_lead(crm, clock)
        with pytest.raises(ScopeViolation):
            orch.run_once()
        assert ctx.store.messages() == []


class TestReengagement:
    def test_digest_and_drafts_for_eligible_past_customer(self, ctx, crm, clock, orch):
        crm.add(profile("P1", otype=OpportunityType.PAST_OWNER, vehicle_of_interest=None,
                        owned_vehicle="2022 Nissan Altima", last_purchase_date=date(2023, 5, 1)), past=True)
        crm.add(profile("P2", otype=OpportunityType.PAST_OWNER, vehicle_of_interest=None,
                        owned_vehicle="2025 Nissan Frontier", last_purchase_date=date(2026, 4, 1)), past=True)
        orch.run_once()
        d1 = ctx.store.get_digest("P1", NISSAN)
        d2 = ctx.store.get_digest("P2", NISSAN)
        assert d1.disposition.value == "qualified" and d1.sms_template and d1.email_template
        assert "appraisal" in d1.next_best_action.lower()
        assert d2.disposition.value == "excluded"
        msgs = ctx.store.messages()
        assert {m.customer_id for m in msgs} == {"P1"}
        assert any("[Titan summary" in n for n in crm.record(NISSAN, "P1").profile.notes)

    def test_reengagement_repeats_only_after_48h(self, ctx, crm, clock, orch):
        crm.add(profile("P1", otype=OpportunityType.DORMANT_LEAD), past=True)
        orch.run_once()
        approve_all(ctx)
        orch.run_once()
        assert len(crm.sent) == 2
        clock.advance(hours=25)
        orch.run_once()
        assert ctx.store.messages(MessageStatus.PENDING_APPROVAL) == []
        clock.set(at(2026, 10, 19, 10, 0))
        orch.run_once()
        assert {m.channel for m in ctx.store.messages(MessageStatus.PENDING_APPROVAL)} == {Channel.EMAIL,
                                                                                            Channel.SMS}


class TestAppointments:
    def test_approval_gate_and_save(self, ctx, crm, clock, orch):
        rec = crm.add(profile())
        agent = AppointmentAgent(ctx)
        a = agent.propose(rooftop_id=NISSAN, customer_id="C1", opportunity_id="OC1", customer_name="Dana",
                          starts_at=at(2026, 10, 17, 11, 0), purpose="complimentary appraisal",
                          arrival_notes="Trade: 2019 Honda CR-V")
        assert a.confirmation_call_at == at(2026, 10, 16, 11, 0)
        orch.run_once()
        assert rec.appointments == []
        agent.approve(a.id, "Glenn Bordine")
        orch.run_once()
        saved = ctx.store.get_appointment(a.id)
        assert saved.status is AppointmentStatus.SAVED
        assert rec.progress == "Appointment Set"
        assert [t.kind for t in rec.tasks.values()] == ["confirmation_call"]
        assert rec.printed == 1

    def test_rejects_outside_hours(self, ctx):
        with pytest.raises(AppointmentError):
            AppointmentAgent(ctx).propose(rooftop_id=NISSAN, customer_id="C1", opportunity_id="O", customer_name="x",
                                          starts_at=at(2026, 10, 18, 11, 0), purpose="p", arrival_notes="")

    def test_progress_failure_does_not_book_a_second_appointment(self, ctx, crm, clock, orch, monkeypatch):
        rec = crm.add(profile())
        agent = AppointmentAgent(ctx)
        proposal = agent.propose(
            rooftop_id=NISSAN, customer_id="C1", opportunity_id="OC1", customer_name="Dana",
            starts_at=at(2026, 10, 17, 11, 0), purpose="complimentary appraisal", arrival_notes="Trade",
        )
        agent.approve(proposal.id, "Glenn Bordine")
        original = MockProfileSession.progress
        calls = {"n": 0}

        def flaky(self):
            calls["n"] += 1
            if calls["n"] == 1:
                return "Open"
            return original(self)

        monkeypatch.setattr(MockProfileSession, "progress", flaky)
        with pytest.raises(AppointmentError, match="Appointment Set"):
            orch.run_once()
        assert len(rec.appointments) == 1
        stored = ctx.store.get_appointment(proposal.id)
        assert stored.status is AppointmentStatus.APPROVED
        assert stored.crm_appointment_id

        orch.run_once()
        assert len(rec.appointments) == 1
        assert ctx.store.get_appointment(proposal.id).status is AppointmentStatus.SAVED
        assert rec.printed == 1

    def test_same_day_confirmation_before_arrival(self, ctx):
        a = AppointmentAgent(ctx).propose(rooftop_id=NISSAN, customer_id="C1", opportunity_id="O",
                                          customer_name="x", starts_at=at(2026, 10, 15, 16, 0), purpose="p",
                                          arrival_notes="")
        assert ctx.clock.now() < a.confirmation_call_at < a.starts_at


class TestReporting:
    def test_report_counts_email_and_sms_separately_and_flags_dashboard_gap(self, ctx, crm, clock, orch):
        rec = new_lead(crm, clock)
        crm.drop_dashboard.add(rec.profile.customer_id)
        orch.run_once()
        approve_all(ctx)
        orch.run_once()
        reports = {r.rooftop_id: r for r in Reporter(ctx).build(clock.now().date(), orch.adapters)}
        r = reports[NISSAN]
        assert (r.email_completed, r.sms_completed) == (1, 1)
        assert len(r.dashboard_missing) == 2
        text = Reporter.render(list(reports.values()), clock.now().date(), clock.now())
        assert "Dashboard mismatches" in text
        assert any(e["event"] == "dashboard_mismatch" for e in ctx.store.audit_events())
