# TITAN-CHANGE-MULTIAGENT-001 v0.5.1

Filed overlay for the **8-PC scale-out**. Document ID stays **TITAN-CHANGE-MULTIAGENT-001**. This file does not rewrite v0.5.0 (`TITAN-CHANGE-MULTIAGENT-001-v0.5.0.md`), v0.4.0, or the Eleads source packet.

---

CONTROLLED CHANGE RECORD
Titan Autonomous + Titan Chief of Staff — 8-PC local scale-out
Document ID: TITAN-CHANGE-MULTIAGENT-001
Version: 0.5.1
Effective date: November 2, 2026
Product owner: Glenn Bordine / Bordine Systems — Project Titan
Source authority: Glenn addendum that four more PCs will arrive (eight total). Keep the 4-PC go-live. Extend overflow lanes toward 60+ actions/hour.
Status: 4-PC go-live retained. 8-PC catalog, enroll, shared lease, and playbook filed. Physical clone and Verify remain Glenn-local. Cloud Linux never opens Eleads.

## 1. Outcome

The live four nodes stay: Alienware-18 Nissan P1, Desktop3 Mitsubishi P1, Node3 Nissan P2/P3, Node4 Mitsubishi P2/P3.

Nodes 5–8 use the same onboarding template (clone, `%USERPROFILE%` worker, CDP 9222, enroll). When scale-out is armed (`SCALE-OUT-8.cmd` or `TITAN_CLUSTER_SIZE=8`), PC1/PC2 narrow to P1 email. Overflow lanes are P1 Nissan calls, P1 Mitsubishi calls, SMS-in-window, appointment-commit.

## 2. Hard constraints (unchanged from v0.5.0)

- Rooftops **28206** and **28546** only. Never **6220**.
- Identity **Bordine, Glenn**.
- SMS **08:00–21:00** America/New_York.
- Ownership screen **180 days**.
- Cloud Linux never opens Eleads, never runs `Titan.ChiefOfStaff.exe`, never types the password, never fabricates sends.
- Workers start from `%USERPROFILE%`. Never `C:\Windows\system32`.
- P1 overdue email + call reschedule before P2/P3.
- Offline workers: `sends=[]`.

## 3. Eight-PC lanes

| Node | Lane | Rooftop |
| --- | --- | --- |
| Alienware-18 | P1 email (narrowed from bundled P1) | 28206 |
| Desktop3 | P1 email | 28546 |
| Node3 | P2/P3 | 28206 |
| Node4 | P2/P3 | 28546 |
| Node5 | P1 calls | 28206 |
| Node6 | P1 calls | 28546 |
| Node7 | SMS in window | 28206 and 28546 |
| Node8 | Appointment-commit | 28206 and 28546 |

Until scale-out is armed, v0.5.0 bundled P1 on PC1/PC2 remains the live law.

## 4. Lease at eight nodes

`CustomerLease` fingerprint stays **rooftop + customer**, not job. All eight nodes share one lock. Optional in v0.5.0; **required** at eight PCs via `TITAN_LEASE_DIR`. Appointment-commit preempts because it holds the same customer key.

## 5. Throughput

Eight verified local COS processes in exclusive lanes can clear overdue email and call work on both rooftops at once, plus in-window SMS and appointment-commit, which is the legal path to or past 60 CRM-proven actions/hour. P2/P3 stay gated. This record does not claim 60/hour while workers are offline.
