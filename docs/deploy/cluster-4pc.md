# 4-PC Titan COS cluster

Titan Autonomous and Titan Chief of Staff live on all four Windows PCs. Cloud Linux writes this playbook. Glenn clones and installs on each PC. This file does not claim any node is online.

**Alienware-18 is the MAIN computer:** coordinator, clone source, and lease host (`TITAN_LEASE_DIR` on Alienware-18). Desktop3, Desktop4, Desktop5, Desktop6, and later nodes are specialists that clone **from** Alienware-18. Do not make Desktop4, Desktop5, or Desktop6 the coordinator.

**Per-node hold.** Glenn released **Desktop4** to finish setup and join. `holdLaunch` stays true (`clusterHoldLaunch` remains for unreleased nodes). Desktop5 and Desktop6 stay install-only. Do **not** run `START-WORKER.cmd` on unreleased nodes.

Rooftops **28206** and **28546** only. Never **6220**. Identity **Bordine, Glenn**. CDP **9222**. Desktop4 workers start from `%USERPROFILE%`.

## Partition

| PC | Node | Rooftop | Lane | Job |
| --- | --- | --- | --- | --- |
| PC1 | Alienware-18 | Valdosta Nissan 28206 | P1 organizer | Overdue emails, reschedule overdue calls, remaining Daily Organizer, SMS 08:00–21:00 ET, appointment-commit fast path |
| PC2 | Desktop3 | Valdosta Mitsubishi 28546 | P1 organizer | Same P1 jobs on Mitsubishi |
| PC3 | Node3 (enroll a real hostname) | Valdosta Nissan 28206 | P2/P3 | Comb the Nissan database, then tailored upgrade emails |
| PC4 | Desktop4 (Node4, **provisional**) | Valdosta Mitsubishi 28546 | P2/P3 | Comb the Mitsubishi database, then tailored upgrade emails. Final COS tasks wait for Glenn’s per-PC study |

Desktop5 (Node5) and Desktop6 (Node6) are **named 8-PC overflow** PCs on WiFi, not 4-PC go-live seats. Provisional: Desktop5 ≈ Nissan 28206 P1 calls; Desktop6 ≈ Mitsubishi 28546 P1 calls. See `docs/deploy/cluster-8pc.md`. WiFi ≠ Cursor worker.

P2/P3 on a rooftop stay closed until that rooftop’s P1 overdue / organizer work is clear. Appointment-commit fast path (PR #16) preempts the rooftop owner. Daily upgrade order (PR #17) is P1 → P2 → P3.

## Why this approaches 60/hour

The floor is **60 CRM-proven actions per hour** after live verified sessions exist.

P1 overdue email clearance is the fastest legal path: emails need no approval under V1.1, and each completed overdue email is one counted action once CRM history shows it under Bordine, Glenn. Two P1 PCs split the two rooftops so they do not wait on each other.

P2 database comb does not send. P3 tailored upgrades send only after P1 and P2 on that rooftop. They add volume later; they do not outrank overdue clearance.

What still blocks live sending:

- **Cluster launch hold** (`holdLaunch=true`) on every node except `releasedNodes` (Desktop4).
- No self-hosted worker online (`workers=[]` on the last cloud inventory). COS is not live until Glenn runs `START-WORKER.cmd` on Desktop4 from `%USERPROFILE%`.
- No Glenn Verify on that PC (`CHECKIN.cmd` twice, Chrome left open on 9222).
- Worker started from `C:\Windows\system32`.
- This cloud Linux agent — it will not open Eleads or claim sends.

## Coordinator lock

1. Lane assignment in `ClusterPartition` — a P1 node never combs the database; a P2 node never works the other rooftop’s organizer.
2. `CustomerLease` — 15-minute lease per rooftop + customer. Same node may renew. Other node is blocked.
3. Shared lease file on **Alienware-18** (the MAIN lease host): set `TITAN_LEASE_DIR` on every specialist to `\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases`. Do not host leases on Desktop4. Default without that share is `%LOCALAPPDATA%\TitanChief\cluster-leases.json` (local only).
4. CRM history is still the only send proof.

## Per-PC install now (launch held)

One-shots. Desktop4 may START-WORKER after install. Desktop5/Desktop6 refuse `START-WORKER`:

```bat
\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\deploy\desktop4\INSTALL-COS.cmd
\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\deploy\desktop5\INSTALL-COS.cmd
\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\deploy\desktop6\INSTALL-COS.cmd
```

Other PCs:

1. Clone **from Alienware-18**. GitHub is fallback.
2. `INSTALL.cmd` once if the host is missing. That is not Verify.
3. Enroll: `deploy\cluster-node\ENROLL-NODE.cmd Alienware-18` (or `Desktop3`, `Desktop4`, `Desktop5`, `Desktop6`, `Node3`–`Node8`).
4. Desktop4: double-click `START-WORKER.cmd` from `%USERPROFILE%`. Desktop5/Desktop6: **STOP** — install-only.

Desktop4 next: `CHECKIN.cmd` twice as Bordine, Glenn on **28546**, leave Chrome open on 9222. Do not Verify two PCs onto the same rooftop tab set.

Alienware-18 detail: `docs/deploy/alienware-18.md`.  
Desktop3 clone: `docs/deploy/desktop3.md`.  
Desktop4 clone from Alienware-18: `docs/deploy/desktop4.md`.  
Desktop5 clone from Alienware-18: `docs/deploy/desktop5.md`.  
Desktop6 clone from Alienware-18: `docs/deploy/desktop6.md`.  
Unnamed PCs (Node3, Node7, Node8): `docs/deploy/node-onboarding.md`.  
When remaining overflow PCs arrive, keep this 4-PC go-live and follow `docs/deploy/cluster-8pc.md`. Do not redesign the first four nodes.
