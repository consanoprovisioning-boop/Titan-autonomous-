from __future__ import annotations

from datetime import datetime

from titan.agents.context import AgentContext
from titan.crm.base import DuplicateTaskWarning
from titan.messaging import confirmation_time
from titan.models import AppointmentProposal, AppointmentStatus, TaskKind
from titan.resources import ProfileSessionManager


class AppointmentError(RuntimeError):
    pass


class AppointmentAgent:
    """PDF section 5: a solid appointment = specific day + time; saved only after Glenn approves."""

    def __init__(self, ctx: AgentContext):
        self.ctx = ctx

    def propose(self, *, rooftop_id: str, customer_id: str, opportunity_id: str, customer_name: str,
                starts_at: datetime, purpose: str, arrival_notes: str) -> AppointmentProposal:
        self.ctx.guard.check_rooftop(rooftop_id)
        now = self.ctx.clock.now()
        if starts_at <= now:
            raise AppointmentError("Appointment must be in the future")
        if self.ctx.window.clamp_to_business_hours(starts_at) != starts_at:
            raise AppointmentError("Appointment is outside dealership hours")
        confirm_at = confirmation_time(self.ctx.window, starts_at, now, self.ctx.settings.cadence.confirmation_lead_hours)
        proposal = AppointmentProposal(customer_id, opportunity_id, rooftop_id, customer_name, starts_at,
                                       purpose, arrival_notes, confirm_at)
        self.ctx.store.save_appointment(proposal)
        self.ctx.audit("appointment_proposed", rooftop_id=rooftop_id, customer_id=customer_id,
                       appointment_id=proposal.id, starts_at=starts_at.isoformat())
        self.ctx.notifier.notify("Appointment approval needed",
                                 f"{customer_name} {starts_at:%a %b %d %I:%M %p} - {purpose}")
        return proposal

    def approve(self, appointment_id: str, approver: str) -> AppointmentProposal:
        a = self.ctx.store.get_appointment(appointment_id)
        if a is None or a.status is not AppointmentStatus.PENDING_APPROVAL:
            raise AppointmentError("Appointment is not awaiting approval")
        a.status = AppointmentStatus.APPROVED
        self.ctx.store.save_appointment(a)
        self.ctx.audit("appointment_approved", rooftop_id=a.rooftop_id, customer_id=a.customer_id,
                       appointment_id=a.id, approver=approver)
        return a

    def save(self, a: AppointmentProposal, sessions: ProfileSessionManager) -> AppointmentProposal:
        if a.status is not AppointmentStatus.APPROVED:
            raise AppointmentError("Only approved appointments are saved")
        with sessions.profile(a.rooftop_id, a.customer_id, a.opportunity_id) as session:
            user, rooftop = sessions.adapter.current_identity()
            self.ctx.guard.verify_session(expected_rooftop=a.rooftop_id, user_name=user, rooftop_id=rooftop)
            if not a.crm_appointment_id:
                a.crm_appointment_id = session.create_sales_appointment(a.starts_at, self.ctx.me, a.arrival_notes)
                # Persist before progress/task/print. Those steps raise on purpose, and the next
                # cycle reloads APPROVED rows. Without this id, that cycle books a second appointment.
                self.ctx.store.save_appointment(a)
            session.set_progress("Appointment Set")
            if session.progress() != "Appointment Set":
                self.ctx.audit("appointment_progress_unverified", rooftop_id=a.rooftop_id,
                               customer_id=a.customer_id, appointment_id=a.id)
                raise AppointmentError("Opportunity progress did not update to Appointment Set")
            if not a.crm_confirmation_task_id:
                existing = [t for t in session.open_tasks() if t.kind == TaskKind.CONFIRMATION_CALL.value]
                if existing:
                    a.crm_confirmation_task_id = existing[0].crm_task_id
                else:
                    try:
                        task = session.create_task(TaskKind.CONFIRMATION_CALL, a.confirmation_call_at,
                                                   f"Confirm {a.purpose} on {a.starts_at:%a %b %d %I:%M %p}")
                    except DuplicateTaskWarning as dup:
                        task = dup.existing
                    a.crm_confirmation_task_id = task.crm_task_id
                self.ctx.store.save_appointment(a)
            session.print_profile_page1()
        a.status = AppointmentStatus.SAVED
        self.ctx.store.save_appointment(a)
        self.ctx.audit("appointment_saved", rooftop_id=a.rooftop_id, customer_id=a.customer_id,
                       appointment_id=a.id, crm_appointment_id=a.crm_appointment_id,
                       confirmation_task=a.crm_confirmation_task_id)
        return a
