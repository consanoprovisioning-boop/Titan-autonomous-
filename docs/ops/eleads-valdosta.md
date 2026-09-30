# Eleads Valdosta — live operating law

This is the live customer-contact system for **Bordine, Glenn** at **Valdosta Nissan (28206)** and **Valdosta Mitsubishi (28546)**. It is not a pilot, MVP, or prototype. Do not write mock customers, mock rooftops, mock sends, or placeholder appointments.

Source packet: Customer Traffic and Appointment Growth, filed as `docs/ops/sources/Eleads-operating-instructions.md`. Titan host and loop-pace rules below do not override that packet, dealership policy, customer consent, applicable law, or an Eleads system restriction.

## Identity and rooftops

- Attribute every CRM action to **Bordine, Glenn**.
- Work only **28206** and **28546**.
- Never open or act in **The TKO Autogroup (6220)**.
- Verify rooftop and user identity before any CRM action.

## Host boundary

One live Eleads session belongs to the **Alienware Titan.ChiefOfStaff** host after Glenn Verify. Titan never stores or types the Eleads password.

This Linux cloud agent must not:

- open `eleadcrm.com`
- start `Titan.ChiefOfStaff.exe`
- run PowerShell self-extractors or `Set-ExecutionPolicy Bypass`
- type an Eleads password

Install-without-launch copy is not Verify and not send authorization. See `docs/deploy/alienware-18.md`.

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

0. **Bordine-assigned new leads first.** Draft email and eligible SMS. Obtain Glenn’s exact-draft approval, then send. CRM note, 24h CALL+EMAIL, alert Glenn. Four-minute first-touch target while the session is live; if blocked, record the reason. Do not claim the target was met.
1. **Planner**
2. **Overdue**
3. **72h-eligible** database / Equity / abandoned working leads. After the first Titan touch, use the normal ~24h cadence. Do not impose a fresh 72h wait on a Titan-touched profile.

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
| 2 | New leads | Prepare email and eligible SMS within the four-minute operating target while the session is available. |
| 3 | Trade, appraisal, lease maturity, missed appointment, recent inquiry | Offer a complimentary review and two future appointment choices. |
| 4 | Qualified dormant new/used vehicle leads | Re-engage after full history and permission review. |
| 5 | Past owners and equity | Discuss current ownership, appraisal, and possible upgrade needs. |
| 6 | Birthday and vehicle anniversary | Relationship follow-up with a clear appraisal or upgrade invitation. |

## Profile inspection (no list-view qualification)

Open the customer and opportunity. Wait for the full activity feed.

Review: completed activities and notes; inbound/outbound SMS; calls and outcomes; email sends/opens/replies/failures; last follow-up and last-modified; appointments (active, missed, rescheduled, confirmed); recent inquiry and stated timing; sold/service/ownership; relationships; Lifetime Value and Equity; vehicle of interest; owned vehicle and purchase timing; trade, mileage, payoff, appraisal; financing / co-signer / documents; stock facts without inventing availability; valid phone/email; DNC/DNT; email opt-out, unsubscribe, hard bounce, Stop; explicit no-interest; correct salesperson and rooftop.

**Ownership screen:** 12 months past-purchase guideline. A recent inquiry overrides the last-purchase timing screen and is reviewed on its own current intent. Model year alone never proves ownership duration or eligibility.

**Do not pursue:** no-further-contact, prohibited channels, bought elsewhere and ended the search, explicitly not in market, or no valid permitted channel.

**Funding:** if Glenn’s notes say unable to obtain funding, bypass unless a later customer inquiry or new verified fact creates a reason.

Glenn-owned profiles: prepare follow-up only after the latest email/SMS timing and all permissions.

## Channels

| Channel | When | Proof |
| --- | --- | --- |
| Email | Valid permitted email; no opt-out / hard bounce; **approved draft**. Any hour. | Sent activity + separate completed email task/note: `Manual email completed via system.` |
| SMS | Valid SMS-permitted phone; no DNT / Stop; **approved draft**. **9:00 AM–7:00 PM America/New_York only.** | Sent activity + separate completed SMS task/note: `SMS text completed via system.` |
| Phone | Valid callable number; no DNC. Autonomous calls require telephony integration. | Actual call outcome. Never mark a call completed if no call occurred. |

A draft, blank composer, preview, or processing screen is not delivery. CRM history under Bordine, Glenn or Glenn Bordine is the evidence. Click Send once. Recheck history before any retry. Do not resend automatically when the result is uncertain.

If Eleads warns a same-type task already exists, cancel the duplicate unless Glenn approves a justified second task.

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
- Sunday: never
- Tuesday: only if the customer requests it

Follow-up task scheduling hours (dealership clock):

- Monday–Friday: 9:00 AM–7:00 PM
- Saturday: 9:00 AM–6:00 PM

A solid appointment is a customer commitment to a **day and time**. Interest without a time stays follow-up. **Never reassign to Bordine / set Glenn as primary until that commitment exists in correspondence.** Then:

1. Show Glenn customer, rooftop, date/time, purpose, arrival notes, and confirmation-call timing. Save only after that approval.
2. Create a Sales Appointment assigned to Bordine, Glenn.
3. Set opportunity progress to Appointment Set. Keep the opportunity open.
4. Verify the scheduled activity and Appointment Set status.
5. Check for an existing confirmation task before creating another.
6. Confirmation call normally 24 hours before, or earlier the same day for a later appointment. Same-day commitments: confirm before arrival.
7. Print page 1 only after appointment and confirmation task are verified.

Arrival notes: vehicles of interest; trade year/make/model; mileage and payoff if given; appraisal request; co-signer attendance; documents they will bring; open questions without unsupported promises; who to ask for; arrival instructions.

Space routine follow-up tasks at least five minutes apart when practical. Use the customer’s requested callback or appointment time when provided. Older inactive customers without a recent inquiry may use four-working-day follow-up; active opportunities may need next-day.

## Safe-send sequence

1. Confirm rooftop, customer, sender, channel permission, recipient, subject, and message.
2. Obtain Glenn’s approval for the exact draft or batch. Do not send on a standing Qualified rule.
3. Preview and confirm the final send control.
4. Click Send once.
5. Refresh activity history before any retry.
6. Verify the completed CRM entry under Bordine, Glenn.

## Daily cycle

1. Verify authenticated session, rooftop, and Bordine identity.
2. Triage direct replies, new leads, overdue tasks, appointments, missed appointments, equity, and recent inquiries.
3. Inspect the full profile, every history tab, permissions, vehicle/trade, and exact timestamps. Decide qualified, held, or excluded.
4. Draft personalized email/SMS with one appointment ask and two future choices.
5. Obtain Glenn’s approval for every exact email and SMS.
6. Send once, verify history, complete separate tasks/notes, check Sales Performance / Daily Activity.
7. Handle replies and present any day-and-time commitment for appointment-save approval.
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
