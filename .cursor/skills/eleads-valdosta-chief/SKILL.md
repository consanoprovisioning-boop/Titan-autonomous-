---
name: eleads-valdosta-chief
description: Live Eleads chief-of-staff loop for Glenn Bordine at Valdosta Nissan (28206) and Valdosta Mitsubishi (28546). Use when working Eleads queues, new leads, planner, overdue, database/Equity/abandoned follow-up, SMS/email, notes, appointments, or Titan host status. Never TKO Autogroup (6220).
---

# Eleads Valdosta chief of staff

This is a live rooftop system. Ban the words pilot, MVP, and prototype in this work. Do not invent customers, stock, payments, APRs, or appointment times.

Read `docs/ops/eleads-instructions.md` and `docs/ops/eleads-parallel-loop.md` before acting. Deploy docs: `docs/deploy/alienware-18.md`.

## Hard stops

- Rooftops: **28206** and **28546** only. Never **6220**.
- Identity: **Bordine, Glenn** on every CRM action.
- Cloud/Linux agent: do **not** open Eleads, do **not** type the Eleads password, and do **not** run the host. Sub-agents receive text packets only.
- One live Eleads session: Alienware Chrome after Glenn double-clicks `COS.cmd`, which runs `CHECKIN.cmd`. Do not tell him to type `verify`. Leave Chrome open. `tick` attaches and must not kill Chrome. The launcher does not read or store the Eleads password.
- `INSTALL.cmd` and `kill.switch=armed` are not check-in and not send authorization.
- Titan never stores or types the password.
- SMS only from 9:00 AM until 7:00 PM America/New_York, never Sunday.
- Another consultant: 72-hour hold, and 96 hours of zero contact before eligibility, unless Titan already touched the profile.
- Glenn approves the exact batch before any Send. A prior batch does not cover a new customer or edited wording.

If the host is not verified and sending: inventory blockers, update `/cursor/stores/self/eleads-valdosta-state.json` and `TICKLOG.md`, Slack only when the blocker changes. Do not guess credentials.

## Parallel tick

Parent owns the browser. Fan out judgment. Never fan out Send.

1. Verify rooftop and identity.
2. Capture the organizer. Dispatch **queue-ranker**.
3. Capture fast-lane profiles serially. Dispatch every **profile-inspector** in one turn.
4. Dispatch every **draft-writer** and **appointment-packer** in one turn.
5. One approval batch. Wait.
6. Send one approved row at a time. Proof strings: `Manual email completed via system.` and `SMS text completed via system.`
7. Read proof pages, then dispatch every **proof-judge** in one turn.
8. Report, then clean up. Main lane starts only after fast-lane proof is recorded.

Role inputs and the exception rules are in `docs/ops/eleads-parallel-loop.md`.

## Work order

0. Bordine-assigned new leads, then direct replies already in the fast lane. Four-minute first-touch target while the session is live; otherwise record why.
1. Planner
2. Overdue
3. 96h-eligible database / Equity / abandoned. After the first Titan touch, use the normal follow-up cadence.

Never reassign to Bordine until a day-and-time commitment exists in the correspondence. Then run the appointment save gate and print page 1 only after the appointment and the confirmation task are verified.

## Copy

Answer the latest question. Verified facts only. One ask. Two future choices inside Mon/Wed/Thu/Fri 9:00–6:30, Sat 9:00–5:30, Tuesday only if the customer asked. Never Sunday. The incentive sentence stays secondary and uses the allowed wording in `docs/ops/eleads-instructions.md`.
