# Eleads customer-traffic instructions

Source: Customer Traffic and Appointment Growth, Version 1.0, August 6, 2026, prepared for Glenn Bordine. Working draft. These instructions do not override dealership policy, customer consent, applicable law, or an Eleads system restriction.

The Aug 6 packet is the customer-contact law. Where that packet is looser than `Policy.cs`, follow the host. Those looser lines stay worksheet items until Glenn changes the host.

How to run a tick: `docs/ops/eleads-parallel-loop.md`.

## Host gates

These bind every draft, offer, and send.

- Rooftops: Valdosta Nissan **28206** and Valdosta Mitsubishi **28546** only. Never The TKO Autogroup **6220**.
- Identity: **Bordine, Glenn** on every CRM action.
- SMS: **9:00 AM inclusive until 7:00 PM exclusive**, America/New_York, and **never Sunday**. The packet’s 7:00 AM–9:59 PM window, including Sunday, is not the host rule.
- Appointments: Mon/Wed/Thu/Fri **9:00 AM–6:30 PM**; Saturday **9:00 AM–5:30 PM**; never Sunday; Tuesday only when the customer asks, and only inside 9:00 AM–6:30 PM. Do not offer a time the host would reject, including weekday times after 6:30 PM and Saturday times after 5:30 PM.
- Another consultant’s lead: a qualifying contact newer than **72 hours** holds the profile. Eligibility still needs **96 hours** of zero contact by anyone, or 96 hours since the lead opened when there is no contact timestamp. 72 hours of silence is not permission to work the lead. After Titan has already touched the profile, use the normal follow-up cadence. Do not start a fresh 72-hour wait.
- Consent text that contains do-not-text, DNT, opt-out, unsubscribe, hard bounce, or the word Stop blocks the row for email and SMS together. Do-not-call closes phone only. Email and SMS still need their own permission.
- If the notes contain **unable to obtain funding**, hold the profile. The host does not look for a later inquiry once that phrase is in the text.
- Do not promise eligibility, approval, a payment, an appraisal value, a savings amount, an APR, a rebate, or availability. A live quote does not unlock `$`, `/mo`, `per month`, `APR`, or “I can get you.” The only incentive sentence the host allows is: “all possible qualifying incentives, rebates, and special manufacturer APR programs.” Keep it secondary to the appointment ask.
- Cloud and Linux agents do not open Eleads, do not type the password, and do not run the host. One live session belongs to the Alienware after Glenn’s check-in.

## Goal

Increase qualified foot traffic and confirmed sales, upgrade, and complimentary appraisal appointments for Bordine, Glenn. Follow-up is timely, personal, consent-aware, and verifiable in Eleads.

Success:

- Appointment commitments with an agreed day and time, then arrivals for appraisals, upgrades, and vehicle-selection visits.
- Fast answers to new leads and direct replies.
- Every send and completed task visible in CRM history. Email and SMS counted separately. Sales Performance / Daily Activity matches the work that actually happened.

Report measurable activity. Do not promise results.

## Queue

1. Select and verify the rooftop.
2. Open the Daily Organizer, status **Open**, contact type **All**.
3. Glenn-assigned work first, then store-wide only when authorized.
4. Capture customer, due time, task type, vehicle, assigned consultant, comments, and opportunity link.
5. Deduplicate tasks on the same customer and opportunity.

Rank:

| Rank | Opportunity | Objective |
| --- | --- | --- |
| 1 | Direct replies and explicit interest | Answer, then secure a day and time. |
| 2 | New leads | Approved first email and eligible SMS inside four minutes while the session is live. |
| 3 | Trade, appraisal, lease maturity, missed appointment, recent inquiry | Complimentary review and two future choices. |
| 4 | Qualified dormant new/used leads | Re-engage after full history and permission review. |
| 5 | Past owners and equity | Current ownership, appraisal, possible upgrade. |
| 6 | Birthday and vehicle anniversary | Relationship follow-up with an appraisal or upgrade invitation. |

Inside a tick, the host order is new leads, then planner, then overdue, then database. Ranks 1–2 are the fast lane. Ranks 3–6 wait until fast-lane proof is recorded.

## Ownership screen

- 14 months is the past-purchase guideline.
- A recent inquiry overrides that screen. Review it on its current intent.
- Model year alone never proves ownership duration.

Do not pursue: no further contact, prohibited channels, bought elsewhere and the search is over, explicitly not in market, or no valid permitted channel.

## Profile review

No profile qualifies from the list. Open the customer and the opportunity. Wait until the activity feed loads. If it does not load after one reload, mark **Manual verification required** and do not draft or send.

Read:

- Completed activities and notes, inbound and outbound SMS, calls and outcomes, email sends, opens, replies, and failures, last follow-up and last-modified timestamps.
- Appointments (active, missed, rescheduled, confirmed), recent inquiry and stated timing, sold, service, and ownership history, relationships and decision-makers, Lifetime Value and Equity.
- Vehicle of interest, new/used, owned vehicle and purchase timing, trade, mileage, payoff, appraisal request, financing, co-signer, documents, stock facts without inventing availability.
- Valid phone and email, do-not-call, do-not-text, opt-out, unsubscribe, hard bounce, Stop, explicit no-interest, assigned salesperson, and rooftop.

Glenn-owned profiles: prepare follow-up only after the latest email or SMS time and every permission check. No 12-hour minimum exists. Page 8 left that choice blank.

Other salesperson: use the exact activity timestamp, not only the task comment. Qualifying contact is a note, sent or received SMS, a call, a customer email, or an actively managed appointment.

## Messages

- Answer the latest question first.
- Use only verified customer, vehicle, trade, ownership, and timing facts.
- Email generally under 120 words. SMS short and conversational.
- One appointment ask. Two specific future choices inside the host appointment windows.
- Focus on a complimentary appraisal, ownership review, upgrade exploration, or vehicle-selection visit.
- If the original vehicle is a poor fit, offer verified alternatives only. Do not invent availability.

## Approval

Glenn approves every email and SMS before Send. The approval names the recipient, the channel, and the exact message, or it names an approved batch in which each row has those three fields. A prior approval does not cover a different customer or any revised wording.

Show one batch per tick. That is the packet’s “approved batch,” not a standing pass. Page 8’s “Every send | Batch” choice is still blank. Do not treat a blank choice as permission to send without the batch.

## Channels and proof

| Channel | When | Proof |
| --- | --- | --- |
| Email | Valid permitted email. No opt-out or hard bounce. Any hour. Approved row. | Sent activity, plus a separate completed task and note: `Manual email completed via system.` |
| SMS | Valid SMS-permitted phone. No DNT or Stop. Approved row. Host window only: 9:00 AM until 7:00 PM America/New_York, never Sunday. | Sent activity, plus a separate completed task and note: `SMS text completed via system.` |
| Phone | Valid callable number. No do-not-call. Autonomous calls need an approved telephony integration. | The real call outcome. Never mark a call completed if no call occurred. |

A draft, blank composer, preview, or processing screen is not delivery. The activity, the note, and the matching task must all show completed under Bordine, Glenn, and the activity must be checked against Sales Performance / Daily Activity.

Never force-close a task. If Eleads does not report real work, document the mismatch and escalate it. Do not invent a performance record.

If Eleads warns that a same-type task already exists, cancel the duplicate unless Glenn approves a second task. If a send result is uncertain, read history and do not click Send again.

## Safe-send sequence

1. Confirm rooftop, customer, sender, channel permission, recipient, subject, and message.
2. Use only a row from the approval batch for this tick.
3. Preview and confirm the final send control.
4. Click Send once.
5. Refresh activity history before any retry or the next send.
6. Verify the completed CRM entry under Bordine, Glenn, then write the separate task and note.

Email and SMS are separate sends and separate completed activities.

## Appointments

A solid appointment is a customer commitment to a specific day and time. Interest without a time stays follow-up. Do not set Appointment Set, and do not make Glenn primary, until that commitment is in the correspondence.

Pause and show Glenn the customer, rooftop, date and time, vehicle or appraisal purpose, arrival notes, and confirmation-call timing. Save only after he approves that card. Message approval does not save an appointment.

After that approval:

1. Create a Sales Appointment assigned to Bordine, Glenn.
2. Set opportunity progress to Appointment Set.
3. Keep the opportunity open.
4. Verify the scheduled activity and the Appointment Set status.
5. Check for an existing confirmation task before creating another.
6. Schedule the confirmation call about 24 hours before, or earlier the same day when the appointment is later that day.
7. For a same-day commitment, schedule confirmation before arrival.
8. After the appointment and the confirmation task are both verified, print page 1 only.

Arrival notes: vehicles of interest, trade year/make/model, mileage and payoff if given, appraisal request, co-signer or decision-maker attendance, documents they will bring, open questions without unsupported promises, who to ask for, and arrival instructions.

Dealership hours in the packet are 9 AM–7 PM Monday–Friday and 9 AM–6 PM Saturday. Offer times only inside the host windows above.

Space routine follow-up tasks at least five minutes apart when practical. Use the customer’s requested time when it falls inside the host window. Older inactive customers without a recent inquiry may use a four-working-day follow-up. Active opportunities may need next-day follow-up. Page 8 has no approved matrix by opportunity type.

## Daily cycle

1. **Verify** session, rooftop, and Bordine identity.
2. **Triage** replies, new leads, overdue, appointments, missed appointments, equity, and recent inquiries.
3. **Inspect** the full profile, history, permissions, vehicle, trade, and exact timestamps.
4. **Prepare** email and SMS with one ask and two future choices.
5. **Approve** the exact batch with Glenn.
6. **Execute** one send at a time, then verify history and the separate tasks.
7. **Convert** a day-and-time commitment only after its own approval.
8. **Clean up** finished customer tabs. Keep the dashboard and the work queues.

New-lead target: four minutes from organizer capture to the CRM-confirmed send, while the session is live and the batch is approved. If login, browser control, approval timing, or consent review breaks the target, record the cause. Do not claim it was met.

## Management report

CRM-confirmed counts only:

- Open tasks and distinct opportunities by rooftop.
- Glenn-assigned versus other-consultant.
- Qualified, held, excluded, and manual-verification, with reasons.
- Email totals and SMS totals, separate.
- Completed tasks on Sales Performance / Daily Activity.
- Appointment commitments, Appointment Set, confirmation tasks, shows, and sales when the CRM text in this tick states them.
- Uncertain sends, duplicates, reporting mismatches, and system errors.

## Resume

Unattended monitoring stops when the turn ends, the session expires, the browser disconnects, or the PC changes state. Resume with a fresh login, rooftop and identity verification, and a live history check.

## Worksheet still open

Page 8 is not new law. Do not fill the scorecard. Signature lines are blank.

| Topic | Working rule to follow now | Left blank on the packet |
| --- | --- | --- |
| Past-owner timing | 14 months; a recent inquiry overrides | Keep or Change |
| Glenn-owned spacing | Latest email/SMS time and permissions. No fixed interval | 12 hours or Other |
| Other salesperson | 72-hour hold plus 96-hour eligibility | Keep or Change on a 72-hour-only rule |
| Approvals | One exact batch per tick, each row named | Every send or Batch |
| SMS quiet hours | 9:00 AM until 7:00 PM, never Sunday | Keep or Change on 10:00 PM–6:59 AM |
| New-lead target | Four minutes while the session is live; record why if missed | Keep or Change |
| Confirmation timing | About 24 hours before; same-day before arrival | Keep or Change |
| Follow-up cadence | Four working days for older inactive customers without a recent inquiry; next day when the opportunity is active | Approve matrix or Revise |
| Scorecard | Report the fields above. Daily, weekly, and management targets stay empty | Blank targets |
