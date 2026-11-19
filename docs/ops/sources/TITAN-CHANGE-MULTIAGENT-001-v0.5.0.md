# TITAN-CHANGE-MULTIAGENT-001 v0.5.0

Filed overlay for the 4-PC Valdosta cluster. Document ID stays **TITAN-CHANGE-MULTIAGENT-001**. This file does not rewrite v0.4.0 (`TITAN-CHANGE-MULTIAGENT-001-v0.4.0.md`) or the Eleads source packet (`Eleads-operating-instructions.md`, `Eleads_project_Instructions_Goal.txt`, or the PDF).

The word “pilot” in v0.4.0 remains historical Titan SDK scope (connector output Draft or Blocked). It is not a label for the live Valdosta Eleads loop. Version 1.1 Eleads sends after a Glenn-verified local session are governed by `docs/ops/eleads-valdosta.md`.

---

CONTROLLED CHANGE RECORD
Titan Autonomous + Titan Chief of Staff — 4-PC local cluster
Document ID: TITAN-CHANGE-MULTIAGENT-001
Version: 0.5.0
Effective date: November 2, 2026
Product owner: Glenn Bordine / Bordine Systems — Project Titan
Source authority: Glenn request to clone Titan Autonomous and Titan COS onto Desktop3, live COS on all 4 PCs, delegate specialist jobs, comb the database, and reach or surpass the 60 actions/hour floor
Status: Architecture, host types, playbooks, and coordinator lock filed. Physical clone and Eleads Verify remain Glenn-local. Cloud Linux never opens Eleads.

## 1. Outcome

Titan COS lives on each of the four Windows PCs. One Core overseer maps the v0.4.0 twelve-step cycle onto Eleads ticks and a small specialist set. Work is partitioned by rooftop and queue lane so two nodes never send to the same customer.

Cloud writes the playbook and code. Glenn, or a local worker already running on that PC, performs the clone. This record does not claim Desktop3, Node3, or Node4 are online.

## 2. Hard constraints (unchanged)

- Rooftops **28206** and **28546** only. Never TKO Autogroup **6220**.
- Identity **Bordine, Glenn** on every CRM action.
- Live SMS window **08:00–21:00** America/New_York.
- Ownership screen **180 days**. Recent inquiry overrides. Model year never proves duration.
- Cloud Linux agent must never open `eleadcrm.com`, run `Titan.ChiefOfStaff.exe`, type or store the Eleads password, or fabricate CRM sends.
- Self-hosted workers start from `%USERPROFILE%` (example `C:\Users\Glenn R Bordine`). Never `C:\Windows\system32`.
- CDP / Eleads Chrome attach is port **9222**, loopback only.
- Complete overdue emails and reschedule overdue calls **first**. Database comb and upgrade offers wait.
- Emails need no approval after a Glenn-verified local worker + Eleads session. SMS stays inside the window.
- If host/workers are offline: `sends=[]` and record that honestly.

## 3. Twelve-step cycle mapped to Eleads ticks

| Step | v0.4.0 capability | Eleads tick / specialist | Cluster node |
| --- | --- | --- | --- |
| 1 | Titan Core | Open one idempotent tick. Core persists state. | Cloud Core + every live node |
| 2 | Audit | Preflight: rooftop, identity, kill.switch, Verify, CDP 9222. Fail closed. | audit |
| 3 | Capability registry | Confirm the 51-role catalog. Deploy at most seven specialists. Never spawn 51. | Core |
| 4 | Follow-up coordination | Daily Organizer Open/All. P1 overdue email + call reschedule first. | queue on PC1 / PC2 |
| 5 | Dealership knowledge | Full-profile facts on this rooftop only. Comb DB is P2, never a list-view send. | profile on PC3 / PC4 after P1 |
| 6 | Response drafting | Channel copy from verified facts. Tailored upgrade is P3. | drafting |
| 7 | Appointment lifecycle | Appointment-commit fast path preempts the queue when day and time are in correspondence. | appointment on the rooftop owner |
| 8 | Quality assurance | Risky claims, completeness, channel fit, VOI. | audit |
| 9 | Compliance | DNC, consent, quiet hours, 180-day screen, 72h/96h. | compliance |
| 10 | Titan Core | Recheck. Commit Draft, Blocked, or a V1.1 send. | Core only |
| 11 | Approval routing | After Verify, eligible email/SMS do not pause for exact-draft. Titan connector writes stay Draft or Blocked. | Core |
| 12 | Reporting + audit | Tick report. CRM history is the only send count. | Core persists; audit checks |

Typed specialist result is unchanged from the Cursor COS loop skill. Specialists do not write `eleads-valdosta-state.json`, do not write `TICKLOG.md`, and do not send.

## 4. Four-PC partition

| PC | Node id | Rooftop | Lane | Jobs |
| --- | --- | --- | --- | --- |
| PC1 | Alienware-18 | 28206 Valdosta Nissan | P1 organizer | Overdue email clearance, overdue call reschedule, Daily Organizer remainder, SMS in window, appointment-commit fast path |
| PC2 | Desktop3 | 28546 Valdosta Mitsubishi | P1 organizer | Same high-velocity P1 jobs on the other rooftop |
| PC3 | Node3 (unnamed until enrolled) | 28206 Valdosta Nissan | P2/P3 | Comb database, then tailored upgrade emails. Closed until Nissan P1 is clear |
| PC4 | Node4 (unnamed until enrolled) | 28546 Valdosta Mitsubishi | P2/P3 | Comb database, then tailored upgrade emails. Closed until Mitsubishi P1 is clear |

Coordinator fences:

1. Rooftop + lane assignment (`ClusterPartition`).
2. Customer lease (`CustomerLease`, 15-minute TTL). Optional shared folder via `TITAN_LEASE_DIR`.
3. CRM history under Bordine, Glenn before any send is counted.

A node that is not enrolled, or whose session is not Glenn-verified, works nothing that sends.

## 5. Jobs that move the 60/hour floor

High-velocity (legal, P1, emails need no approval after Verify):

- Complete overdue Daily Organizer emails.
- Reschedule overdue calls. Never mark a call complete if no call occurred.

Secondary (only after that rooftop’s P1 is clear):

- Comb the customer database (P2 study: full profile, no send).
- Tailored upgrade emails (P3, profile-by-profile, never a generic blast).

Appointment-commit fast path preempts both lanes when a customer day and time are already in correspondence.

Sixty actions per hour is a floor after live nodes exist. It is not claimed while workers are offline.

## 6. Desktop3 clone (Glenn-local)

Cloud does not copy files onto Desktop3. Glenn runs `docs/deploy/desktop3.md` and `deploy/desktop3/START-HERE.txt` on that PC: clone the repo, publish or copy `dist\titan-chief-win-x64`, `INSTALL.cmd`, enroll as Desktop3, `START-WORKER.cmd` from `%USERPROFILE%`, CHECKIN / Verify, leave Chrome on CDP 9222.

## 7. Authority relative to v0.4.0

- 51 roles remain a logical catalog. Do not spawn 51 agents.
- Titan SDK connector output stays Draft or Blocked.
- V1.1 Eleads overlay after Verify is unchanged.
- This overlay adds cluster placement, per-rooftop P1→P2→P3 gates, customer leases, and the Desktop3 / node-onboarding playbooks.
