# Desktop3 — clone Titan Autonomous + Titan COS

**Cloud did not copy files onto Desktop3.** This playbook is what Glenn runs on that PC. PC2 in the 4-PC cluster. Rooftop **Valdosta Mitsubishi 28546**. Lane **P1 organizer**.

**Alienware-18 is MAIN.** Clone from Alienware-18. Desktop3 is a specialist, not the coordinator.

**Launch is held** until Glenn says all PCs are ready and online. Clone, install, and enroll now. Do **not** run `START-WORKER.cmd` or CHECKIN-to-send yet.

Never **6220**. Identity **Bordine, Glenn**. Worker directory **`%USERPROFILE%`**, never `C:\Windows\system32`. Chrome CDP **9222**.

## What Glenn needs on Desktop3

- Windows 10/11
- Git (or a USB copy of this repo)
- .NET 8 desktop runtime
- Google Chrome
- Cursor CLI (`agent`) signed in (`agent login`)

## 1. Clone from Alienware-18 (preferred)

```bat
cd /d "%USERPROFILE%"
git clone "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-" Titan-autonomous-
cd Titan-autonomous-
git checkout cursor/eleads-cluster-4pc-7dfe
```

GitHub fallback:

```bat
cd /d "%USERPROFILE%"
git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
cd Titan-autonomous-
git checkout cursor/eleads-cluster-4pc-7dfe
```

If Git is not installed, copy the repo folder from Alienware-18 onto Desktop3 (USB). Do not copy `%LOCALAPPDATA%\TitanChief` from another PC.

## 2. Get the Windows host files

On a build PC (or Desktop3 if the SDK is installed):

```bat
scripts\publish-win-x64.cmd
```

Or copy `dist\titan-chief-win-x64\` from a trusted publish. Confirm `Titan.ChiefOfStaff.exe` is present.

## 3. Install (not Verify, not a send)

```bat
cd /d "%USERPROFILE%\Titan-autonomous-\dist\titan-chief-win-x64"
INSTALL.cmd
```

Expected: files in `%LOCALAPPDATA%\TitanChief\app\`, `kill.switch` reads `armed`. Titan never types the password.

## 4. Enroll this PC as Desktop3

```bat
cd /d "%USERPROFILE%\Titan-autonomous-\deploy\cluster-node"
ENROLL-NODE.cmd Desktop3
```

That writes `%LOCALAPPDATA%\TitanChief\node.json` with `nodeId=Desktop3` and rooftop 28546. It does not start Eleads.

## 5. STOP — do not start the worker yet

`START-WORKER.cmd` fail-closes while cluster hold is on. Do not run it until Glenn says all PCs are ready and online. When he does, the window **must** show `%USERPROFILE%` (example `C:\Users\Glenn R Bordine`), never `C:\Windows\system32`.

## 6. CHECKIN + Verify (held)

Do **not** CHECKIN-to-send until Glenn says all PCs are ready. Filed for then:

1. Close every Chrome window and the tray icon.
2. Double-click `CHECKIN.cmd` beside `Titan.ChiefOfStaff.exe`.
3. Sign into Eleads as **Bordine, Glenn** on **Valdosta Mitsubishi (28546)**.
4. Double-click `CHECKIN.cmd` again. That second run is Verify. Do not type `verify`.
5. Leave that Chrome window open on CDP 9222.

Do not start `Titan.ChiefOfStaff.exe` from Explorer except through CHECKIN / TICK. Do not run `Titan.cmd` from `C:\Windows\system32`.

## 7. Desktop3 job

While verified, Desktop3 only works **28546 P1**:

1. Complete overdue emails (high-velocity, no approval after Verify).
2. Reschedule overdue calls. Never mark a call complete if no call occurred.
3. Remaining Daily Organizer Open/All for Glenn-assigned Mitsubishi work.
4. Eligible SMS 08:00–21:00 ET.
5. Appointment-commit fast path when a Mitsubishi customer already gave a day and time.

Desktop3 does **not** comb the Nissan database and does **not** send Mitsubishi upgrade emails until Desktop4 (Node4)’s P2/P3 lane is open after Mitsubishi P1 is clear.

## Cloud attach

This cloud agent inventories workers and records ready or blocked. It will not open Eleads. Launch is held. A missing Desktop3 worker stays `status=missing`. `sends=[]`.
