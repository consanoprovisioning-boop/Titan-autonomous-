# Titan Chief of Staff on Alienware 18

Rooftops **28206** and **28546** only. Never **6220**. Identity **Bordine, Glenn**.

## Need on the PC

- .NET 8 desktop runtime
- Google Chrome
- Cursor worker: `agent worker start --verbose` (id `1a1f1f15-72c4-4334-ad8b-f87d54efe73a`)

This cloud agent cannot copy files onto the Alienware.

## Install

Build with `scripts/publish-win-x64.sh` or, on the Alienware, `scripts\publish-win-x64.cmd`. Then in cmd.exe:

```bat
deploy\alienware-local-program\INSTALL.cmd C:\path\to\dist\titan-chief-win-x64
```

Or double-click `INSTALL.cmd` inside `dist\titan-chief-win-x64\`.

That copies into `%LOCALAPPDATA%\TitanChief\app\` and arms `kill.switch`. It does not start the host, does not open Eleads, and does not type a password.

## Home-screen app and local live loop (Glenn, 2026-10-22)

Titan operates two ways from the same install in `%LOCALAPPDATA%\TitanChief\app`:

1. **Local program** — `Titan.ChiefOfStaff.exe` with verbs `checkin`, `tick`, `loop`, and `autostart`. `autostart` is the one-double-click path: Verify (waiting for Glenn's own sign-in if Chrome shows the login page), then run the live loop. `loop` repeats `tick` against Glenn's verified Chrome (CDP 9222) at the operating-law pace (`LoopPace`: 15 min while SMS is open 09:00–19:00 ET, 30 min evenings, 45 min overnight), stops on Ctrl+C or when Verify is lost, and never types a password.
2. **Home-screen app** — `Titan COS.exe` (`src/Titan.Launcher`), a single-file Windows app. `INSTALL.cmd` puts it on the Desktop and launches it automatically; double-clicking the Desktop app runs `autostart` with no further clicks. Manual Check In / Verify, Tick Once, Restart, and Stop buttons remain in the window.

`LOOP.cmd` runs the same console loop without the window. `Titan COS.cmd` opens the app window and is the fallback copied to the Desktop if shortcut creation fails.

## Check in

Command Prompt’s own command is named `verify`. Typing `verify` by itself never starts Titan.

After install, close every Chrome window and the tray icon, then double-click `CHECKIN.cmd` next to `Titan.ChiefOfStaff.exe` — or use the app's Check In / Verify button, which runs the same verb.

Sign into Eleads in that Chrome window as Bordine, Glenn on 28206 or 28546. Leave Chrome open. Double-click `CHECKIN.cmd` again. That second run is Verify.

Do not type `Titan.cmd` from `C:\Windows\system32`. Another program on this PC uses that name.

The cloud loop then attaches with `scripts/launch-eleads-cos-loop.sh`. `tick`/`loop` on the Alienware attach to that same Chrome and do not close it.

`INSTALL.cmd` and `kill.switch=armed` are not Verify and not send authorization.
