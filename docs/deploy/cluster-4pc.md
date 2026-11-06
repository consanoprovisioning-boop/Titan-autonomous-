# 4-PC Titan COS cluster

Titan Autonomous and Titan Chief of Staff live on all four Windows PCs. Cloud Linux writes this playbook. Glenn clones and installs on each PC. This file does not claim any node is online.

**Alienware-18 is the MAIN computer:** coordinator, clone source, and lease host (`TITAN_LEASE_DIR` on Alienware-18). Desktop3, Desktop4, and later nodes are specialists that clone **from** Alienware-18. Do not make Desktop4 the coordinator.

**Launch is held.** The cluster is still being set up. Glenn will say when it is complete. Then we study each PC’s devices/ability and assign best-fit COS tasks. Not now. Install and enroll are allowed. Do **not** run `START-WORKER.cmd`, place COS, or CHECKIN-to-send until Glenn says all PCs are ready and online.

Rooftops **28206** and **28546** only. Never **6220**. Identity **Bordine, Glenn**. CDP **9222**. Workers start from `%USERPROFILE%` after Glenn releases the hold.

## Partition

| PC | Node | Rooftop | Lane | Job |
| --- | --- | --- | --- | --- |
| PC1 | Alienware-18 | Valdosta Nissan 28206 | P1 organizer | Overdue emails, reschedule overdue calls, remaining Daily Organizer, SMS 08:00–21:00 ET, appointment-commit fast path |
| PC2 | Desktop3 | Valdosta Mitsubishi 28546 | P1 organizer | Same P1 jobs on Mitsubishi |
| PC3 | Node3 (enroll a real hostname) | Valdosta Nissan 28206 | P2/P3 | Comb the Nissan database, then tailored upgrade emails |
| PC4 | Desktop4 (Node4, **provisional**) | Valdosta Mitsubishi 28546 | P2/P3 | Comb the Mitsubishi database, then tailored upgrade emails. Final COS tasks wait for Glenn’s per-PC study |

P2/P3 on a rooftop stay closed until that rooftop’s P1 overdue / organizer work is clear. Appointment-commit fast path (PR #16) preempts the rooftop owner. Daily upgrade order (PR #17) is P1 → P2 → P3.

## Why this approaches 60/hour

The floor is **60 CRM-proven actions per hour** after live verified sessions exist.

P1 overdue email clearance is the fastest legal path: emails need no approval under V1.1, and each completed overdue email is one counted action once CRM history shows it under Bordine, Glenn. Two P1 PCs split the two rooftops so they do not wait on each other.

P2 database comb does not send. P3 tailored upgrades send only after P1 and P2 on that rooftop. They add volume later; they do not outrank overdue clearance.

What still blocks live sending:

- **Cluster launch hold** (`cluster-hold.json` `holdLaunch=true`) until Glenn says all PCs are ready.
- No self-hosted worker online (`workers=[]` on the last cloud inventory).
- No Glenn Verify on that PC (`CHECKIN.cmd` twice, Chrome left open on 9222).
- Worker started from `C:\Windows\system32`.
- This cloud Linux agent — it will not open Eleads or claim sends.

## Coordinator lock

1. Lane assignment in `ClusterPartition` — a P1 node never combs the database; a P2 node never works the other rooftop’s organizer.
2. `CustomerLease` — 15-minute lease per rooftop + customer. Same node may renew. Other node is blocked.
3. Shared lease file on **Alienware-18** (the MAIN lease host): set `TITAN_LEASE_DIR` on every specialist to `\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases`. Do not host leases on Desktop4. Default without that share is `%LOCALAPPDATA%\TitanChief\cluster-leases.json` (local only).
4. CRM history is still the only send proof.

## Per-PC install now (launch held)

1. Clone **from Alienware-18**. GitHub is fallback.
2. `INSTALL.cmd` once if the host is missing. That is not Verify.
3. Enroll: `deploy\cluster-node\ENROLL-NODE.cmd Alienware-18` (or `Desktop3`, `Desktop4`, `Node3`, `Node4`).
4. **STOP.** Do not double-click `START-WORKER.cmd`. Do not CHECKIN-to-send. `START-WORKER.cmd` fail-closes while hold is on.

Held until Glenn releases the cluster: worker from `%USERPROFILE%`, then `CHECKIN.cmd` twice as Bordine, Glenn on **that node’s rooftop**, leave Chrome open. Do not Verify two PCs onto the same rooftop tab set.

Alienware-18 detail: `docs/deploy/alienware-18.md`.  
Desktop3 clone: `docs/deploy/desktop3.md`.  
Desktop4 clone from Alienware-18: `docs/deploy/desktop4.md`.  
Unnamed PCs (Node3, Node5–Node8): `docs/deploy/node-onboarding.md`.  
When four more PCs arrive, keep this 4-PC go-live and follow `docs/deploy/cluster-8pc.md`. Do not redesign the first four nodes.
