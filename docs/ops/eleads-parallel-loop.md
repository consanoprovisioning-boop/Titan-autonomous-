# Parallel loop

One Eleads browser on the Alienware. Cloud agents never open eleadcrm.com. Sub-agents get text packets only. They do not click, send, or share the browser.

Binding rules: `docs/ops/eleads-instructions.md`.

## Gates and fan-out

| Stage | Mode | Browser |
| --- | --- | --- |
| Verify | Sequential, once before any CRM action | Parent |
| Triage capture | Sequential. Daily Organizer, Open, All | Parent |
| Queue rank | One agent after the list exists | None |
| Profile capture | Sequential. Open each customer and wait for the feed | Parent |
| Inspect | Fan-out, one agent per profile, same turn | None |
| Prepare | Fan-out, one agent per qualified profile, same turn | None |
| Approve | One human batch. No Send before it | None |
| Execute | Sequential. One Send and one CRM write at a time | Parent |
| Proof read | Sequential page reads | Parent |
| Proof judge | Fan-out after those reads, same turn | None |
| Convert | Fan-out cards with the drafts. Save is a second Glenn gate, then one serial write | Parent, after approval |
| Clean up | Sequential, after this tick’s proof is on the report | Parent |

Fast lane is ranks 1–2 (direct replies, then new leads). Main lane is ranks 3–6 and starts only after fast-lane proof is recorded. Glenn-assigned work before store-wide work.

## Roles

Dispatch every independent agent in the same turn. Do not wait for one inspector before starting the next.

**queue-ranker**

- Input: organizer rows and the verified rooftop id.
- Output: a deduped queue split into fast lane and main lane.
- Must not: open CRM, send, write, or include 6220. List view never qualifies a profile.

**profile-inspector** (one per customer)

- Input: one captured profile, including whether the feed finished loading.
- Output: `qualified`, `held`, `excluded`, or `manual-verification-required`, plus the reason, permitted channels, and facts that are safe to quote.
- Apply the host gates: 72-hour hold, 96-hour eligibility, 14-month screen, funding phrase, and do-not-pursue.
- Must not: open CRM, send, qualify a profile whose history did not load, or invent vehicle, price, APR, rebate, or appraisal facts.

**draft-writer** (one per qualified profile)

- Input: that inspector result only.
- Output: separate email and SMS rows. Each row has recipient, channel, full message, one appointment ask, and two future choices inside the host windows. Email generally under 120 words. SMS only inside 9:00 AM until 7:00 PM America/New_York, never Sunday.
- Must not: send, change the recipient, draft a held, excluded, or manual profile, or use a forbidden promise.

**proof-judge** (one per customer)

- Input: the parent’s post-send activity, task, note, and Sales Performance / Daily Activity text.
- Output: `confirmed`, `uncertain`, or `mismatch` per channel. Email and SMS stay separate. `confirmed` means the activity, the note, and the matching task are completed under Bordine, Glenn.
- Must not: open CRM, resend, force-close, or treat a composer as proof.

**appointment-packer** (one per reply that already names a day and time)

- Input: inspector facts plus the agreed day and time.
- Output: one card with customer, rooftop, date and time, purpose, arrival notes, and confirmation-call timing.
- Must not: save, set Appointment Set, print, or create the confirmation task. Interest without a time stays follow-up.

## Parent checklist

1. Verify the session, rooftop 28206 or 28546, and Bordine, Glenn. Stop on 6220 or any other user. If the rooftop changes later, stop and verify again.
2. Capture the organizer. Dispatch **queue-ranker**.
3. Fast lane: capture each profile serially. One reload if the feed is still spinning. Then dispatch every **profile-inspector** in one turn.
4. In one turn, dispatch every **draft-writer** for qualified profiles and every **appointment-packer** for an agreed day and time.
5. Show Glenn one approval batch. Wait. The send list is only the approved exact rows.
6. For each approved row, serially: confirm rooftop, customer, sender, permission, recipient, subject, and message; preview; click Send once; refresh history before the next row. Email and SMS are separate sends.
7. When the activity is visible, write the matching completed task and note: `Manual email completed via system.` or `SMS text completed via system.` Cancel a same-type duplicate warning. A second task needs Glenn’s approval.
8. Read activity, task, and Sales Performance / Daily Activity for each customer in the tick. Then dispatch every **proof-judge** in one turn.
9. Record proof. The fast-lane tick is closed.
10. Repeat steps 3–9 for one main-lane slice.
11. For approved appointment cards only: create the Sales Appointment under Bordine, Glenn, set Appointment Set, keep the opportunity open, verify both, schedule one confirmation call, then print page 1 only.
12. Close finished customer tabs. Keep the dashboard and the work queues.

## Merge

**Approval batch.** One table. One row per channel: customer, rooftop, recipient, channel, full message, two appointment choices, permission basis. Held, excluded, and manual rows go in a side list. Appointment cards are a second batch.

**Report.** Write it after the proof-judges return. Use the field list in `docs/ops/eleads-instructions.md`. Count email and SMS only when `confirmed`, and count them separately. Leave shows and sales blank unless this tick’s CRM text states them.

## Exceptions

**History will not load.** After one reload, mark `manual-verification-required`. Do not treat the customer as untouched. Do not draft or send. Leave the task open.

**Send uncertain.** Do not click Send again. If the activity is already under Bordine, Glenn, complete the separate task and note and send that packet to proof-judge. If it is missing or ambiguous, mark `uncertain`, leave the task open, and report the mismatch.

**Four-minute target missed.** Record system, login, browser, approval timing, or consent review. Still wait for the batch. Lateness is not a reason to skip a gate.

## Cadence

A tick is one lane slice through approval, serial send, and recorded proof. Start the next tick only after this tick’s proof lines are on the report and cleanup is done. Then recapture the organizer while the session is live. Direct replies and new leads are the next tick whenever they are present. The four-minute clock runs from organizer capture to the CRM-confirmed send.
