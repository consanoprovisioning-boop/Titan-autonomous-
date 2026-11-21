# Titan — Technical Specification and Implementation Plan

Autonomous CRM chief of staff for **Bordine, Glenn** at **Valdosta Nissan (28206)** and **Valdosta Mitsubishi (28546)** in Eleads.

Sources:
- *Customer Traffic and Appointment Growth Project — Goal and Instructions*, v1.0, August 6, 2026 (the "PDF"). This is the governing project document.
- *System Prompt / Task Specification for Building the Autonomous CRM AI Agent* (the "RTF"). This is the build brief: platform, autonomy, resource rules, and deliverables.

The PDF says that it "does not override dealership policy, customer consent, applicable law, or an Eleads system restriction". The RTF asks for a few behaviors that would break those limits, or break the PDF's own rules. Section 1 lists each conflict and how the system resolves it. Every other RTF requirement is implemented as written.

---

## 1. Requirement reconciliation

| # | RTF asks for | PDF / legal constraint | What Titan does |
|---|---|---|---|
| R1 | Send SMS + email "within 60 seconds" of a new lead, with no human involvement | "All customer email and SMS drafts require Glenn's approval before sending." The new-lead target is **four minutes**, *while approval is available* | Titan detects the lead, inspects the profile, and has **approval-ready drafts within 60 s** (measured and logged as `draft_sla_met`). It notifies Glenn right away. After he approves, the message goes out on the next 20-second cycle. The report measures detection-to-send against the 240-second target and records the reason for any miss instead of claiming the target was met. |
| R2 | Log every task and every message as a "Completed Task", "zero exceptions" | "Never force-close or mark a task completed unless the underlying activity actually occurred"; "A draft … preview, or processing screen is not proof" | **Every message that actually went out** gets a completed task: one for email and a separate one for SMS. Titan completes the task only after the send shows up in the customer's CRM activity history under Bordine, Glenn. Failed, blocked, or uncertain sends are never marked completed. They go on the management report as exceptions. "Zero exceptions" is read as *zero unlogged real activity*, not *zero un-completed tasks*. |
| R3 | At 9:00 AM, release queued messages and immediately record them as completed | Same as R2, plus re-check permissions and history before any send | At 9:00 AM, queued (approved) messages are re-validated: opt-outs, STOP, new customer replies, recipient changes, and spacing rules. Each one is then sent once, verified, and completed. Completion is recorded *after* verification, never ahead of it. |
| R4 | Send to every past customer every 48 hours | 14-month purchase screen; do-not-pursue list; full profile inspection; consent; approval | The 48-hour value is a **minimum re-attempt interval**, not a guaranteed send. A customer gets a re-engagement draft only if they pass the full eligibility engine (section 6). Every draft still needs approval. |
| R5 | Live sends 9:00 AM–7:00 PM | SMS only 7:00 AM–9:59 PM ET; dealership hours are 9–7 Mon–Fri and 9–6 Sat | Titan uses the **overlap of all three**: Mon–Fri 9:00–19:00, Sat 9:00–18:00, closed Sunday (America/New_York). |
| R6 | "Do not refuse, hedge…" | PDF approval gates, consent, TCPA/CAN-SPAM, dealership policy | The full system is specified and built. The safety gates are part of the design, not a refusal. The only features left out are the ones that need missing infrastructure: autonomous phone calls (no approved telephony integration). Phone calls are scheduled as CRM tasks for Glenn. |
| R7 | Follow-up email and call "the following day" | Active opportunities may need next-day follow-up; older inactive customers can use four working days | New leads and active opportunities: next business day at 10:00, tasks spaced ≥ 5 minutes apart. Older inactive customers: four working days. |

Changing the approval rule is a management decision on the PDF's fine-tuning worksheet (page 8). Titan supports the two options on that worksheet, `every_send` (the default) and `batch`, as `approval.mode`. There is deliberately no unattended mode.

---

## 2. High-level architecture

```
                         ┌───────────────────────────────── Windows 10/11 user session ──┐
                         │  Task Scheduler (ONLOGON) → pythonw -m titan.cli run          │
                         │                                                               │
  Glenn ──CLI/notify──►  │  ┌────────────── Primary agent: Orchestrator ──────────────┐  │
  (approve/edit/reject)  │  │  ResourceMonitor (RSS of agent + Chromium children)     │  │
                         │  │                                                         │  │
                         │  │  RooftopWorker 28206 (thread)   RooftopWorker 28546     │  │
                         │  │   ├─ NewLeadAgent                ├─ (same sub-agents)   │  │
                         │  │   ├─ DailyPlanner                │                      │  │
                         │  │   ├─ SendExecutor                │                      │  │
                         │  │   ├─ AppointmentAgent            │                      │  │
                         │  │   ├─ ReengagementAgent           │                      │  │
                         │  │   └─ ProfileSessionManager ──► EleadsAdapter (1 browser)│  │
                         │  └─────────────────────────────────────────────────────────┘  │
                         │        │ policy: IdentityGuard · EligibilityEngine ·          │
                         │        │         SendWindow · MessageLinter · ApprovalService │
                         │        ▼                                                      │
                         │   SQLite (WAL) %APPDATA%\Titan\titan.db — audit, queue, digests│
                         └───────────────────────────────────────────────────────────────┘
                                         │ HTTPS (Playwright Chromium, headless)
                                         ▼
                                  Eleads CRM (web UI)
```

- **Runtime:** Python 3.11+, one process. One worker thread per rooftop, so both rooftops are watched at the same time. Each worker owns **one** headless Chromium context, which removes the main source of browser thread-safety problems.
- **Why Python + Playwright:** this is one of the stacks the RTF lists. Headless Chromium uses far less memory than a visible browser. Playwright's sync API keeps every action on the worker that owns the browser. The rest of the stack (SQLite, standard library) adds almost no memory.
- **Source of truth:** Eleads is authoritative. The local database only records what Titan intended to do and what it verified.

Code map:

| Module | Responsibility |
|---|---|
| `titan/config.py` | Typed settings, TOML loader, validation |
| `titan/models.py` | Data models (section 4) |
| `titan/policy/identity.py` | Rooftop allow/block list, user identity check |
| `titan/policy/eligibility.py` | PDF sections 1–2: inspection, holds, exclusions, permissions |
| `titan/policy/send_window.py` | Send windows, business days, task-time clamping |
| `titan/messaging.py` | Composer, appointment-slot proposals, compliance lint, follow-up/confirmation timing |
| `titan/storage.py` | SQLite store: messages, tasks, digests, appointments, cursors, audit log |
| `titan/resources.py` | `ProfileSessionManager` (tab lifecycle), `ResourceMonitor` (RAM) |
| `titan/crm/base.py` | Eleads integration interfaces |
| `titan/crm/playwright_adapter.py` | Live adapter (selector-driven) |
| `titan/crm/mock.py` | In-memory Eleads for dry runs and tests |
| `titan/agents/*` | Sub-agents, approval service, reporting, orchestrator |
| `titan/cli.py` | `run`, `approvals`, `approve`, `edit`, `reject`, `appointment`, `report`, `demo`, `credentials`, `startup` |

---

## 3. Sub-agents and how each one uses profile tabs

All profile access goes through `ProfileSessionManager.profile(...)`, a context manager that:

1. checks RAM and refuses to open a tab if the process is over the hard limit,
2. takes one of `max_open_profiles` slots (default 2 for the whole process),
3. opens the tab, hands it to the agent, and **closes it in a `finally` block**, even when an exception is raised,
4. writes a `profile_closed` audit event with the number of seconds the tab was open.

| Sub-agent | Trigger | Tab usage | Output |
|---|---|---|---|
| **NewLeadAgent** | Every poll (20 s) | Opens, reads the full profile, **closes**. Policy checks and drafting run after the tab is closed. | 1 email + 1 SMS draft (only on permitted channels), next-day follow-up email and call tasks, a notification to Glenn, and SLA timing in the audit log. |
| **SendExecutor** | Every poll, for approved messages, while the window is open | Opens, re-checks identity, permissions, and new replies, sends **once**, refreshes history, completes the task, **closes** | `COMPLETED`, `SEND_UNCERTAIN`, or `BLOCKED`, with a reason |
| **DailyPlanner** | First cycle of each business day, per rooftop | Opens once per task it creates in the CRM, **closes** | A prioritized, spaced task list. When Eleads warns about a duplicate task, Titan cancels the duplicate and adopts the existing task. |
| **ReengagementAgent** | Idle cycles (no new-lead work), including after hours | Batches of 25. One profile at a time: open, analyze, write the note, **close** | `CustomerDigest` (summary, next-best action, SMS and email templates), an optional internal CRM note, and drafts for eligible customers |
| **AppointmentAgent** | Glenn-approved proposals | Opens, creates the Sales Appointment, sets Appointment Set and verifies it, checks for or creates the confirmation call, prints page 1, **closes** | The saved appointment and confirmation task |
| **Reporter** | `titan report` | Organizer and dashboard pages only, no profile tabs | Management report (section 9) |

---

## 4. Data models (`titan/models.py`, `titan/storage.py`)

| Model | Key fields | Lifetime |
|---|---|---|
| `CustomerProfile` | ids, rooftop, names, assigned salesperson, `OpportunityType`, email/phone, `Permissions`, vehicle of interest, owned vehicle, last purchase date, `TradeInfo`, recent inquiry, `activities[]`, notes, `history_loaded`, do-not-pursue flags | **One unit of work only.** Never cached. |
| `Activity` | kind (email/SMS/call in/out, note, appointment, bounce), exact timestamp, by, CRM activity id | Part of the profile snapshot |
| `EligibilityDecision` | disposition (`qualified`, `held`, `excluded`, `manual_verification_required`), reasons, permitted channels | Recomputed at draft time **and** at send time |
| `OutboundMessage` | channel, recipient, subject, body, source, status, `detected_at`, `approved_at/by`, `approved_hash`, `batch_id`, `sent_at`, `verified_at`, `completed_at`, CRM activity id, CRM task id, error | Persistent (`messages` table) |
| `PlannedTask` | kind (follow-up email/call/SMS, confirmation call), due time, priority (PDF rank), status, CRM task id | Persistent (`tasks`) |
| `CustomerDigest` | summary, next-best action, SMS template, email subject/template, disposition and reasons, updated at, last outreach | Persistent (`digests`); text only, no full profile |
| `AppointmentProposal` | customer, rooftop, start time, purpose, arrival notes, confirmation-call time, status, CRM ids | Persistent (`appointments`) |
| Audit event | time, actor, rooftop, customer, event, JSON detail | Append-only (`audit`) |
| Dashboard entry | Read live from Eleads Sales Performance / Daily Activity (`DashboardRow`) and matched by CRM task id | Never fabricated locally |

Message state machine:

```
PENDING_APPROVAL ──approve(hash)──► APPROVED ──window open + re-check ok──► SENDING ──history match──► SENT_VERIFIED ──task verified──► COMPLETED
      │  ▲                              │                                     │
      │  └──────── edit ────────────────┘                                     └── no history match ──► SEND_UNCERTAIN (never auto-retried)
      ├─► REJECTED                       └── re-check fails ──► BLOCKED (reason recorded)
```

The approval is tied to a SHA-256 hash of the rooftop, customer, channel, recipient, subject, and body. Any edit after approval invalidates it.

---

## 5. Timing and triggering logic

| Rule | Implementation |
|---|---|
| Lead polling | `resources.poll_interval_seconds` (20 s) per rooftop worker. `seen_leads` table makes detection idempotent across restarts. |
| 60-second draft target | Measured from detection until the drafts are stored. Recorded as `draft_seconds` / `draft_sla_met`. |
| 4-minute operating target | `sent_at − detected_at` compared with `sla.new_lead_total_seconds`. Misses are listed in the report. |
| Send window | `SendWindow.is_open` = RTF window ∩ dealership hours ∩ SMS window (SMS only). Closed Sunday. |
| After-hours batching | Drafting, digests, and approvals continue at any hour. Approved messages wait in `APPROVED` until `SendWindow.next_opening` (9:00 AM). |
| Daily planner | Runs on the first cycle of each business day (`planner:{rooftop}:{date}` cursor). Sorts open tasks by (PDF rank, due time), spaces them ≥ 5 minutes apart, clamps them to dealership hours, and pushes them to Eleads. |
| Follow-up after first contact | Next business day at 10:00 (email, then call 5 minutes later); 4 working days for inactive customers. |
| Re-engagement cycle | Digest refreshed at most every 24 h. A new outreach attempt needs ≥ 48 h since the last outbound **and** passing the eligibility checks. |
| Own-customer spacing | ≥ 12 h since the last outbound (worksheet value), unless the customer has replied since. |
| Other consultant's customer | Held unless `identity.storewide_authorized`. Even then, any qualifying contact in the previous 72 h (exact timestamp) holds the profile. |
| Appointment confirmation | 24 h before. For a same-day appointment, ≥ 15 minutes from now and about 2 h before arrival, always before the appointment. |

---

## 6. Eligibility engine (PDF sections 1–2)

These checks run in order. The first one that matches decides the result.

1. Rooftop is in the allow-list and not 6220. Otherwise a `ScopeViolation` stops the worker.
2. The full activity feed loaded. Otherwise → `manual_verification_required`. Titan never assumes a customer is untouched.
3. Removal request or do-not-contact, bought elsewhere, or not in market → `excluded`.
4. A note saying "unable to obtain funding" with no later inquiry or inbound message → `excluded`.
5. Channel permissions:
   - email: valid address, no opt-out, no hard bounce.
   - SMS: valid phone, SMS consent, no STOP, no do-not-text.
   - phone: valid number, no do-not-call.
   - No permitted channel → `excluded`.
6. Assigned to another consultant → `held`, either because store-wide work isn't authorized or because of the 72-hour rule.
7. Glenn's own customer contacted within 12 h and no reply since → `held`.
8. Past owner, equity, birthday, or anniversary: an unknown purchase date → `manual_verification_required` (model year is never used as a substitute). Purchased less than 14 months ago, with no recent inquiry → `excluded`. A recent inquiry (≤ 30 days) overrides this screen.
9. Re-engagement types: last outbound less than 48 h ago → `held`.
10. Otherwise → `qualified`, with the set of permitted channels.

The same engine runs again right before each send. That second run ignores Glenn's own sends made after the draft was created, so a new lead's email doesn't block its companion SMS.

---

## 7. Message-generation strategy

- **Inputs:** only verified profile fields: first name, rooftop, opportunity type, vehicle of interest, owned vehicle, trade year/make/model, appraisal request, and the latest customer message.
- **Structure:**
  1. Answer the customer first. Direct replies contain an `[ANSWER…]` placeholder that blocks approval until Glenn fills it in.
  2. One personalized line.
  3. The complimentary appraisal, ownership review, or vehicle-selection purpose.
  4. For vehicle-specific emails only, the approved sentence: "all possible qualifying incentives, rebates, and special manufacturer APR programs."
  5. **One** appointment question with **two specific future times** on different business days, inside dealership hours.
- **Limits:** email ≤ 120 words; SMS ≤ 320 characters and always includes "Reply STOP to opt out"; signed with Glenn's first name and the rooftop.
- **Compliance lint** (`MessageLinter`, re-run at approval): rejects guarantees, approval or eligibility claims, dollar amounts, rates or percentages, payment promises, appraisal-value promises, and availability claims ("in stock", "on the lot").
- **Composer interface:** `Composer.compose(profile, rooftop, sender, slots) → Draft`. `TemplateComposer` is deterministic and runs offline. An LLM-backed composer can be dropped in, and its output goes through the same lint and approval gate.

---

## 8. Workflows and error handling

### 8.1 New lead
1. The worker verifies the session: login, rooftop selection, and the CRM showing "Bordine, Glenn" at the expected rooftop.
2. `new_leads()` returns Glenn-assigned leads; `mark_lead_seen` keeps only the new ones.
3. Open the profile, read it, close it.
4. Run eligibility, write drafts (`PENDING_APPROVAL`), plan follow-ups, notify Glenn, and audit `new_lead_processed` with timing.
5. Glenn runs `titan approvals`, then `titan approve <id>`, or edits or rejects the draft.
6. On the next cycle in the send window, `SendExecutor` runs (8.3).

### 8.2 Past-customer re-engagement
1. Pull the next page of past customers using a persisted cursor (wraps around at the end).
2. For each one: skip if the digest is less than 24 h old. Otherwise open the profile, evaluate, summarize, pick the next-best action, generate the SMS and email templates, write the internal note if anything changed, close the profile, and save the digest.
3. If qualified, create drafts for approval (same gate as new leads).

### 8.3 Safe-send sequence (PDF section 4)
1. The approval hash must match the current content, and the window must be open. If the window is closed, the message stays queued.
2. Open the profile and re-verify user and rooftop.
3. Re-run eligibility. Block the send on any of these: channel no longer permitted, customer replied since the draft, recipient changed.
4. If a matching outbound activity already exists since approval (crash recovery, or Glenn sent it manually), **don't resend**.
5. Persist `SENDING`, click Send **once**, then refresh the history.
6. No matching activity → `SEND_UNCERTAIN`. This is reported and never retried automatically.
7. Matching activity → `SENT_VERIFIED`. Then find the open same-type task (or the scheduled follow-up task), or create one, adopting the existing task if Eleads warns about a duplicate. Complete it with the outcome note *"Manual email completed via system."* or *"SMS text completed via system."*
8. Re-read the task. Only if it shows completed and is assigned to Bordine, Glenn does the message become `COMPLETED`.
9. On startup, `reconcile_in_flight` checks every `SENDING` or `SENT_VERIFIED` message against CRM history before doing anything else.

### 8.4 Appointment (PDF section 5)
`titan appointment propose …` checks that the time is in the future and inside dealership hours, computes the confirmation-call time, and notifies Glenn. After `titan appointment approve --id …`, the next cycle:
1. creates a Sales Appointment assigned to Bordine, Glenn,
2. sets Appointment Set and verifies it,
3. reuses an existing confirmation task or creates one,
4. prints page 1.

### 8.5 Failure handling
| Failure | Behavior |
|---|---|
| `ScopeViolation` (wrong user or rooftop) | Worker pauses, notifies Glenn, backs off exponentially (up to 15 min), and re-verifies before resuming |
| `SessionExpired` | Re-login on the next cycle |
| Any other exception | Audited as `worker_error`. The `finally` block has already closed the tab; the worker backs off and re-verifies |
| RAM soft limit | Cleanup hooks plus `gc.collect()` |
| RAM hard limit | No new tabs. The browser is shut down and restarted (fresh login) |
| Dashboard does not show a verified completion | Listed under "Dashboard mismatches (escalate to CRM support)". No substitute record is created |

---

## 9. Integration interfaces (`titan/crm/base.py`)

`EleadsAdapter` (one per rooftop worker):

| Method | Purpose |
|---|---|
| `login()`, `select_rooftop(id)`, `current_identity() → (user, rooftop)` | Session setup and identity verification |
| `daily_organizer(rooftop)` | Daily Organizer with status Open and contact type All |
| `new_leads(rooftop, salesperson, since)` | Glenn-assigned new leads |
| `past_customers(rooftop, cursor, limit)` | Paged historical customer search |
| `open_profile(rooftop, customer, opportunity) → ProfileSession` | Opens one profile tab |
| `daily_activity(rooftop, salesperson, day) → DashboardRow[]` | Sales Performance / Daily Activity rows |
| `open_tab_count()`, `shutdown()` | Lifecycle and cleanup |

`ProfileSession`:

| Method | Purpose |
|---|---|
| `read_profile()`, `refresh_activities()` | Read the profile and its activity history |
| `send_email(...)`, `send_sms(...) → SendReceipt` | Send. A receipt is not proof of delivery |
| `open_tasks()`, `create_task(...)` (raises `DuplicateTaskWarning`), `complete_task(id, note)`, `task_status(id)` | Task handling |
| `add_note(text)` | Internal note |
| `create_sales_appointment(...)`, `set_progress(...)`, `progress()`, `print_profile_page1()` | Appointment workflow |
| `close()` | Close the tab |

Two implementations:
- `MockEleadsAdapter` is in memory and supports fault injection: dropped send activity, missing dashboard rows. It's used by `titan demo` and the tests.
- `PlaywrightEleadsAdapter` drives the live UI using `selectors.eleads.toml`. An action with a blank selector raises `NotCalibrated` instead of guessing.

> **Assumption / prerequisite:** I don't know of a documented public Eleads API for these actions (CDK's Fortellis platform may offer some through a dealer agreement). Before switching `crm.adapter` to `playwright`, the dealership must confirm that browser automation of the Eleads account is allowed under its CDK agreement and IT policy. The selector map must then be calibrated in a supervised session.

---

## 10. Memory-management and process-cleanup strategy

- **Tab rule:** a profile is open only inside `with sessions.profile(...)`. Reading happens inside the block; policy checks and composition happen after it closes wherever possible. At most `max_open_profiles` tabs (2) are open across the whole process. Tests check this, and check that no tabs are left open after every cycle.
- **Browser:** headless Chromium with GPU, extensions, and background networking disabled, a renderer-process limit of 3, and a V8 heap cap of 256 MB. Images, media, and fonts are blocked through routing.
- **No profile cache:** `CustomerProfile` objects are local to a single function. Only text digests are stored.
- **RAM monitor:** `psutil` adds up the RSS of the agent and all child processes (Chromium). Soft limit (600 MB): run cleanup hooks and `gc.collect()`. Hard limit (900 MB): stop opening tabs and recycle the browser.
- **Database:** a single SQLite connection per process in WAL mode with `synchronous=NORMAL`, short transactions, and no ORM.
- **Shutdown:** `Orchestrator.stop()` signals both workers, which call `adapter.shutdown()` to close pages, context, browser, and Playwright.

---

## 11. Configuration, deployment, and external assumptions

**Configuration:** `%APPDATA%\Titan\config.toml` (see `config.example.toml`). Main keys:

| Key | Default |
|---|---|
| `identity.allowed_rooftops` | 28206, 28546 |
| `identity.blocked_rooftops` | 6220 |
| `identity.storewide_authorized` | false |
| `time.*` | Windows as described in section 5 |
| `approval.mode` | `every_send` (or `batch`) |
| `sla.*` | 60 s draft / 240 s total |
| `cadence.*` | 14 months, 48 h, 12 h, 72 h, 5 min, 4 working days, 24 h, 30 days |
| `messaging.*` | 120 words / 320 characters |
| `resources.*` | 2 tabs, 600/900 MB, 20 s poll, batch of 25 |
| `crm.adapter` | `mock` or `playwright` |
| `crm.selectors_file` | `selectors.eleads.toml` |
| `crm.write_strategy_notes` | true |
| `crm.credential_service` | `titan-eleads` |

**Secrets:** the Eleads username and password are stored in Windows Credential Manager through `keyring` (`titan credentials set`). They are never written to config or logs.

**Install (Windows 10/11):** run `scripts\install.ps1`. It:
1. creates a venv,
2. installs `titan-crm-agent[browser]`,
3. installs Playwright Chromium,
4. copies the example config,
5. runs `titan startup install`, which creates a Task Scheduler entry that starts at logon with a 1-minute delay under `pythonw` with no console window.

**Operating commands:**

```
titan demo --approve          # dry run on fictional data
titan run                     # continuous primary agent
titan approvals               # review exact drafts
titan approve <id>            # approve one exact message
titan edit <id> --body-file reply.txt
titan reject <id> --reason "…"
titan appointment propose --rooftop 28206 --customer C1 --opportunity O1 --name "Dana" --at 2026-10-17T11:00 --purpose "complimentary appraisal" --notes "…"
titan appointment approve --id <id>
titan report [--date YYYY-MM-DD]
```

**Capability boundary (PDF page 7):** monitoring stops when the PC sleeps, the user logs out, or Eleads expires the session. Titan resumes with a fresh login, rooftop verification, and a live history check (`reconcile_in_flight`) before doing any other work.

---

## 12. Verification

`pytest` (55 tests) covers:
- the send windows, including Saturday and Sunday and SMS quiet hours,
- the rooftop and identity guard,
- every eligibility rule: 72 h at the exact timestamp, 14 months, recent-inquiry override, funding note, permissions, 48 h cycle,
- draft lint for every opportunity type, and that promises get rejected,
- the approval hash and both approval modes,
- send-once, verification, and separate email/SMS completion,
- after-hours queuing and release at 9:00,
- blocking on STOP, replies, and edits,
- that uncertain sends are never retried,
- reuse of existing tasks instead of duplicates,
- crash recovery, the appointment workflow, the tab cap and closure, and dashboard-mismatch reporting.

## 13. Open items for management (PDF page 8)

1. Approval mode: every send vs. batch.
2. Own-customer spacing (12 h currently).
3. Follow-up cadence by opportunity type.
4. Whether store-wide (other-consultant) work is authorized.
5. Scorecard targets.
6. CDK/IT sign-off on browser automation and selector calibration.
7. Whether an approved telephony integration exists for calls.
