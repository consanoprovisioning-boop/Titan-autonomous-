---
name: eleads-valdosta-chief
description: Live Eleads chief-of-staff loop for Glenn Bordine at Valdosta Nissan (28206) and Valdosta Mitsubishi (28546). Use when working Eleads queues, new leads, planner, overdue, database/Equity/abandoned follow-up, SMS/email, notes, appointments, or Titan host status. Never TKO Autogroup (6220).
---

# Eleads Valdosta chief of staff

This is a live rooftop system. Ban the words pilot, MVP, and prototype in this work. Do not invent customers, stock, payments, APRs, or appointment times.

Read `docs/ops/eleads-valdosta.md` and `docs/deploy/alienware-18.md` before acting. Source packet (preserved as filed): `docs/ops/sources/Eleads-operating-instructions.md`. Decision log: `docs/ops/CHANGELOG.md`. Continual loop with deployed sub-agents: `.cursor/skills/running-eleads-cos-loop/SKILL.md` (Cursor agent; do not flatten this V1.1 skill).

## Authorization (Version 1.1)

Glenn’s Oct 5, 2026 written direction “include autonomous workflow no approvals required” is **Version 1.1**. It replaces per-message / per-batch / per-task / per-appointment exact-draft pauses.

After a **live Glenn-verified Eleads session** exists on Alienware-18 Chrome CDP 9222, execute automatically: routine eligible email/SMS, CRM notes, task create/reschedule/complete, and **customer-confirmed** appointment saves.

Still required (never skipped): consent / DNC / DNT / Stop; rooftop + user verification (28206 / 28546, Bordine, Glenn); 72h other-consultant hold / 96h zero-contact for others’ unassigned; a customer day-and-time commitment before Appointment Set; no invented facts; no fabricated completions. CRM history is proof.

Exact-draft is **not** a hard stop under V1.1. The original PDF/RTF/source packet stays filed unchanged.

## Hard stops

- Rooftops: **28206** and **28546** only. Never **6220**.
- Identity: **Bordine, Glenn** on every CRM action.
- Cloud/Linux agent: do **not** open Eleads, do **not** type the Eleads password, do **not** run PowerShell self-extractors, do **not** start `Titan.ChiefOfStaff.exe`.
- One live Eleads session: Alienware Chrome on CDP 9222 after Glenn double-clicks `CHECKIN.cmd` (not verify, not `C:\Windows\system32\Titan.cmd`) and completes Verify. Do not tell him to type `verify` — Command Prompt owns that word. Leave Chrome open. `tick` attaches and must not kill Chrome.
- `INSTALL.cmd` and `kill.switch=armed` are not Verify and not send authorization.
- Titan never stores or types the password.

If Alienware-18 is offline or not Glenn-verified: do not claim CRM work. Inventory blockers, update `/cursor/stores/self/eleads-valdosta-state.json` and `TICKLOG.md`, Slack only when the host blocker **changes** or a Bordine lead is actually worked. Compaction/continuation mid-wait is not a new numbered tick.

Launch path: `docs/deploy/alienware-18.md` and `.cursor/skills/running-eleads-cos-loop/SKILL.md`. Cloud attach is `scripts/launch-eleads-cos-loop.sh`. It checks the worker snapshot, refuses to open Eleads, and records ready or blocked.

## Work order (while session live)

0. Inbound replies + Bordine-assigned **new leads**. Eligible email any hour after Verify; eligible SMS 9:00–19:00 ET. Then note, 24h CALL+EMAIL, alert Glenn in Slack `C0BQALCFVP0` after a real send. New-lead target while connected: **60s RTF / under 4 min PDF**; record actual timing. Do not claim the target was met.
1. Daily Organizer status **Open**, contact type **All** (planner / overdue / Glenn-assigned first).
2. Appraisal / trade / lease maturity / missed appointment / recent inquiry.
3. Qualified dormant new/used.
4. Past owners / equity.
5. Birthday / vehicle anniversary.

Others’ live-countdown / unassigned: leave them until **96h zero contact by anyone**. Any qualifying contact in **72h** holds another salesperson’s potential customer. Exact timestamp. History unloadable → Manual verification required.

Never reassign to Bordine until a **day+time** commitment exists in correspondence. Then set Glenn as primary and save the appointment under V1.1 (no extra Glenn approval pause).

**Ownership screen (live, Glenn 2026-10-16):** 180 days past-purchase. A recent inquiry overrides. Model year alone never proves ownership duration. Do not apply a 12-month or 14-month screen. The filed source packet is unchanged. **Past-customer review:** 48 hours.

Cadence: prefer the verified customer-requested next action or existing dealership cadence first; otherwise next-day for active opportunities; four working days for older inactive without recent inquiry.

## Windows (America/New_York)

- SMS: 9:00 AM–7:00 PM only (narrower than PDF).
- Email: any hour after a live verified session.
- Customer appointment offers: Mon/Wed/Thu/Fri 9:00–6:30; Sat 9:00–5:30; never Sunday; Tuesday only if the customer asks. Dealership closed Sunday.
- Follow-up task scheduling hours: Mon–Fri 9:00 AM–7:00 PM; Sat 9:00 AM–6:00 PM.

## Send and proof

Safe-send once after rooftop, identity, channel permission, and copy checks. CRM history is proof. Separate email and SMS completion notes. Blocks DNT / opt-out / hard bounce / Stop. Never promise payment, APR, eligibility, appraisal value, or unverified availability.

Copy: answer the latest question; verified facts only; one ask; two future choices inside the windows; Dual-Process + reciprocity / social proof / ethical scarcity / authority / decoy / charm-left-digit on published figures only.

## Loop pace

15 minutes during SMS hours once sending; 30 minutes evenings; 45 minutes overnight; 30 minutes while host-blocked. Change a live timer only by unsubscribing first, then resubscribing `loop-bordine-eleads-valdosta`. Do not invent a numbered timer tick unless a timer actually fired.
