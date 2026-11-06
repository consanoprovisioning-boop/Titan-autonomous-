# Desktop4 — clone Titan Autonomous + Titan COS from Alienware-18

**Cloud did not copy files onto Desktop4.** This playbook is what Glenn runs **on Desktop4**. PC4 in the 4-PC cluster. Named **Node4**. Rooftop **Valdosta Mitsubishi 28546**. Lane **P2/P3** (comb the Mitsubishi database, then tailored upgrades after Mitsubishi P1 on Desktop3 is clear).

Never **6220**. Identity **Bordine, Glenn**. Worker directory **`%USERPROFILE%`**, never `C:\Windows\system32`. Chrome CDP **9222**. SMS **08:00–21:00** ET. Ownership screen **180 days**. P1 before P2/P3.

Alienware-18 stays Nissan 28206 P1. Desktop3 stays Mitsubishi 28546 P1. Enroll as `Desktop4` or `Node4` — both map to this lane.

## What Glenn needs on Desktop4

- Windows 10/11
- Git (LAN clone from Alienware-18) or network share access (robocopy)
- .NET 8 desktop runtime (SDK only if this PC must publish)
- Google Chrome
- Cursor CLI (`agent`) signed in (`agent login`)

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

Or publish on Desktop4 (needs the .NET 8 SDK) / on Alienware-18 and then copy `dist\titan-chief-win-x64\`:

```bat
cd /d "%USERPROFILE%\Titan-autonomous-"
scripts\publish-win-x64.cmd
```

Confirm `Titan.ChiefOfStaff.exe` is present. `CHECKIN.cmd` and `INSTALL.cmd` sit beside that exe.

## 3. Install (not Verify, not a send)

```bat
cd /d "%USERPROFILE%\Titan-autonomous-\dist\titan-chief-win-x64"
INSTALL.cmd
```

Expected: files in `%LOCALAPPDATA%\TitanChief\app\`, `kill.switch` reads `armed`. Titan never types the password.

## 4. Enroll this PC as Desktop4

```bat
cd /d "%USERPROFILE%\Titan-autonomous-\deploy\cluster-node"
ENROLL-NODE.cmd Desktop4
```

Or double-click `deploy\desktop4\ENROLL.cmd`. `ENROLL-NODE.cmd Node4` is the same lane: it writes `nodeId=Desktop4` and rooftop 28546. It does not start Eleads.

## 5. Start the Cursor worker

Double-click `deploy\desktop4\START-WORKER.cmd` (also `deploy\cluster-node\START-WORKER.cmd` after enroll).

The window **must** show `%USERPROFILE%` (example `C:\Users\Glenn R Bordine`). Leave it open.

```bat
cd /d "%USERPROFILE%"
agent worker start --name "Desktop4" --verbose
```

If `agent` is missing, the script tries `%LOCALAPPDATA%\cursor-agent\agent.cmd`. Never start the worker from `C:\Windows\system32`.

## 6. CHECKIN + Verify (Mitsubishi)

1. Close every Chrome window and the tray icon.
2. Double-click `CHECKIN.cmd` beside `Titan.ChiefOfStaff.exe`.
3. Sign into Eleads as **Bordine, Glenn** on **Valdosta Mitsubishi (28546)**. Never **6220**.
4. Double-click `CHECKIN.cmd` again. That second run is Verify. Do not type `verify`.
5. Leave that Chrome window open on CDP 9222.

Do not start `Titan.ChiefOfStaff.exe` from Explorer except through CHECKIN / TICK. Do not run `Titan.cmd` from `C:\Windows\system32`.

## 7. Desktop4 job

While verified, Desktop4 only works **28546 P2/P3**:

1. Confirm Mitsubishi P1 is clear on Desktop3 (`DailyUpgradeQueue.CanStartOnRooftop`).
2. Comb the Mitsubishi database (equity, abandon, in-market, upgrade). Full activity feed. No list-view send.
3. Record ownership (180-day screen), permissions, vehicle, VOI, timestamps. Hold others’ leads (72h / 96h).
4. After P2, send tailored upgrade emails from proven profile facts. Eligible SMS 08:00–21:00 ET.
5. Appointment-commit fast path when a Mitsubishi customer already gave a day and time.

Desktop4 does **not** work Nissan 28206 and does **not** take Mitsubishi P1 organizer (that is Desktop3).

## Cloud attach

This cloud agent inventories workers and records ready or blocked. It will not open Eleads. A missing Desktop4 worker stays `status=missing`. `sends=[]` until a verified local worker + session exist. Cloud Linux cannot copy files onto Desktop4 itself.
