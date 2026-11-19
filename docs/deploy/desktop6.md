# Desktop6 — install Titan from Alienware-18, then join the cluster

**Cloud did not copy files onto Desktop6.** This playbook is what Glenn runs **on Desktop6 (DeskTop6ACER)**. Desktop6 is released to join. Desktop5 stays install-only.

**Alienware-18 is the PC cluster MAIN computer:** coordinator, clone source, and lease host (`TITAN_LEASE_DIR` on Alienware-18). Desktop6 is a specialist that clones **from** Alienware-18. Do not make Desktop6 the coordinator. Desktop6 must **not** take Nissan **28206**.

**Provisional jobs (Glenn assigned now, device study later):** named **Node6**, rooftop **Valdosta Mitsubishi 28546**, P1 organizer. Order: overdue emails first (no extra approval after Verify), then reschedule overdue calls, then organizer remainder. SMS **08:00–21:00** ET. Appointment-commit fast path if replies. P2/P3 only after that rooftop P1 is clear.

Never **6220**. Identity **Bordine, Glenn**. Worker directory **`%USERPROFILE%`**, never `C:\Windows\system32`. Chrome CDP **9222**. Ownership screen **180 days**. P1 before P2/P3.

## Desktop6 is released

Glenn released **this PC** (DeskTop6ACER) to start Titan COS and take Mitsubishi 28546 P1 jobs now. `cluster-hold.json` keeps `"holdLaunch": true` (clusterHoldLaunch remains for unreleased nodes) and lists `"releasedNodes": ["Desktop4", "Desktop6"]`. `START-WORKER.cmd` on Desktop6 is allowed. Do not start the worker from `C:\Windows\system32`. Cloud Linux cannot run `agent worker start` or `TICK.cmd` on this Windows box, and has no CreateAgent/place-worker API.

A Cursor worker already online from `%USERPROFILE%` (including `C:\Users\glenn\OneDrive\Desktop`) is the cluster worker. Do not start a second worker. That worker process is **not** Titan COS.

## One-shot on Desktop6 (install, then START COS)

Command Prompt on Desktop6, from `%USERPROFILE%`. Never `C:\Windows\system32`.

Preferred (clone source is Alienware-18):

```bat
\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\deploy\desktop6\INSTALL-COS.cmd
```

If that share does not yet have `INSTALL-COS.cmd`, GitHub fallback then the same file:

```bat
cd /d "%USERPROFILE%"
git clone --branch cursor/eleads-cluster-4pc-7dfe https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
"%USERPROFILE%\Titan-autonomous-\deploy\desktop6\INSTALL-COS.cmd"
```

If the repo is already under `%USERPROFILE%\Titan-autonomous-`:

```bat
"%USERPROFILE%\Titan-autonomous-\deploy\desktop6\INSTALL-COS.cmd"
```

That one file clones from Alienware-18 if the repo is missing, checks out `cursor/eleads-cluster-4pc-7dfe`, copies or publishes `dist\titan-chief-win-x64`, runs `INSTALL.cmd`, runs `ENROLL-NODE.cmd Desktop6`, prints **INSTALLED. DESKTOP6 RELEASED TO JOIN.**, and **STOPS**. It does not start the worker itself. Cloud Linux cannot run this file.

## What Glenn needs on Desktop6

- Windows 10/11
- Git (LAN clone from Alienware-18) or network share access (robocopy)
- .NET 8 desktop runtime (SDK only if this PC must publish)
- Google Chrome
- Cursor CLI (`agent`) signed in (`agent login`) — worker may already be running from `%USERPROFILE%`

The steps below are what `INSTALL-COS.cmd` does. Use them only if you are not running the one-shot.

## 1. Preferred: clone from Alienware-18 on the LAN

On Desktop6, Command Prompt, starting from Glenn’s profile. Do **not** copy `%LOCALAPPDATA%\TitanChief` from Alienware-18.

If the Alienware computer name or profile folder differs, run `hostname` and `echo %USERPROFILE%` on Alienware-18 and substitute.

### Git clone from the Alienware working copy

```bat
cd /d "%USERPROFILE%"
git clone "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-" Titan-autonomous-
cd Titan-autonomous-
git checkout cursor/eleads-cluster-4pc-7dfe
```

If that share is missing, use the admin share (same folder):

```bat
cd /d "%USERPROFILE%"
git clone "\\Alienware-18\C$\Users\Glenn R Bordine\Titan-autonomous-" Titan-autonomous-
cd Titan-autonomous-
git checkout cursor/eleads-cluster-4pc-7dfe
```

### Robocopy if Git cannot clone from UNC

```bat
cd /d "%USERPROFILE%"
robocopy "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-" "%USERPROFILE%\Titan-autonomous-" /E /XD node_modules bin obj
cd /d "%USERPROFILE%\Titan-autonomous-"
git checkout cursor/eleads-cluster-4pc-7dfe
```

Robocopy exit codes 0–7 mean copy finished. Do not treat 1 as failure.

## 1b. Fallback: GitHub checkout of the cluster branch

Use this when Desktop6 cannot see Alienware-18.

```bat
cd /d "%USERPROFILE%"
git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
cd Titan-autonomous-
git checkout cursor/eleads-cluster-4pc-7dfe
```

If Git is not installed, copy the repo folder from Alienware-18 onto Desktop6 (USB). Still do not copy `%LOCALAPPDATA%\TitanChief`.

## 2. Get the Windows host files

Prefer the already-published folder from Alienware-18:

```bat
robocopy "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\dist\titan-chief-win-x64" "%USERPROFILE%\Titan-autonomous-\dist\titan-chief-win-x64" /E
```

Or publish on Alienware-18 (the MAIN build PC) and then copy `dist\titan-chief-win-x64\`. Publish on Desktop6 only if that PC has the .NET 8 SDK:

```bat
cd /d "%USERPROFILE%\Titan-autonomous-"
scripts\publish-win-x64.cmd
```

Confirm `Titan.ChiefOfStaff.exe` is present. `INSTALL.cmd` sits beside that exe.

## 3. Install (not a send)

```bat
cd /d "%USERPROFILE%\Titan-autonomous-\dist\titan-chief-win-x64"
INSTALL.cmd
```

Expected: files in `%LOCALAPPDATA%\TitanChief\app\`, `kill.switch` reads `armed`. Titan never types the password. This is not launch.

## 4. Enroll this PC as Desktop6

```bat
cd /d "%USERPROFILE%\Titan-autonomous-\deploy\cluster-node"
ENROLL-NODE.cmd Desktop6
```

`ENROLL-NODE.cmd Node6` is the same **provisional** covering lane: it writes `nodeId=Desktop6` and rooftop 28546. It does not start Eleads. It does not start the worker. It does not turn on scale-out.

Point this PC at the Alienware-18 lease folder when Glenn shares it (do not host leases on Desktop6):

```bat
setx TITAN_LEASE_DIR "\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases"
```

## Next on this PC (Desktop6 is released)

Install on Desktop6 is done. **Now** start COS:

1. If no Cursor worker window is open, double-click `deploy\desktop6\START-WORKER.cmd` from `%USERPROFILE%` (never `C:\Windows\system32`). Leave that window open. If worker `3b6886be-8df5-5399-a9f8-0b8c65e254b4` is already online from `%USERPROFILE%`, skip this.
2. Double-click `%LOCALAPPDATA%\TitanChief\app\TICK.cmd` (or `deploy\alienware-local-program\TICK.cmd`). That starts Titan COS on this PC. Do not run `C:\Windows\system32\Titan.cmd`.

Provisional jobs: Mitsubishi 28546 P1 overdue emails, then overdue call reschedule, then organizer remainder, SMS 08:00–21:00 ET, appointment-commit fast path. Never mark a call complete if no call occurred. Do not take Nissan 28206. Desktop5 stays install-only. Do not run `SCALE-OUT-8.cmd`.

## Cloud attach

This cloud agent inventories workers and records ready or blocked. It will not open Eleads. `clusterHoldLaunch` stays true for unreleased nodes; Desktop6 is in `releasedNodes`. A missing Desktop6 worker stays `status=missing`. Cloud Linux cannot CreateAgent/PLACE a COS subagent onto the worker. `sends=[]` unless a host-side COS tick actually sent and CRM history proves it.
