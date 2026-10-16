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

## Check in

Command Prompt’s own command is named `verify`. Typing `verify` by itself never starts Titan.

After install, close every Chrome window and the tray icon, then double-click `CHECKIN.cmd` next to `Titan.ChiefOfStaff.exe`.

Sign into Eleads in that Chrome window as Bordine, Glenn on 28206 or 28546. Leave Chrome open. Double-click `CHECKIN.cmd` again. That second run is Verify.

Do not type `Titan.cmd` from `C:\Windows\system32`. Another program on this PC uses that name.

The cloud loop then attaches with `scripts/launch-eleads-cos-loop.sh`. `tick` on the Alienware attaches to that same Chrome and does not close it.

`INSTALL.cmd` and `kill.switch=armed` are not Verify and not send authorization.
