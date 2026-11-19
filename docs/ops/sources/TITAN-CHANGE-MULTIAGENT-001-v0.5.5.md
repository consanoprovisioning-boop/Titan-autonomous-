# TITAN-CHANGE-MULTIAGENT-001 v0.5.5

Filed overlay for Glenn’s per-node release of **Desktop4** so that PC can finish setup and join the live cluster. Document ID stays **TITAN-CHANGE-MULTIAGENT-001**. This file does not rewrite v0.5.4, v0.5.3, v0.5.2, v0.5.1, v0.5.0, v0.4.0, or the Eleads source packet.

---

CONTROLLED CHANGE RECORD
Titan Autonomous + Titan Chief of Staff — Desktop4 per-node launch release
Document ID: TITAN-CHANGE-MULTIAGENT-001
Version: 0.5.5
Effective date: November 6, 2026
Product owner: Glenn Bordine / Bordine Systems — Project Titan
Source authority: Glenn, on Desktop4 (“this PC” / DeskTop4): “finish setup and launch COS to add this PC to cluster.” Overrides the previous all-PCs-ready hold **for Desktop4 only**.
Status: Desktop4 released for START-WORKER / CHECKIN / COS placement on that node. `holdLaunch` stays **true** (clusterHoldLaunch remains for unreleased nodes). Desktop5 and Desktop6 stay install-only. Alienware-18 stays MAIN. Cloud Linux never opens Eleads. `workers=[]` is not a live COS.

## 1. Outcome

- **This PC is Desktop4** (Node4). Conversation identity DeskTop4. Desktop5 and Desktop6 are on WiFi; they were not named as ready.
- **Per-node release:** `deploy/cluster-node/cluster-hold.json` keeps `"holdLaunch": true` and adds `"releasedNodes": ["Desktop4"]`. `START-WORKER.cmd` on Desktop4 is allowed. Other nodes stay fail-closed.
- **Desktop5 / Desktop6 stay install-only** until Glenn says so. Do not `SCALE-OUT-8.cmd`.
- **Alienware-18 stays MAIN:** coordinator, clone source, lease host (`TITAN_LEASE_DIR` on Alienware-18). Desktop4 is not the coordinator.
- **Cloud Linux** still does not open Eleads, start `Titan.ChiefOfStaff.exe`, type a password, or claim COS live while no correct-dir worker is online. `sends=[]` until CRM history on a Glenn-verified Desktop4 session.
- **Provisional lane unchanged:** Desktop4 = Mitsubishi **28546 P2/P3**. P1 overdue emails + overdue call reschedule still outrank P2/P3 on that rooftop.

## 2. Hard constraints (unchanged)

- Rooftops **28206** and **28546** only. Never **6220**.
- Identity **Bordine, Glenn**.
- SMS **08:00–21:00** America/New_York.
- Ownership screen **180 days**.
- Workers from `%USERPROFILE%` only. Never `C:\Windows\system32`.
- CHECKIN.cmd, not `verify`, not `C:\Windows\system32\Titan.cmd`. Chrome CDP **9222**.
- P1 before P2/P3 on each rooftop. Appointment-commit fast path PR #16. DailyUpgradeQueue P1→P2→P3 PR #17.
- Offline or no Verify: `sends=[]`. Do not fabricate launch success.

## 3. What Glenn runs on Desktop4 (this PC)

From `%USERPROFILE%` (never `C:\Windows\system32`):

1. `INSTALL-COS.cmd` if Titan is not already installed on this PC.
2. `deploy\desktop4\START-WORKER.cmd` — leave that window open.
3. `CHECKIN.cmd` twice as Bordine, Glenn on **28546**. Leave Chrome on CDP 9222.

Cloud cannot start `agent worker start` on his Windows box. If `list-self-hosted-workers` is empty, COS is not live.
