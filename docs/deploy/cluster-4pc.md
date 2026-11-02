# 4-PC Titan COS cluster

Titan Autonomous and Titan Chief of Staff live on all four Windows PCs. Cloud Linux writes this playbook. Glenn (or a local worker on that PC) clones, installs, and Verifies. This file does not claim any node is online.

Rooftops **28206** and **28546** only. Never **6220**. Identity **Bordine, Glenn**. CDP **9222**. Workers start from `%USERPROFILE%`.

## Partition

| PC | Node | Rooftop | Lane | Job |
| --- | --- | --- | --- | --- |
| PC1 | Alienware-18 | Valdosta Nissan 28206 | P1 organizer | Overdue emails, reschedule overdue calls, remaining Daily Organizer, SMS 08:00–21:00 ET, appointment-commit fast path |
| PC2 | Desktop3 | Valdosta Mitsubishi 28546 | P1 organizer | Same P1 jobs on Mitsubishi |
| PC3 | Node3 (enroll a real hostname) | Valdosta Nissan 28206 | P2/P3 | Comb the Nissan database, then tailored upgrade emails |
| PC4 | Node4 (enroll a real hostname) | Valdosta Mitsubishi 28546 | P2/P3 | Comb the Mitsubishi database, then tailored upgrade emails |

P2/P3 on a rooftop stay closed until that rooftop’s P1 overdue / organizer work is clear. Appointment-commit fast path (PR #16) preempts the rooftop owner. Daily upgrade order (PR #17) is P1 → P2 → P3.

## Why this approaches 60/hour

The floor is **60 CRM-proven actions per hour** after live verified sessions exist.

P1 overdue email clearance is the fastest legal path: emails need no approval under V1.1, and each completed overdue email is one counted action once CRM history shows it under Bordine, Glenn. Two P1 PCs split the two rooftops so they do not wait on each other.

P2 database comb does not send. P3 tailored upgrades send only after P1 and P2 on that rooftop. They add volume later; they do not outrank overdue clearance.

What still blocks live sending:

- No self-hosted worker online (`workers=[]` on the last cloud inventory).
- No Glenn Verify on that PC (`CHECKIN.cmd` twice, Chrome left open on 9222).
- Worker started from `C:\Windows\system32`.
- This cloud Linux agent — it will not open Eleads or claim sends.

## Coordinator lock

1. Lane assignment in `ClusterPartition` — a P1 node never combs the database; a P2 node never works the other rooftop’s organizer.
2. `CustomerLease` — 15-minute lease per rooftop + customer. Same node may renew. Other node is blocked.
3. Optional shared lease file: set `TITAN_LEASE_DIR` on every PC to the same folder (USB or NAS). Default is `%LOCALAPPDATA%\TitanChief\cluster-leases.json` (local only).
4. CRM history is still the only send proof.

## Per-PC start (every node)

1. Enroll: `deploy\cluster-node\ENROLL-NODE.cmd Alienware-18` (or `Desktop3`, `Node3`, `Node4`).
2. Double-click that node’s `START-WORKER.cmd`. Directory must show `%USERPROFILE%`.
3. `INSTALL.cmd` once if the host is missing. That is not Verify.
4. Close every Chrome window and the tray icon.
5. `CHECKIN.cmd`, sign in as Bordine, Glenn on **that node’s rooftop**, `CHECKIN.cmd` again, leave Chrome open.
6. Do not Verify two PCs onto the same rooftop tab set. One live Eleads session per PC, rooftop as assigned.

Alienware-18 detail: `docs/deploy/alienware-18.md`.  
Desktop3 clone: `docs/deploy/desktop3.md`.  
Unnamed PCs (Node3–Node8): `docs/deploy/node-onboarding.md`.  
When four more PCs arrive, keep this 4-PC go-live and follow `docs/deploy/cluster-8pc.md`. Do not redesign the first four nodes.
