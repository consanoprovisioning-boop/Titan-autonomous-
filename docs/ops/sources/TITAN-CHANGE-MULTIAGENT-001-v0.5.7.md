# TITAN-CHANGE-MULTIAGENT-001 v0.5.7

Filed overlay for Glenn’s **DeskTop6ACER missing-tools / programs / settings** pass so Titan COS can operate on this already-released cluster PC. Document ID stays **TITAN-CHANGE-MULTIAGENT-001**. This file does not rewrite v0.5.6, v0.5.5, v0.5.4, v0.5.3, v0.5.2, v0.5.1, v0.5.0, v0.4.0, or the Eleads source packet.

---

CONTROLLED CHANGE RECORD
Titan Autonomous + Titan Chief of Staff — Desktop6 missing-tools one-shot (Git, .NET 8, Chrome, enroll, lease)
Document ID: TITAN-CHANGE-MULTIAGENT-001
Version: 0.5.7
Effective date: November 23, 2026
Product owner: Glenn Bordine / Bordine Systems — Project Titan
Source authority: Glenn, on DeskTop6ACER (“this Cluster PC is logged in install any tools/ programs/settings missing related to the operation of Titan COS”). Approvals already granted (Version 1.1). Desktop6 remains released (v0.5.6). This is bootstrap/prereq install, not a live CRM send.
Status: One-shot `deploy/desktop6/INSTALL-MISSING.cmd` is the file Glenn runs on this PC. Cloud Linux cannot winget onto his Windows box, cannot open Eleads, cannot start `Titan.ChiefOfStaff.exe`, and cannot fabricate workers/sends. `holdLaunch` stays **true** for unreleased nodes. `releasedNodes` stays `["Desktop4", "Desktop6"]`. Desktop5 stays install-only. Alienware-18 stays MAIN.

## 1. Outcome

- **This PC is Desktop6** (Node6 / DeskTop6ACER), already in the cluster, claimed logged in as Bordine, Glenn on **28546**. Login claim is not Verify.
- **Missing-tools one-shot:** `deploy/desktop6/INSTALL-MISSING.cmd` checks and installs Git, .NET 8 desktop runtime, and Google Chrome via winget if absent; pulls `cursor/eleads-cluster-4pc-7dfe` from Alienware-18 or GitHub; runs `INSTALL.cmd` and `ENROLL-NODE.cmd Desktop6`; writes node settings (nodeId=Desktop6, rooftop 28546, identity Bordine, Glenn, SMS 08:00–21:00 ET, 180-day ownership, CDP 9222, never 6220); `setx TITAN_LEASE_DIR` to the Alienware-18 share; leaves Chrome on 9222; **does not** run `TICK.cmd`; **does not** run a second CHECKIN; START-WORKER only if no worker process is listed on this PC.
- **Do not start a second worker.** Last known Desktop6 worker `3b6886be-8df5-5399-a9f8-0b8c65e254b4` from `%USERPROFILE%` (not `C:\Windows\system32`). Prefer `%USERPROFILE%`, not OneDrive Desktop.
- **Cloud Linux** still does not open Eleads, start COS, type a password, or claim sends. `sends=[]` unless CRM history proves a host-side send.

## 2. Hard constraints (unchanged)

- Rooftops **28206** and **28546** only. Never **6220**. Desktop6 must not take Nissan **28206**.
- Identity **Bordine, Glenn**.
- SMS **08:00–21:00** America/New_York.
- Ownership screen **180 days**.
- Workers from `%USERPROFILE%` only. Never `C:\Windows\system32`.
- `TICK.cmd` / `CHECKIN.cmd`, not `C:\Windows\system32\Titan.cmd`. Chrome CDP **9222**.
- Lease host is **Alienware-18** (`TITAN_LEASE_DIR=\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases`).
- Offline or no host-side COS tick: `sends=[]`. Do not fabricate launch success.

## 3. What Glenn runs on DeskTop6ACER (this PC)

From `%USERPROFILE%` (never `C:\Windows\system32`):

```bat
\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\deploy\desktop6\INSTALL-MISSING.cmd
```

If that share does not yet have `INSTALL-MISSING.cmd`:

```bat
cd /d "%USERPROFILE%"
winget install -e --id Git.Git --accept-package-agreements --accept-source-agreements
git clone --branch cursor/eleads-cluster-4pc-7dfe https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
"%USERPROFILE%\Titan-autonomous-\deploy\desktop6\INSTALL-MISSING.cmd"
```

If the repo is already under `%USERPROFILE%\Titan-autonomous-`:

```bat
"%USERPROFILE%\Titan-autonomous-\deploy\desktop6\INSTALL-MISSING.cmd"
```

That file prints **Already present** vs **Installed this run**. It does not send. Next COS tick on this PC remains `%LOCALAPPDATA%\TitanChief\app\TICK.cmd` after tools/settings are in place. Do not run `C:\Windows\system32\Titan.cmd`.
