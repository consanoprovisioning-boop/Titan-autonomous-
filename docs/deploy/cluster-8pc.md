# 8-PC Titan COS scale-out

The live **4-PC go-live stays**. Alienware-18, Desktop3, Node3, and Node4 keep their jobs until Glenn turns scale-out on. This file is the ready map for the next four boxes (Nodes 5–8). Same clone, `%USERPROFILE%` worker, CDP **9222**, and enroll path — not a redesign.

Cloud Linux writes this playbook. Glenn clones and Verifies on each PC. This file does not claim any node is online.

Rooftops **28206** and **28546** only. Never **6220**. Identity **Bordine, Glenn**. SMS **08:00–21:00** ET. Ownership screen **180 days**. P1 before P2/P3.

4-PC runbook (run this today): `docs/deploy/cluster-4pc.md`.

## Live 4-PC (unchanged)

| PC | Node | Rooftop | Lane |
| --- | --- | --- | --- |
| PC1 | Alienware-18 | 28206 | Bundled P1 organizer (email + calls + SMS + appointments) |
| PC2 | Desktop3 | 28546 | Bundled P1 organizer |
| PC3 | Node3 | 28206 | P2/P3 comb + tailored upgrades after Nissan P1 |
| PC4 | Node4 | 28546 | P2/P3 after Mitsubishi P1 |

## 8-PC lane map (when Nodes 5–8 exist)

Turn scale-out on with `deploy\cluster-node\SCALE-OUT-8.cmd` on every PC (and set `TITAN_LEASE_DIR` to the same shared folder). PC1/PC2 **narrow** to email so they do not keep doing calls/SMS/appointments.

| PC | Node | Rooftop | Lane | Job |
| --- | --- | --- | --- | --- |
| PC1 | Alienware-18 | 28206 | P1 email | Overdue / organizer **emails** only |
| PC2 | Desktop3 | 28546 | P1 email | Overdue / organizer **emails** only |
| PC3 | Node3 | 28206 | P2/P3 | Comb Nissan DB, then tailored upgrades |
| PC4 | Node4 | 28546 | P2/P3 | Comb Mitsubishi DB, then tailored upgrades |
| PC5 | Node5 | 28206 | P1 calls | Reschedule overdue Nissan **calls**. Never mark a call complete if no call occurred |
| PC6 | Node6 | 28546 | P1 calls | Reschedule overdue Mitsubishi **calls** |
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

On **each new PC** (same as Desktop3, but enroll Node5–Node8):

```bat
cd /d "%USERPROFILE%"
git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
cd Titan-autonomous-
git checkout cursor/eleads-cluster-4pc-7dfe
scripts\publish-win-x64.cmd
dist\titan-chief-win-x64\INSTALL.cmd
deploy\cluster-node\ENROLL-NODE.cmd Node5
deploy\cluster-node\START-WORKER.cmd
```

Replace `Node5` with `Node6`, `Node7`, or `Node8` on the matching box. Worker directory must be `%USERPROFILE%`, never `C:\Windows\system32`. Then CHECKIN twice as Bordine, Glenn on that node’s rooftop (Node7/Node8 may use either allowed rooftop; never 6220). Leave Chrome on 9222.

On **every** PC, including the original four:

```bat
set TITAN_LEASE_DIR=\\NAS\Titan\leases
deploy\cluster-node\SCALE-OUT-8.cmd
```

That writes `cluster-mode.json` (`scaleOut=true`, `size=8`) locally and into `TITAN_LEASE_DIR` if set. Alienware-18 and Desktop3 then drop calls/SMS/appointments so Node5–Node8 can take those lanes.

Cloud does not clone onto the new PCs and does not open Eleads.

## Still blocked for live sending

- `list-self-hosted-workers` empty (`workers=[]`) — do not invent Node5–Node8 as online.
- No Glenn Verify on that PC.
- Worker started from `C:\Windows\system32`.
- This cloud Linux agent.
