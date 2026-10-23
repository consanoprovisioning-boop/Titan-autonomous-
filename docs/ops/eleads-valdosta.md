# Eleads Valdosta — live operating law

This is the live customer-contact system for **Bordine, Glenn** at **Valdosta Nissan (28206)** and **Valdosta Mitsubishi (28546)**. It is not a pilot, MVP, or prototype. Do not write mock customers, mock rooftops, mock sends, or placeholder appointments.

Source packet (preserved as filed, not rewritten): Customer Traffic and Appointment Growth, `docs/ops/sources/Eleads-operating-instructions.md`, plus `Eleads_Project_Goal.pdf` and `Eleads_project_Instructions_Goal.txt`. Titan host and loop-pace rules below do not override that packet’s consent, identity, rooftop, and proof requirements, dealership policy, applicable law, or an Eleads system restriction.

**Authorization version:** 1.1 (Glenn Oct 5, 2026). See `docs/ops/CHANGELOG.md`.

**Ownership screen (live):** 180 days (Glenn 2026-10-16). A recent inquiry overrides. Model year never proves duration.

**Continual loop:** Cursor agent skill `.cursor/skills/running-eleads-cos-loop/SKILL.md` deploys the small specialist set under one Core. This file is the long law.

## Version 1.1 standing authorization

Glenn’s Oct 5, 2026 written direction “include autonomous workflow no approvals required” is Version 1.1. It replaces per-message / per-batch / per-task / per-appointment exact-draft pauses in this live law.

After a **live Glenn-verified Eleads session** exists (Alienware-18, `TitanCOS.exe` or `CHECKIN.cmd`, Verify complete, Chrome CDP 9222), execute automatically:

- Routine eligible email and SMS
- CRM notes
- Task create / reschedule / complete
- Customer-confirmed appointment saves (day and time already in correspondence)

Exact-draft is not a hard stop under V1.1. Do not pause for Glenn to approve the exact wording of a routine eligible send or a customer-confirmed appointment save.

Still required before any send or appointment save:

- Rooftop and user identity verified (28206 or 28546; Bordine, Glenn)
- Channel permission (no DNC / DNT / Stop / opt-out / hard bounce as applicable)
- 72-hour other-consultant hold / 96-hour zero-contact for others’ unassigned, using exact timestamps
- Customer day-and-time commitment in correspondence before Appointment Set or reassignment to Bordine
- Verified facts only; no fabricated completions
- CRM history as proof of send

## Identity and rooftops

- Attribute every CRM action to **Bordine, Glenn**.
- Work only **28206** and **28546**.
- Never open or act in **The TKO Autogroup (6220)**.
- Verify rooftop and user identity before any CRM action.

## Host boundary

One live Eleads session belongs to the **Alienware Titan COS** host after Glenn Verify. Titan never stores or types the Eleads password. On that PC, double-click `TitanCOS.exe` is the full launch.

This Linux cloud agent must not:

- open `eleadcrm.com`
- start `Titan.ChiefOfStaff.exe` or `TitanCOS.exe`
- run PowerShell self-extractors or `Set-ExecutionPolicy Bypass`
- type an Eleads password

If Alienware-18 is offline or not Glenn-verified: do not claim CRM work, background CRM monitoring, sends, or appointments. Tell Glenn to double-click `TitanCOS.exe` (not `verify`, not `C:\Windows\system32\Titan.cmd`), sign in as Bordine, Glenn if Eleads asks, and leave the overlay up. Then attach.

Install-without-launch copy is not Verify and not send authorization. See `docs/deploy/alienware-18.md`.

Cloud attach, from the Cursor agent on Linux:

1. Refresh `/cursor/stores/self/worker-snapshot.json` from cursor-cloud `list-self-hosted-workers` with `scope=all` and `status=all`.
2. Run `scripts/launch-eleads-cos-loop.sh --workers-file /cursor/stores/self/worker-snapshot.json`.

That script refuses to open Eleads and writes `/cursor/stores/self/eleads-valdosta-state.json`. Exit 3 is blocked. Exit 0 is a Glenn-verified Alienware session. It does not create a numbered timer tick.

## Success

Customer outcomes:

- More specific appointment commitments with an agreed day and time.
- More customers arriving for appraisals, upgrades, and vehicle-selection visits.
- Fast, helpful responses to new leads and direct customer replies.

Operational outcomes:

- Every send and completed task is visible in CRM history.
- Email and SMS are counted as separate activities.
- Management reports match the actual work performed.

## Mandatory work order

While the Alienware host is verified and sending:

0. **Inbound replies and Bordine-assigned new leads first.** Eligible email any hour after Verify; eligible SMS 9:00 AM–7:00 PM ET. CRM note, 24h CALL+EMAIL, alert Glenn after a real send. New-lead target while the session is live: **60 seconds (RTF) / under 4 minutes (PDF)**; record actual timing. If blocked, record the reason. Do not claim the target was met.
1. **Daily Organizer** status **Open**, contact type **All** (Glenn-assigned first, including planner and overdue).
2. **Appraisal / trade / lease maturity / missed appointment / recent inquiry.**
3. **Qualified dormant** new/used vehicle leads.
4. **Past owners and equity.**
5. **Birthday and vehicle anniversary.**

Leave other consultants’ live-countdown and unassigned leads alone until **96 hours with zero contact by anyone**. Then they are eligible. Any qualifying contact in the preceding **72 hours** (note, SMS, call, customer email, or actively managed appointment) holds another salesperson’s potential customer. Use the exact activity timestamp, not only the task comment.

If history cannot load: **Manual verification required.** Do not assume untouched.

## Daily Organizer

1. Select and verify the rooftop.
2. Open Daily Organizer: status **Open**, contact type **All**.
3. Glenn-assigned work first, then store-wide only when authorized.
4. Capture customer, due time, task type, vehicle, assigned consultant, comments, opportunity link.
5. Deduplicate tasks on the same customer and opportunity.

Priority inside a queue:

| Rank | Opportunity type | Primary objective |
| --- | --- | --- |
| 1 | Direct replies and explicit interest | Answer the customer, then secure a day and time. |
| 2 | New leads | Prepare and send eligible email and SMS within the 60s RTF / under-4-min PDF target while the session is available. Record actual timing. |
| 3 | Trade, appraisal, lease maturity, missed appointment, recent inquiry | Offer a complimentary review and two future appointment choices. |
| 4 | Qualified dormant new/used vehicle leads | Re-engage after full history and permission review. |
| 5 | Past owners and equity | Discuss current ownership, appraisal, and possible upgrade needs. |
| 6 | Birthday and vehicle anniversary | Relationship follow-up with a clear appraisal or upgrade invitation. |

## Profile inspection (no list-view qualification)

Open the customer and opportunity. Wait for the full activity feed.

Review: completed activities and notes; inbound/outbound SMS; calls and outcomes; email sends/opens/replies/failures; last follow-up and last-modified; appointments (active, missed, rescheduled, confirmed); recent inquiry and stated timing; sold/service/ownership; relationships; Lifetime Value and Equity; vehicle of interest; owned vehicle and purchase timing; trade, mileage, payoff, appraisal; financing / co-signer / documents; stock facts without inventing availability; valid phone/email; DNC/DNT; email opt-out, unsubscribe, hard bounce, Stop; explicit no-interest; correct salesperson and rooftop.

**Ownership screen (live, Glenn 2026-10-16):** 180 days past-purchase. A recent inquiry overrides the last-purchase timing screen and is reviewed on its own current intent. Model year alone never proves ownership duration or eligibility. Do not apply a 12-month or 14-month screen. The Oct 5 Version 1.1 figure of 14 months is superseded for live work. The source packet still says 12 months as filed history and is not rewritten; that filed line is not the live screen.

**Past-customer review:** 48 hours.

**Do not pursue:** no-further-contact, prohibited channels, bought elsewhere and ended the search, explicitly not in market, or no valid permitted channel.

**Funding:** if Glenn’s notes say unable to obtain funding, bypass unless a later customer inquiry or new verified fact creates a reason.

Glenn-owned profiles: prepare follow-up only after the latest email/SMS timing and all permissions.

## Channels

| Channel | When | Proof |
| --- | --- | --- |
| Email | Valid permitted email; no opt-out / hard bounce. Any hour after a live verified session. | Sent activity + separate completed email task/note: `Manual email completed via system.` |
| SMS | Valid SMS-permitted phone; no DNT / Stop. **9:00 AM–7:00 PM America/New_York only.** | Sent activity + separate completed SMS task/note: `SMS text completed via system.` |
| Phone | Valid callable number; no DNC. Autonomous calls require telephony integration. | Actual call outcome. Never mark a call completed if no call occurred. |

A draft, blank composer, preview, or processing screen is not delivery. CRM history under Bordine, Glenn or Glenn Bordine is the evidence. Click Send once. Recheck history before any retry. Do not resend automatically when the result is uncertain.

If Eleads warns a same-type task already exists, cancel the duplicate unless a justified second task is required.

Never force-close a task. If Eleads fails to report real work, document the mismatch.

## Copy (live)

- Answer the latest customer question or concern first.
- Use only verified customer, vehicle, trade, ownership, and timing facts.
- Email generally under 120 words. SMS short and conversational.
- One clear appointment ask. Offer two specific future choices inside the appointment windows.
- Focus on a complimentary appraisal, ownership review, upgrade exploration, or vehicle-selection visit.
- If the original vehicle is no longer the best fit, offer only verified alternatives. Do not invent availability.
- Dual-Process plus reciprocity, social proof, ethical scarcity, authority, decoy, and charm-left-digit on **published figures only**.
- Vehicle-specific secondary line: “all possible qualifying incentives, rebates, and special manufacturer APR programs.” Keep this secondary to the appointment request.
- Never promise eligibility, approval, a payment, an appraisal value, a specific savings amount, an APR, a rebate, or vehicle availability unless verified live and qualified.
- Blocks DNT / opt-out.

## Appointment windows (America/New_York)

Customer-facing appointment offers:

- Mon / Wed / Thu / Fri: 9:00 AM–6:30 PM
- Sat: 9:00 AM–5:30 PM
- Sunday: never (dealership closed)
- Tuesday: only if the customer requests it

Follow-up task scheduling hours (dealership clock):

- Monday–Friday: 9:00 AM–7:00 PM
- Saturday: 9:00 AM–6:00 PM

A solid appointment is a customer commitment to a **day and time**. Interest without a time stays follow-up. **Never reassign to Bordine / set Glenn as primary until that commitment exists in correspondence.** Then, under V1.1, save without an extra Glenn approval pause:

1. Create a Sales Appointment assigned to Bordine, Glenn (customer, rooftop, date/time, purpose, arrival notes, confirmation-call timing).
2. Set opportunity progress to Appointment Set. Keep the opportunity open.
3. Verify the scheduled activity and Appointment Set status.
4. Check for an existing confirmation task before creating another.
5. Confirmation call normally 24 hours before, or earlier the same day for a later appointment. Same-day commitments: confirm before arrival.
6. Print page 1 only after appointment and confirmation task are verified.

Arrival notes: vehicles of interest; trade year/make/model; mileage and payoff if given; appraisal request; co-signer attendance; documents they will bring; open questions without unsupported promises; who to ask for; arrival instructions.

Space routine follow-up tasks at least five minutes apart when practical. Prefer the verified customer-requested next action or existing dealership cadence first. Use the customer’s requested callback or appointment time when provided. Active opportunities may use next-day follow-up; older inactive customers without a recent inquiry may use four-working-day follow-up.

## Safe-send sequence

1. Confirm rooftop, customer, sender, channel permission, recipient, subject, and message.
2. Under V1.1, do not wait for exact-draft approval when the session is live and the send is eligible.
3. Preview and confirm the final send control.
4. Click Send once.
5. Refresh activity history before any retry.
6. Verify the completed CRM entry under Bordine, Glenn.

## Daily cycle

1. Verify authenticated session, rooftop, and Bordine identity.
2. Triage inbound replies, new leads, Daily Organizer Open/All, overdue tasks, appointments, missed appointments, equity, and recent inquiries.
3. Inspect the full profile, every history tab, permissions, vehicle/trade, and exact timestamps. Decide qualified, held, or excluded.
4. Draft personalized email/SMS with one appointment ask and two future choices.
5. Send eligible messages under V1.1 (no exact-draft pause).
6. Send once, verify history, complete separate tasks/notes, check Sales Performance / Daily Activity.
7. Handle replies and save any day-and-time commitment as an appointment under V1.1.
8. Close completed customer tabs and unused tabs. Keep necessary queues open.

## Management report (CRM-confirmed counts only)

- Total open tasks and distinct opportunities by rooftop.
- Glenn-assigned versus other-consultant opportunities.
- Qualified, held, and excluded profiles with reasons.
- Email and SMS totals separately, CRM-confirmed only.
- Completed tasks visible on Sales Performance / Daily Activity.
- Appointment commitments, Appointment Set verification, confirmation tasks, shows, and sales outcomes.
- Uncertain sends, duplicates, reporting mismatches, and system errors.

Do not report a send that is not in CRM history.
