# TITAN-CHANGE-MULTIAGENT-001 v0.5.3

Filed overlay for Glenn’s standing orders while the PC cluster is still being set up. Document ID stays **TITAN-CHANGE-MULTIAGENT-001**. This file does not rewrite v0.5.2, v0.5.1, v0.5.0, v0.4.0, or the Eleads source packet.

---

CONTROLLED CHANGE RECORD
Titan Autonomous + Titan Chief of Staff — hold launch, Alienware-18 MAIN
Document ID: TITAN-CHANGE-MULTIAGENT-001
Version: 0.5.3
Effective date: November 6, 2026
Product owner: Glenn Bordine / Bordine Systems — Project Titan
Source authority: Glenn standing orders (1) cluster still being set up — he will say when complete, then study each PC and assign best-fit COS tasks; (2) install on Desktop4 allowed, do not launch until all PCs are ready and online; (3) Alienware-18 is the PC cluster MAIN computer.
Status: Launch held. Install/enroll allowed. Desktop4 lane remains provisional Mitsubishi 28546 P2/P3. Cloud Linux never opens Eleads.

## 1. Outcome

- **Alienware-18 is MAIN:** coordinator, clone source, and lease host (`TITAN_LEASE_DIR` on Alienware-18). Desktop3, Desktop4, and later nodes clone **from** Alienware-18. Desktop4 is not the coordinator.
- **Launch is held** (`deploy/cluster-node/cluster-hold.json` `holdLaunch=true`) until Glenn says all PCs are ready and online. `START-WORKER.cmd` and `scripts/launch-eleads-cos-loop.sh` fail-closed. No COS placement. No CHECKIN-to-send.
- **Install is allowed** on Desktop4 (clone, `INSTALL.cmd`, `ENROLL-NODE.cmd Desktop4`).
- **Desktop4 lane is provisional:** Mitsubishi 28546 P2/P3 (named Node4). Final best-fit COS tasks wait for Glenn’s per-PC study. Not now.

## 2. Hard constraints (unchanged)

- Rooftops **28206** and **28546** only. Never **6220**.
- Identity **Bordine, Glenn**.
- SMS **08:00–21:00** America/New_York.
- Ownership screen **180 days**.
- Cloud Linux never opens Eleads, never runs `Titan.ChiefOfStaff.exe`, never types the password, never fabricates workers or sends.
- Workers start from `%USERPROFILE%` only after Glenn releases the hold. Never `C:\Windows\system32`.
- CHECKIN.cmd, not `verify`. Chrome CDP **9222**.
- P1 before P2/P3 on each rooftop.
- Offline or held: `sends=[]`.

## 3. Release

Glenn sets `holdLaunch` to `false` in `cluster-hold.json` (or `TITAN_CLUSTER_HOLD_LAUNCH=off`) after he says the cluster is complete. Then study each PC and assign tasks. Until then, install only.
