# Desktop6 — install Titan from Alienware-18 (no launch)

**Cloud did not copy files onto Desktop6.** This playbook is what Glenn runs **on Desktop6**. Clone, install, and enroll only.

**Alienware-18 is the PC cluster MAIN computer:** coordinator, clone source, and lease host (`TITAN_LEASE_DIR` on Alienware-18). Desktop6 is a specialist that clones **from** Alienware-18. Do not make Desktop6 the coordinator. Desktop4 is also not the coordinator.

**Provisional lane (not final):** named **Node6**, rooftop **Valdosta Mitsubishi 28546**, 8-PC overflow **P1 calls**. This is **not** a 4-PC go-live seat. Glenn will say when the cluster is complete. Then we study each PC’s devices/ability and assign best-fit COS tasks. Not now. Do not run `SCALE-OUT-8.cmd` yet.

WiFi on the LAN is not a Cursor worker. Never **6220**. Identity **Bordine, Glenn**. Worker directory **`%USERPROFILE%`**, never `C:\Windows\system32`. Chrome CDP **9222**. SMS **08:00–21:00** ET. Ownership screen **180 days**. P1 before P2/P3. Never mark a call complete if no call occurred.

## DO NOT launch

Until Glenn says **all PCs are ready and online**, do **not**:

- run `START-WORKER.cmd`
- run `agent worker start`
- CHECKIN-to-send / Verify as go-live
- place a COS agent on Desktop6
- start `Titan.ChiefOfStaff.exe`
- turn on 8-PC scale-out

`START-WORKER.cmd` fail-closes while `deploy\cluster-node\cluster-hold.json` has `"holdLaunch": true`. Install and enroll are allowed.

## One-shot on Desktop6 (install only)

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

That one file clones from Alienware-18 if the repo is missing, checks out `cursor/eleads-cluster-4pc-7dfe`, copies or publishes `dist\titan-chief-win-x64`, runs `INSTALL.cmd`, runs `ENROLL-NODE.cmd Desktop6`, prints **INSTALLED. LAUNCH HELD.**, and **STOPS**. It refuses `START-WORKER`. It does not CHECKIN-to-send. Cloud Linux cannot run this file.

## What Glenn needs on Desktop6

- Windows 10/11
- Git (LAN clone from Alienware-18) or network share access (robocopy)
- .NET 8 desktop runtime (SDK only if this PC must publish)
- Google Chrome
- Cursor CLI (`agent`) signed in (`agent login`) — install the CLI; do not start the worker yet

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

Confirm `Titan.ChiefOfStaff.exe` is present. `INSTALL.cmd` sits beside that exe. Do not run `CHECKIN.cmd` yet.

## 3. Install (not Verify, not a send)

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

`ENROLL-NODE.cmd Node6` is the same **provisional** overflow lane: it writes `nodeId=Desktop6` and rooftop 28546. It does not start Eleads. It does not start the worker. It does not turn on scale-out.

Point this PC at the Alienware-18 lease folder when Glenn shares it (do not host leases on Desktop6):

```bat
setx TITAN_LEASE_DIR "\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases"
```

## Stop here

Install on Desktop6 is done. **Do not** run `START-WORKER.cmd`. **Do not** CHECKIN-to-send. **Do not** run `SCALE-OUT-8.cmd`. Wait until Glenn says the cluster is complete and all PCs are ready and online.

## Held until Glenn releases the cluster

These steps stay filed. Do not run them now. `START-WORKER.cmd` fail-closes while hold is on.

1. Double-click `deploy\cluster-node\START-WORKER.cmd` from `%USERPROFILE%` (never `C:\Windows\system32`).
2. Close every Chrome window and the tray icon.
3. `CHECKIN.cmd` beside `Titan.ChiefOfStaff.exe`, sign in as Bordine, Glenn on **28546**, `CHECKIN.cmd` again. Leave Chrome on CDP 9222.

Provisional job after release **and** 8-PC scale-out: Mitsubishi 28546 P1 overdue call reschedule. Never mark a call complete if no call occurred. Final COS tasks wait for Glenn’s per-PC study.

## Cloud attach

This cloud agent inventories workers and records ready or blocked. It will not open Eleads. Launch is held (`clusterHoldLaunch=true`). WiFi connected is not `status=online`. A missing Desktop6 worker stays `status=missing`. `sends=[]`. Cloud Linux cannot copy files onto Desktop6 itself.
