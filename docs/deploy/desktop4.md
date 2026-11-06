# Desktop4 — install Titan from Alienware-18, then join the cluster

**Cloud did not copy files onto Desktop4.** This playbook is what Glenn runs **on Desktop4**. Desktop4 is released to join. Desktop5 and Desktop6 stay install-only.

**Alienware-18 is the PC cluster MAIN computer:** coordinator, clone source, and lease host (`TITAN_LEASE_DIR` on Alienware-18). Desktop4 is a specialist that clones **from** Alienware-18. Do not make Desktop4 the coordinator.

**Provisional lane:** named **Node4**, rooftop **Valdosta Mitsubishi 28546**, P2/P3. P1 overdue emails and overdue call reschedule on 28546 still outrank this lane.

Never **6220**. Identity **Bordine, Glenn**. Worker directory **`%USERPROFILE%`**, never `C:\Windows\system32`. Chrome CDP **9222**. SMS **08:00–21:00** ET. Ownership screen **180 days**. P1 before P2/P3.

## Desktop4 is released

Glenn released **this PC** to finish setup and launch COS. `cluster-hold.json` keeps `"holdLaunch": true` (clusterHoldLaunch remains for unreleased nodes) and lists `"releasedNodes": ["Desktop4"]`. `START-WORKER.cmd` on Desktop4 is allowed. Do not start the worker from `C:\Windows\system32`. Cloud Linux cannot run `agent worker start` on this Windows box.

## One-shot on Desktop4 (install, then START-WORKER)

Command Prompt on Desktop4, from `%USERPROFILE%`. Never `C:\Windows\system32`.

Preferred (clone source is Alienware-18):

```bat
\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\deploy\desktop4\INSTALL-COS.cmd
```

If that share does not yet have `INSTALL-COS.cmd`, GitHub fallback then the same file:

```bat
cd /d "%USERPROFILE%"
git clone --branch cursor/eleads-cluster-4pc-7dfe https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
"%USERPROFILE%\Titan-autonomous-\deploy\desktop4\INSTALL-COS.cmd"
```

If the repo is already under `%USERPROFILE%\Titan-autonomous-`:

```bat
"%USERPROFILE%\Titan-autonomous-\deploy\desktop4\INSTALL-COS.cmd"
```

That one file clones from Alienware-18 if the repo is missing, checks out `cursor/eleads-cluster-4pc-7dfe`, copies or publishes `dist\titan-chief-win-x64`, runs `INSTALL.cmd`, runs `ENROLL-NODE.cmd Desktop4`, prints **INSTALLED. DESKTOP4 RELEASED TO JOIN.**, and **STOPS**. It does not start the worker itself. Cloud Linux cannot run this file.

## What Glenn needs on Desktop4

- Windows 10/11
- Git (LAN clone from Alienware-18) or network share access (robocopy)
- .NET 8 desktop runtime (SDK only if this PC must publish)
- Google Chrome
- Cursor CLI (`agent`) signed in (`agent login`) — then `START-WORKER.cmd` from `%USERPROFILE%`

The steps below are what `INSTALL-COS.cmd` does. Use them only if you are not running the one-shot.

## 1. Preferred: clone from Alienware-18 on the LAN

On Desktop4, Command Prompt, starting from Glenn’s profile. Do **not** copy `%LOCALAPPDATA%\TitanChief` from Alienware-18.

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

Use this when Desktop4 cannot see Alienware-18.

```bat
cd /d "%USERPROFILE%"
git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
cd Titan-autonomous-
git checkout cursor/eleads-cluster-4pc-7dfe
```

If Git is not installed, copy the repo folder from Alienware-18 onto Desktop4 (USB). Still do not copy `%LOCALAPPDATA%\TitanChief`.

## 2. Get the Windows host files

Prefer the already-published folder from Alienware-18:

```bat
robocopy "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-\dist\titan-chief-win-x64" "%USERPROFILE%\Titan-autonomous-\dist\titan-chief-win-x64" /E
```

Or publish on Alienware-18 (the MAIN build PC) and then copy `dist\titan-chief-win-x64\`. Publish on Desktop4 only if that PC has the .NET 8 SDK:

```bat
cd /d "%USERPROFILE%\Titan-autonomous-"
scripts\publish-win-x64.cmd
```

Confirm `Titan.ChiefOfStaff.exe` is present. `INSTALL.cmd` sits beside that exe. Do not run `CHECKIN.cmd` yet.

## 3. Install (not Verify, not a send)

```bat
cd /d "%USERPROFILE%\Titan-autonomous-\dist\titan-chief-win-x64"
INSTALL.cmd
```

Expected: files in `%LOCALAPPDATA%\TitanChief\app\`, `kill.switch` reads `armed`. Titan never types the password. This is not launch.

## 4. Enroll this PC as Desktop4

```bat
cd /d "%USERPROFILE%\Titan-autonomous-\deploy\cluster-node"
ENROLL-NODE.cmd Desktop4
```

Or double-click `deploy\desktop4\ENROLL.cmd`. `ENROLL-NODE.cmd Node4` is the same **provisional** lane: it writes `nodeId=Desktop4` and rooftop 28546. It does not start Eleads. It does not start the worker.

Point this PC at the Alienware-18 lease folder when Glenn shares it (do not host leases on Desktop4):

```bat
setx TITAN_LEASE_DIR "\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases"
```

## Next on this PC (Desktop4 is released)

Install on Desktop4 is done. **Now** join the cluster:

1. Double-click `deploy\desktop4\START-WORKER.cmd` from `%USERPROFILE%` (never `C:\Windows\system32`). Leave that window open.
2. Close every Chrome window and the tray icon.
3. `CHECKIN.cmd` beside `Titan.ChiefOfStaff.exe`, sign in as Bordine, Glenn on **28546**, `CHECKIN.cmd` again. Leave Chrome on CDP 9222. Do not type `verify`. Do not run `C:\Windows\system32\Titan.cmd`.

Provisional job: Mitsubishi 28546 P2/P3 after Desktop3 P1 is clear. Desktop5 and Desktop6 stay install-only.

## Cloud attach

This cloud agent inventories workers and records ready or blocked. It will not open Eleads. `clusterHoldLaunch` stays true for unreleased nodes; Desktop4 is in `releasedNodes`. A missing Desktop4 worker stays `status=missing`. COS is not live while `workers=[]`. `sends=[]`. Cloud Linux cannot copy files onto Desktop4 itself and cannot start the worker.
