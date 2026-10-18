# Titan Chief of Staff on Glenn’s Windows PCs

Rooftops **28206** and **28546** only. Never **6220**. Identity **Bordine, Glenn**.

Install on each PC separately. Alienware-18 is one enrolled host. One live Eleads session at a time. See `docs/deploy/windows-hosts.md`.

## Need on the PC

- .NET 8 desktop runtime
- Google Chrome
- Cursor worker: `agent worker start --verbose`

This cloud agent cannot copy files onto Glenn’s PCs.

## Install

Build with `scripts/publish-win-x64.sh` or, on the Windows PC, `scripts\publish-win-x64.cmd`. Then in cmd.exe:

```bat
deploy\alienware-local-program\INSTALL.cmd C:\path\to\dist\titan-chief-win-x64
```

Or double-click `INSTALL.cmd` inside `dist\titan-chief-win-x64\`.

That copies into `%LOCALAPPDATA%\TitanChief\app\`, enrolls this PC, and arms `kill.switch`. It does not start the host, does not open Eleads, and does not type a password. Do not copy `%LOCALAPPDATA%\TitanChief` between PCs.

## Check in

Command Prompt’s own command is named `verify`. Typing `verify` by itself never starts Titan.

After install, close every Chrome window and the tray icon, then double-click `CHECKIN.cmd` next to `Titan.ChiefOfStaff.exe`.

Sign into Eleads in that Chrome window as Bordine, Glenn on 28206 or 28546. Leave Chrome open. Double-click `CHECKIN.cmd` again. That second run is Verify. Chrome debugging stays on `127.0.0.1:9222`.

Do not type `Titan.cmd` from `C:\Windows\system32`. Another program on this PC uses that name.

The cloud loop then attaches with `scripts/launch-eleads-cos-loop.sh`. `tick` on that PC attaches to that same Chrome and does not close it.

`INSTALL.cmd` and `kill.switch=armed` are not Verify and not send authorization.
