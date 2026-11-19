# TITAN-CHANGE-MULTIAGENT-001 v0.5.6

Filed overlay for Glenn’s per-node release of **Desktop6 (DeskTop6ACER)** so that PC can start Titan COS and take Mitsubishi 28546 P1 jobs now. Document ID stays **TITAN-CHANGE-MULTIAGENT-001**. This file does not rewrite v0.5.5, v0.5.4, v0.5.3, v0.5.2, v0.5.1, v0.5.0, v0.4.0, or the Eleads source packet.

---

CONTROLLED CHANGE RECORD
Titan Autonomous + Titan Chief of Staff — Desktop6 per-node launch release and Mitsubishi P1 covering jobs
Document ID: TITAN-CHANGE-MULTIAGENT-001
Version: 0.5.6
Effective date: November 19, 2026
Product owner: Glenn Bordine / Bordine Systems — Project Titan
Source authority: Glenn, on DeskTop6ACER (“this PC”): “logged in start Titan COS on this PC put into cluster and give jobs.” Overrides the previous install-only hold **for Desktop6 only**. Approvals already granted (Version 1.1).
Status: Desktop6 released for START-WORKER / TICK / COS on that node. `holdLaunch` stays **true** (clusterHoldLaunch remains for unreleased nodes). Desktop4 stays released. Desktop5 stays install-only. Alienware-18 stays MAIN Nissan 28206 P1. Cloud Linux never opens Eleads. A Cursor worker online is not a live COS. `sends=[]` unless CRM history proves a host-side send.

## 1. Outcome

- **This PC is Desktop6** (Node6 / DeskTop6ACER). Worker `3b6886be-8df5-5399-a9f8-0b8c65e254b4` from `%USERPROFILE%` (not `C:\Windows\system32`).
- **Per-node release:** `deploy/cluster-node/cluster-hold.json` keeps `"holdLaunch": true` and sets `"releasedNodes": ["Desktop4", "Desktop6"]`. `START-WORKER.cmd` on Desktop6 is allowed. Desktop5 stays fail-closed.
- **Jobs now (provisional until device study):** Valdosta Mitsubishi **28546** P1 organizer: overdue emails first, then reschedule overdue calls, then organizer remainder. SMS 08:00–21:00 ET. Appointment-commit fast path if replies. P2/P3 only after that rooftop P1 is clear. Do not steal Alienware-18’s Nissan **28206**.
- **No SCALE-OUT-8.** Desktop6 covering P1 does not turn on the 8-PC overlay. Presence of Desktop6 alone is not scale-out.
- **Alienware-18 stays MAIN:** coordinator, clone source, lease host. Desktop6 is not the coordinator.
- **Cloud Linux** still does not open Eleads, start `Titan.ChiefOfStaff.exe`, type a password, or claim COS live. cursor-cloud has no CreateAgent/place-worker API. `sends=[]` until CRM history on a host-side Desktop6 tick.
- **Login claim:** CHECKIN.cmd claimed logged in. That is not Verify. Do not tell Glenn to type `verify`.

## 2. Hard constraints (unchanged)

- Rooftops **28206** and **28546** only. Never **6220**.
- Identity **Bordine, Glenn**.
- SMS **08:00–21:00** America/New_York.
- Ownership screen **180 days**.
- Workers from `%USERPROFILE%` only. Never `C:\Windows\system32`.
- `TICK.cmd` / `CHECKIN.cmd`, not `C:\Windows\system32\Titan.cmd`. Chrome CDP **9222**.
- P1 before P2/P3 on each rooftop. Appointment-commit fast path PR #16. DailyUpgradeQueue P1→P2→P3 PR #17.
- Offline or no host-side COS tick: `sends=[]`. Do not fabricate launch success.

## 3. What Glenn runs on DeskTop6ACER (this PC)

From `%USERPROFILE%` (never `C:\Windows\system32`):

1. `INSTALL-COS.cmd` if Titan is not already installed on this PC.
2. Skip `START-WORKER.cmd` if worker `3b6886be-8df5-5399-a9f8-0b8c65e254b4` is already online from `%USERPROFILE%`.
3. `%LOCALAPPDATA%\TitanChief\app\TICK.cmd` to start Titan COS on this PC.

Cloud cannot start `agent worker start` or place a COS subagent on his Windows box. If the worker is online and COS is not, the remaining human step is `TICK.cmd`.
