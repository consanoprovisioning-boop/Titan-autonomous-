---
name: titan-cos-multiagent-cluster
description: Run Titan Chief of Staff on the Valdosta cluster (4-PC go-live plus ready 8-PC scale-out). Delegate specialist jobs by rooftop and lane, comb the database after P1, and push toward 60 actions/hour without double-sends. Never TKO 6220.
---

# Titan COS multi-agent cluster (4 live, 8 ready)

Use this skill when Glenn asks to run Titan COS on more than one PC, clone Desktop3, add more PCs, comb the database, or raise throughput toward 60 actions/hour.

Long law: `docs/ops/eleads-valdosta.md`. Host runbooks: `docs/deploy/cluster-4pc.md`, `docs/deploy/cluster-8pc.md`, `docs/deploy/desktop3.md`, `docs/deploy/node-onboarding.md`, `docs/deploy/alienware-18.md`. Governance: `docs/ops/sources/TITAN-CHANGE-MULTIAGENT-001-v0.5.1.md` (8-PC overlay), `docs/ops/sources/TITAN-CHANGE-MULTIAGENT-001-v0.5.0.md` (4-PC), `docs/ops/sources/TITAN-CHANGE-MULTIAGENT-001-v0.4.0.md` (51-role catalog). Continual loop: `.cursor/skills/running-eleads-cos-loop/SKILL.md`.

Ban the words pilot, MVP, and prototype for the Valdosta loop.

## Authority

Version 1.1 after a **Glenn-verified Eleads session on that PC** (CHECKIN.cmd, Verify, Chrome CDP 9222). Exact-draft is not a hard stop then.

Still never skipped: consent / DNC / DNT / Stop; rooftops **28206** and **28546** only; identity **Bordine, Glenn**; 72h / 96h holds; customer day and time before Appointment Set; 180-day ownership screen; no invented facts; CRM history is proof.

This cloud Linux agent must not open `eleadcrm.com`, start `Titan.ChiefOfStaff.exe`, type or store the password, or fabricate sends. If workers are offline: `sends=[]`.

## Inventory first

Call cursor-cloud `list-self-hosted-workers` with `scope=all` and `status=all`. Write `/cursor/stores/self/worker-snapshot.json`. Then:

```bash
scripts/launch-eleads-cos-loop.sh --workers-file /cursor/stores/self/worker-snapshot.json
```

Label each node online or missing from that snapshot. Do not invent Desktop3 or Node3–Node8. A worker whose directory is `C:\Windows\system32` is online and must not receive a placed agent.

Known Alienware ids (historical): wrong-directory `1a1f1f15-72c4-4334-ad8b-f87d54efe73a`; verified `%USERPROFILE%` `e630caf9-875d-4439-ab1a-3e6c386f6c0f`. Treat them as offline when the live list is empty.

## One Core, seven specialists, four or eight PCs

Do not spawn 51 agents. Core persists and sends. Specialists return typed results.

| Specialist | Cluster job |
| --- | --- |
| queue/prioritization | P1 Daily Organizer, overdue rank |
| profile inspection | P2 database comb, full history |
| drafting | P3 tailored upgrade + organizer copy |
| compliance | DNC, consent, 180-day, 72h/96h, SMS window |
| appointment | Appointment-commit fast path |
| CRM proof | History under Bordine, Glenn |
| audit | Preflight / QA / postflight |

## 12-step cycle → Eleads tick

1. Core opens one idempotent tick.
2. Audit preflight. Fail closed if Verify or rooftop is invalid.
3. Core confirms the 51-role catalog. Deploy at most seven specialists.
4. Queue builds Open/All work in order 0–5. Stop at the first unfinished rank.
5. Profile inspection uses fresh rooftop facts only.
6. Drafting writes eligible copy or blocked.
7. Appointment specialist books only with day and time in correspondence.
8. Audit QA.
9. Compliance.
10. Core rechecks and commits Draft, Blocked, or a V1.1 send.
11. After Verify, eligible sends do not pause for exact-draft. Connector writes stay Draft or Blocked.
12. Core writes the tick report. CRM history is the only send count.

## Partition (do not double-send)

Live **4-PC** (run today):

| Node | Rooftop | Lane | Do this |
| --- | --- | --- | --- |
| Alienware-18 | 28206 | P1 bundled | Overdue emails, reschedule overdue calls, organizer remainder, SMS 08:00–21:00 ET |
| Desktop3 | 28546 | P1 bundled | Same P1 jobs on Mitsubishi |
| Node3 | 28206 | P2/P3 | Comb DB, then tailored upgrades, only after Nissan P1 is clear |
| Node4 | 28546 | P2/P3 | Comb DB, then tailored upgrades, only after Mitsubishi P1 is clear |

Ready **8-PC** after `SCALE-OUT-8.cmd` (PC1/PC2 narrow to email):

| Node | Rooftop | Lane |
| --- | --- | --- |
| Alienware-18 | 28206 | P1 email |
| Desktop3 | 28546 | P1 email |
| Node3 | 28206 | P2/P3 |
| Node4 | 28546 | P2/P3 |
| Node5 | 28206 | P1 calls |
| Node6 | 28546 | P1 calls |
| Node7 | both allowed | SMS in window |
| Node8 | both allowed | Appointment-commit |

Lease: `CustomerLease` (15 min), fingerprint rooftop + customer, exclusive across all eight. Shared folder `TITAN_LEASE_DIR` is required at eight PCs. Enroll with `deploy/cluster-node/ENROLL-NODE.cmd`. Worker start must `cd /d %USERPROFILE%`.

## High-velocity job (60/hour needle)

P1 overdue email completion + overdue call reschedule. At eight PCs those split across four machines. Emails need no approval after Verify. Do not start P2/P3 on a rooftop until that rooftop’s P1 is clear. Do not claim 60/hour while workers are offline.

## Desktop3

Cloud does not clone onto Desktop3. Glenn follows `docs/deploy/desktop3.md`.
