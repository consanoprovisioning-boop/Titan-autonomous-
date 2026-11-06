# 8-PC Titan COS scale-out

The live **4-PC go-live stays**. Alienware-18, Desktop3, Node3, and Desktop4 (Node4, provisional) keep their jobs until Glenn turns scale-out on. This file is the ready map for the next four boxes. Desktop5 (Node5) and Desktop6 (Node6) are **named** overflow PCs on WiFi; Node7 and Node8 stay unnamed until enrolled. Same clone, `%USERPROFILE%` worker, CDP **9222**, and enroll path — not a redesign.

**Alienware-18 is MAIN** (coordinator, clone source, lease host). Specialists clone from Alienware-18. Do not make Desktop4, Desktop5, or Desktop6 the coordinator. WiFi on the LAN is not a Cursor worker.

**Per-node hold.** Desktop4 is released to join. Desktop5 and Desktop6 stay clone/install/enroll only. Do not `START-WORKER` on unreleased overflow nodes. Do not `SCALE-OUT-8.cmd` yet.

Cloud Linux writes this playbook. Glenn clones and installs on each PC. This file does not claim any node is online.

Rooftops **28206** and **28546** only. Never **6220**. Identity **Bordine, Glenn**. SMS **08:00–21:00** ET. Ownership screen **180 days**. P1 before P2/P3.

4-PC runbook (run this today): `docs/deploy/cluster-4pc.md`.

## Live 4-PC (unchanged)

| PC | Node | Rooftop | Lane |
| --- | --- | --- | --- |
| PC1 | Alienware-18 | 28206 | Bundled P1 organizer (email + calls + SMS + appointments) |
| PC2 | Desktop3 | 28546 | Bundled P1 organizer |
| PC3 | Node3 | 28206 | P2/P3 comb + tailored upgrades after Nissan P1 |
| PC4 | Desktop4 (Node4, provisional) | 28546 | P2/P3 after Mitsubishi P1 |

## 8-PC lane map (when Nodes 5–8 exist)

Turn scale-out on with `deploy\cluster-node\SCALE-OUT-8.cmd` on every PC (and set `TITAN_LEASE_DIR` to the same shared folder). PC1/PC2 **narrow** to email so they do not keep doing calls/SMS/appointments.

| PC | Node | Rooftop | Lane | Job |
| --- | --- | --- | --- | --- |
| PC1 | Alienware-18 | 28206 | P1 email | Overdue / organizer **emails** only |
| PC2 | Desktop3 | 28546 | P1 email | Overdue / organizer **emails** only |
| PC3 | Node3 | 28206 | P2/P3 | Comb Nissan DB, then tailored upgrades |
| PC4 | Desktop4 (Node4, provisional) | 28546 | P2/P3 | Comb Mitsubishi DB, then tailored upgrades |
| PC5 | Desktop5 (Node5, **provisional**) | 28206 | P1 calls | Reschedule overdue Nissan **calls**. Never mark a call complete if no call occurred. Final COS tasks wait for Glenn’s per-PC study |
| PC6 | Desktop6 (Node6, **provisional**) | 28546 | P1 calls | Reschedule overdue Mitsubishi **calls**. Final COS tasks wait for Glenn’s per-PC study |
| PC7 | Node7 | 28206 and 28546 | SMS in window | Eligible SMS 08:00–21:00 ET. One customer lease still blocks the other nodes |
| PC8 | Node8 | 28206 and 28546 | Appointment-commit | Fast path when day and time are already in correspondence. Lease preempts other lanes |

## How 8 local COS processes hit 60+/hour without colliding

Each PC runs its own Titan COS tick against its own Chrome CDP 9222 session after Glenn Verify. Core still deploys at most seven specialists per tick. Eight machines in parallel raise throughput because they do **different lanes**, not the same customer.

Needle-movers (legal, after Verify):

- Four P1 channel lanes: Nissan email, Mitsubishi email, Nissan calls, Mitsubishi calls. Emails need no approval. Two rooftops no longer share one bundled P1 box.
- SMS lane (Node7) only inside the window.
- Appointment-commit (Node8) is bursty but high value; it takes the customer lease so nobody else texts or emails that person mid-book.
- P2/P3 stay gated. They add volume after P1 is clear; they do not outrank overdue clearance.

Anti-double-send at 8 nodes:

1. Exclusive rooftop + lane (`ClusterPartition.ScaleOutNodes`). Email and call on the same rooftop are different nodes.
2. Exclusive **customer lease** (`CustomerLease`): fingerprint is rooftop + customer, not job. A second node of the eight cannot acquire the same person. 15-minute TTL. Same node may renew.
3. Shared lease file **required at 8 PCs**: set `TITAN_LEASE_DIR` on every box to one NAS/USB folder. Local-only lease files cannot see each other.
4. CRM history under Bordine, Glenn is still the only send count.

Do not spawn 51 agents on one PC. Do not Verify two PCs into TKO 6220.

## What Glenn does when the next 4 boxes arrive

On **Desktop5** and **Desktop6** (named overflow; install only, then STOP):

```bat
\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\deploy\desktop5\INSTALL-COS.cmd
\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\deploy\desktop6\INSTALL-COS.cmd
```

Those files refuse `START-WORKER`. Playbooks: `docs/deploy/desktop5.md`, `docs/deploy/desktop6.md`. Enroll `Desktop5`/`Node5` or `Desktop6`/`Node6`. Do **not** run `SCALE-OUT-8.cmd` yet.

On **each remaining unnamed PC** (clone from Alienware-18, enroll Node7 or Node8). **Install only while launch is held:**

```bat
cd /d "%USERPROFILE%"
git clone "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-" Titan-autonomous-
cd Titan-autonomous-
git checkout cursor/eleads-cluster-4pc-7dfe
dist\titan-chief-win-x64\INSTALL.cmd
deploy\cluster-node\ENROLL-NODE.cmd Node7
```

Do **not** run `START-WORKER.cmd` until Glenn says all PCs are ready. Replace `Node7` with `Node8` on the matching box.

On **every** PC, including the original four, lease host is Alienware-18 (not Desktop4, not a separate NAS unless Glenn moves it):

```bat
set TITAN_LEASE_DIR=\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases
deploy\cluster-node\SCALE-OUT-8.cmd
```

That writes `cluster-mode.json` (`scaleOut=true`, `size=8`) locally and into `TITAN_LEASE_DIR` if set. Alienware-18 and Desktop3 then drop calls/SMS/appointments so Desktop5, Desktop6, Node7, and Node8 can take those lanes. Do **not** run this while launch is held.

Cloud does not clone onto the new PCs and does not open Eleads.

## Still blocked for live sending

- Cluster launch hold until Glenn says all PCs are ready.
- `list-self-hosted-workers` empty (`workers=[]`) — do not invent Desktop5, Desktop6, or Node7–Node8 as online. WiFi connected is not online.
- No Glenn Verify on that PC.
- Worker started from `C:\Windows\system32`.
- This cloud Linux agent.
