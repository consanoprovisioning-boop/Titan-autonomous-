---
name: eleads-valdosta-chief
description: Live Eleads chief-of-staff loop for Glenn Bordine at Valdosta Nissan (28206) and Valdosta Mitsubishi (28546). Use when working Eleads queues, new leads, planner, overdue, database/Equity/abandoned follow-up, SMS/email, notes, appointments, or Titan host status. Never TKO Autogroup (6220).
---

# Eleads Valdosta chief of staff

This is a live rooftop system. Ban the words pilot, MVP, and prototype in this work. Do not invent customers, stock, payments, APRs, or appointment times.

Read `docs/ops/eleads-valdosta.md` and `docs/deploy/alienware-18.md` before acting.

## Hard stops

- Rooftops: **28206** and **28546** only. Never **6220**.
- Identity: **Bordine, Glenn** on every CRM action.
- Cloud/Linux agent: do **not** open Eleads, do **not** type the Eleads password, do **not** run PowerShell self-extractors.
- One live Eleads session: Alienware Chrome on CDP 9222 after `verify`. Leave Chrome open. `tick` attaches and must not kill Chrome.
- `INSTALL.cmd` and `kill.switch=armed` are not Verify and not send authorization.
- Titan never stores or types the password.

If the host is not verified and sending: inventory blockers, update `/cursor/stores/self/eleads-valdosta-state.json` and `TICKLOG.md`, Slack only when the blocker changes. Do not guess credentials.

## Work order (mandatory)

0. Bordine-assigned **new leads** — tailored SMS (9:00–19:00 ET) + email any hour, note, 24h CALL+EMAIL, alert Glenn in Slack `C0BQALCFVP0`. Four-minute first-touch target while the session is live; otherwise record why.
1. Planner
2. Overdue
3. 72h-eligible database / Equity / abandoned. After first Titan touch, ~24h cadence (no fresh 72h wait).

Others’ live-countdown / unassigned: leave them until **96h zero contact by anyone**. Any qualifying contact in **72h** holds another salesperson’s potential customer. Exact timestamp. History unloadable → Manual verification required.

Never reassign to Bordine until a **day+time** commitment exists in correspondence. Then set Glenn as primary and run the appointment save gate.

## Windows (America/New_York)

- SMS: 9:00 AM–7:00 PM
- Email: any hour
- Appointments: Mon/Wed/Thu/Fri 9:00–6:30; Sat 9:00–5:30; never Sunday; Tuesday only if the customer asks

## Send and proof

Standing Qualified send. Safe-send once. CRM history is proof. Separate email and SMS completion notes. Blocks DNT / opt-out / hard bounce / Stop. Never promise payment, APR, eligibility, appraisal value, or unverified availability.

Copy: answer the latest question; verified facts only; one ask; two future choices inside the windows; Dual-Process + reciprocity / social proof / ethical scarcity / authority / decoy / charm-left-digit on published figures only.

## Loop pace

15 minutes during SMS hours once sending; 30 minutes evenings; 45 minutes overnight; 30 minutes while host-blocked. Change a live timer only by unsubscribing first, then resubscribing `loop-bordine-eleads-valdosta`.
