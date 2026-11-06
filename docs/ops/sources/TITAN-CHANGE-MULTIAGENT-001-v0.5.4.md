# TITAN-CHANGE-MULTIAGENT-001 v0.5.4

Filed overlay naming **Desktop5** and **Desktop6** as WiFi-connected 8-PC overflow PCs with the same install-only pattern as Desktop4. Document ID stays **TITAN-CHANGE-MULTIAGENT-001**. This file does not rewrite v0.5.3, v0.5.2, v0.5.1, v0.5.0, v0.4.0, or the Eleads source packet.

---

CONTROLLED CHANGE RECORD
Titan Autonomous + Titan Chief of Staff — Desktop5/Desktop6 named overflow, install only
Document ID: TITAN-CHANGE-MULTIAGENT-001
Version: 0.5.4
Effective date: November 6, 2026
Product owner: Glenn Bordine / Bordine Systems — Project Titan
Source authority: Glenn statement that DeskTop4, DeskTop5, DeskTop6 are connected to the WiFi network; standing orders still hold launch and keep Alienware-18 as MAIN.
Status: Launch held. Install/enroll allowed on Desktop4, Desktop5, and Desktop6. Desktop4/5/6 lanes remain provisional. WiFi ≠ Cursor worker. Cloud Linux never opens Eleads.

## 1. Outcome

- **Alienware-18 stays MAIN:** coordinator, clone source, and lease host. Desktop4, Desktop5, and Desktop6 clone **from** Alienware-18. None of them is the coordinator.
- **Launch stays held** (`holdLaunch=true`) until Glenn says all PCs are ready and online. No `START-WORKER`, no COS placement, no CHECKIN-to-send, no `SCALE-OUT-8.cmd`.
- **Install is allowed** on Desktop4, Desktop5, and Desktop6 (clone, `INSTALL.cmd`, `ENROLL-NODE.cmd`). One-shots: `deploy/desktop4/INSTALL-COS.cmd`, `deploy/desktop5/INSTALL-COS.cmd`, `deploy/desktop6/INSTALL-COS.cmd`. Each refuses `START-WORKER` and prints **INSTALLED. LAUNCH HELD.**
- **Named nodes on the LAN (WiFi):** Desktop4, Desktop5, Desktop6. WiFi connected is not a Cursor worker. Do not claim workers online unless `list-self-hosted-workers` shows them.
- **Provisional lanes (not final COS assignments):**
  - Desktop4 (Node4) = Mitsubishi **28546 P2/P3** (4-PC go-live seat; unchanged)
  - Desktop5 (Node5) = Nissan **28206 P1 calls** (8-PC overflow; provisional)
  - Desktop6 (Node6) = Mitsubishi **28546 P1 calls** (8-PC overflow; provisional)
- Final best-fit COS tasks wait until Glenn says the cluster is complete, then study each PC’s devices/ability. Not now.

## 2. Hard constraints (unchanged)

- Rooftops **28206** and **28546** only. Never **6220**.
- Identity **Bordine, Glenn**.
- SMS **08:00–21:00** America/New_York.
- Ownership screen **180 days**.
- Cloud Linux never opens Eleads, never runs `Titan.ChiefOfStaff.exe`, never types the password, never fabricates workers or sends.
- Workers start from `%USERPROFILE%` only after Glenn releases the hold. Never `C:\Windows\system32`.
- CHECKIN.cmd, not `verify`. Chrome CDP **9222**.
- P1 before P2/P3 on each rooftop.
- Never mark a call complete if no call occurred.
- Offline or held: `sends=[]`.

## 3. Release

Glenn sets `holdLaunch` to `false` in `cluster-hold.json` (or `TITAN_CLUSTER_HOLD_LAUNCH=off`) after he says the cluster is complete. Then study each PC and assign tasks. Until then, install only.
