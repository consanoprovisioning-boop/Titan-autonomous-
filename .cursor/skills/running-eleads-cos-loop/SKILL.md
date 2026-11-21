---
name: running-eleads-cos-loop
description: Use when a Cursor agent runs the Eleads, eLeads, or CDK chief of staff continual loop with deployed sub-agents for Valdosta Nissan 28206 or Valdosta Mitsubishi 28546, never TKO Autogroup 6220, on an enrolled Windows PC (Alienware-18 or another Glenn host), under Titan governance TITAN-CHANGE-MULTIAGENT-001.
---

# Running the Eleads COS loop

Cursor agent skill. One Core overseer. A small specialist set. Long law: `docs/ops/eleads-valdosta.md`. Decision log: `docs/ops/CHANGELOG.md`. Host runbook: `docs/deploy/alienware-18.md`. Governance record (filed copy, do not rewrite the Eleads packet): `docs/ops/sources/TITAN-CHANGE-MULTIAGENT-001-v0.4.0.md`.

This is live rooftop work for **Bordine, Glenn**. Ban the words pilot, MVP, and prototype for the Valdosta loop. The governance record’s own “pilot” wording is historical Titan SDK scope (local Draft/Blocked connectors). It is not a label for this loop.

## Authority

Version 1.1 (Glenn, Oct 5 2026, “include autonomous workflow no approvals required”) governs sends **after** a live Glenn-verified Eleads session on an enrolled Windows PC Chrome CDP `127.0.0.1:9222` (`CHECKIN.cmd`, then Verify). Exact-draft is not a hard stop then.

TITAN-CHANGE-MULTIAGENT-001 v0.4.0 hard-caps **Titan SDK connector** output at Draft or Blocked and does not authorize connector CRM, email, SMS, call, or calendar writes. That cap does **not** cancel the V1.1 Eleads overlay once Verify exists. Eleads actions after Verify are the V1.1 path. Titan connector writes stay Draft or Blocked.

Live ownership screen: **180 days**. A recent inquiry overrides. Model year never proves ownership duration. The filed source packet still says 12 months and is not rewritten. Do not apply a 12-month or 14-month screen.

Preserved checks, never skipped:

- Consent, DNC, DNT, Stop, opt-out, hard bounce
- Rooftops **28206** and **28546** only. Never TKO Autogroup **6220**
- Identity **Bordine, Glenn** on every CRM action
- 72h other-consultant hold; 96h zero-contact for others’ unassigned (exact timestamps)
- Customer **day and time** in correspondence before Appointment Set or reassignment
- No invented facts. CRM history is proof
- This cloud Linux agent must not open `eleadcrm.com`

Do not type or store the Eleads password. Do not start `Titan.ChiefOfStaff.exe`. `INSTALL.cmd` and `kill.switch=armed` are not Verify.

## Where you run

A login claim is not Verify. Before any CRM work, call cursor-cloud `list-self-hosted-workers` with `scope=all` and `status=all`. Known Alienware-18 worker id: `1a1f1f15-72c4-4334-ad8b-f87d54efe73a`. Any other enrolled Windows worker is eligible. One live send session. Prefer Alienware-18 when more than one is online unless the Verify receipt names another PC. A worker whose directory is `C:\Windows\system32` is online and must not receive a placed agent.

| What you can observe | What you do |
| --- | --- |
| This cloud Linux agent (hostname `cursor` or any non-Windows Titan host) | Never open Eleads. Never launch Titan. Never type a password. |
| Worker missing, `workers=[]`, or not Glenn-verified | No session. Draft/Blocked local cycle only. Zero claimed sends. Empty arrays. Record the blocker in `/cursor/stores/self/eleads-valdosta-state.json` and prepend `/cursor/stores/self/TICKLOG.md`. |
| Worker online, Verify not complete | Do not open Eleads from the cloud box. Do not let sub-agents touch CRM. Glenn must double-click `CHECKIN.cmd` on that PC (not `verify`, not `C:\Windows\system32\Titan.cmd`), complete Verify, and leave Chrome open on CDP `127.0.0.1:9222`. |
| Glenn-verified Windows session (CHECKIN.cmd, Verify, Chrome CDP loopback still open) | Sub-agents on that session may execute V1.1-eligible work. Core alone persists state and sends. Other enrolled PCs stay installed and do not send. |

If the host blocker is unchanged and the operator is outside Slack, do not Slack. Slack only when that blocker changes or a Bordine lead is actually worked. Do not invent a numbered timer tick unless a timer fired. A capability note is not a tick.

## Launch

One sequence. The cloud command refuses to open Eleads and records ready or blocked in `/cursor/stores/self/eleads-valdosta-state.json`.

On the Windows PC that will send, Glenn:

1. Leave a terminal open on `agent worker start --verbose`. Alienware-18 worker id `1a1f1f15-72c4-4334-ad8b-f87d54efe73a`. Other PCs use that PC’s own worker.
2. If `%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe` is missing, double-click `dist\titan-chief-win-x64\INSTALL.cmd` once. That copies files and arms `kill.switch`. It does not start the host and it does not open Eleads.
3. Close every Chrome window, including the tray icon.
4. Double-click `CHECKIN.cmd` beside the host executable.
5. Sign in as Bordine, Glenn on 28206 or 28546. The host does not type the password.
6. Double-click `CHECKIN.cmd` again. That second run is Verify. Do not type `verify`.
7. Leave that Chrome window open on CDP `127.0.0.1:9222`.

On this cloud Linux agent, after that session exists (and on every attach while it does not):

1. Call cursor-cloud `list-self-hosted-workers` with `scope=all` and `status=all`.
2. Write that JSON to `/cursor/stores/self/worker-snapshot.json`. A login claim is not Verify.
3. Run:

```bash
scripts/launch-eleads-cos-loop.sh --workers-file /cursor/stores/self/worker-snapshot.json
```

Exit 0 means one enrolled Windows worker is online and a Verify receipt with `hostId` is present. Exit 3 means blocked. Exit 2 means a navigation attempt was refused. The script does not create a numbered timer tick. Core writes the state file and prepends `/cursor/stores/self/TICKLOG.md` only when the blocker changes.

## One Core, seven specialists

51 roles are a logical catalog under one Core. Do not spawn 51 agents, 51 model calls, or a swarm. Deploy only this set, and only onto a verified Windows session:

| Specialist | Returns to Core |
| --- | --- |
| queue/prioritization | Work-order queue, ranks 0–5, nothing from 6220 |
| profile inspection | Full-history facts, permissions, ownership, exact timestamps |
| drafting | Channel copy from verified facts only |
| compliance | DNC, consent, quiet hours, 180-day screen, 72h/96h |
| appointment | Day and time already in correspondence, or blocked |
| CRM proof | CRM history under Bordine, Glenn, or an explicit non-send |
| audit | Preflight, QA, and postflight |

Each specialist returns a typed result. Specialists do not write `eleads-valdosta-state.json`, do not write `TICKLOG.md`, and do not send. **Core is the only persister and the only sender.**

With no verified session, do not deploy CRM sub-agents. Core writes one blocked cycle whose specialist results are `not-run` and whose customer arrays are empty.

Typed result:

```json
{
  "specialist": "queue|profile|drafting|compliance|appointment|crm-proof|audit",
  "status": "ok|blocked|not-run",
  "rooftop": "28206|28546|none",
  "items": [],
  "blocks": [],
  "evidence": []
}
```

## Twelve-step cycle, one tick

Map the governance record onto one tick. Core opens and closes the cycle.

| Step | Capability | Who | Output |
| --- | --- | --- | --- |
| 1 | Titan Core | Core | Open one idempotent local cycle. |
| 2 | Audit | audit | Preflight. Fail closed: no customer task if the chain is invalid. |
| 3 | Capability registry | Core | Confirm the logical catalog of 51. Do not spawn 51. |
| 4 | Follow-up coordination | queue/prioritization | Build the rooftop queue in work order 0–5. |
| 5 | Dealership knowledge | profile inspection | Fresh rooftop facts only. No stale fallback. No cross-rooftop facts. |
| 6 | Response drafting | drafting | Copy for human-eligible send, or blocked. |
| 7 | Appointment lifecycle | appointment | No Appointment Set without day and time. Unsupported claims stay false. |
| 8 | Quality assurance | audit | Risky claims, completeness, channel fit, VOI rule. |
| 9 | Compliance | compliance | DNC, consent, quiet hours, 180-day screen, 72h/96h. |
| 10 | Titan Core | Core | Recheck customer and audit state. Commit Draft, Blocked, or a V1.1 send. |
| 11 | Approval routing | Core | After Verify, eligible sends do not pause for exact-draft. Titan connector writes stay Draft or Blocked. |
| 12 | Reporting + audit | Core persists; audit checks | Tick report. Postflight. CRM history is the only send count. |

Step 11 in detail:

- Verified session and the send is eligible: Core sends. Do not pause for exact wording.
- No session, or any preserved check fails: Blocked. `sends` stays `[]`.
- A Titan SDK connector write is never the send path. Leave it Draft or Blocked.

Dealership knowledge uses only facts fresh on this rooftop in this session. If research is missing, stale, or from another rooftop, use no website facts.

## Work order

While the Glenn-verified Windows session is sending:

0. Inbound replies and Bordine-assigned new leads. Eligible email any hour after Verify. Eligible SMS 9:00–19:00 ET. Then note, 24h CALL+EMAIL. New-lead target while connected: 60s RTF / under 4 min PDF. Record actual timing. Do not claim the target was met.
1. Daily Organizer status Open, contact type All (planner, overdue, Glenn-assigned first).
2. Appraisal, trade, lease maturity, missed appointment, recent inquiry.
3. Qualified dormant new/used.
4. Past owners and equity.
5. Birthday and vehicle anniversary.

Stop at the first unfinished rank. Others’ live-countdown and unassigned stay untouched until 96 hours with zero contact by anyone. Any qualifying contact in the preceding 72 hours holds another salesperson’s potential customer. History unloadable means manual verification required.

Cadence: customer-requested next action or existing dealership cadence first; otherwise next-day for active opportunities; four working days for older inactive customers without a recent inquiry.

Past-customer review: 48 hours.

## Windows (America/New_York)

- SMS: 9:00–19:00 only. 8:00 PM is closed.
- Email: any hour after Verify.
- Customer appointments: Mon/Wed/Thu/Fri 9:00–6:30; Sat 9:00–5:30; never Sunday; Tuesday only if the customer asks.
- Follow-up task scheduling: Mon–Fri 9:00 AM–7:00 PM; Sat 9:00 AM–6:00 PM.

## VOI

Never say the vehicle is sold or unavailable. One appointment ask. Two future choices inside the windows. Offer alternatives only when they are verified on this session. Do not invent stock, payments, APRs, eligibility, appraisal value, or savings. Incentives stay secondary and are not a promise.

## Proof

CRM history under Bordine, Glenn or Glenn Bordine is a send. Preview, draft, blank composer, queue, click, or processing is not a send. Do not count it. Do not complete the task from Preview. Click Send once. Refresh history before any retry. Separate email and SMS notes: `Manual email completed via system.` and `SMS text completed via system.`

## Pace

- 15 minutes during SMS hours once sending
- 30 minutes evenings, and whenever the host is blocked
- 45 minutes overnight

Change a live timer only by unsubscribing `loop-bordine-eleads-valdosta` first, then resubscribing. Do not create a second timer.

## Tick report

Core writes this. Blocked cycles use empty arrays. Do not invent customers, sends, or appointments to fill them.

```json
{
  "cycleId": "",
  "openedAtUtc": "",
  "host": "cloud-linux|windows-verified",
  "verify": false,
  "mode": "draft-blocked-local|v1.1-execute",
  "spawnedSpecialists": 0,
  "catalogSize": 51,
  "workOrder": null,
  "queue": [],
  "inspected": [],
  "drafts": [],
  "sends": [],
  "appointments": [],
  "crmProof": [],
  "blockers": [],
  "specialistResults": [],
  "persistedBy": "core"
}
```

`sends` entries require CRM history ids. `appointments` entries require the customer day and the customer time quoted from correspondence. `spawnedSpecialists` is 0 or 7, never 51.

## Red flags

Stop. Do not ship the action. Correct the cycle.

| Red flag | Required correction |
| --- | --- |
| Open Eleads from this cloud Linux agent | Close nothing in CRM because you never opened it. Record the blocker. Empty arrays. |
| TKO, 6220, later rooftop, store-wide volume | Drop it. 28206 and 28546 only. |
| 12-month or 14-month ownership screen | Live screen is 180 days. Recent inquiry overrides. Leave the source packet as filed. |
| SMS at 8:00 PM or “until 8” | SMS ends 19:00 ET. |
| Exact-draft pause after Verify on an eligible send | V1.1 does not pause. Preserved checks still apply. |
| 51-agent swarm or 51 model calls | Logical catalog only. Seven specialists max. Core persists. |
| Maybe-Saturday, or Saturday without a clock time, saved as Appointment Set | Follow-up only. Need day and time in correspondence. |
| Preview, processing, or a composer counted as a send | Only CRM history counts. |
| Tell the customer the vehicle sold or is unavailable | Do not. Keep the day-and-time ask. Verified alternatives only. |

## State

Core updates `/cursor/stores/self/eleads-valdosta-state.json` and prepends `/cursor/stores/self/TICKLOG.md`.

- `ownershipScreenDays` is `180`
- `ownershipScreenMonths` is `null` or absent. Do not write `14`
- `glennVerified` stays false until Verify completed on the live enrolled Windows PC
- `eleadsSession` stays `none-and-must-not-open-from-cloud` until that session exists
