# Titan Chief of Staff on Alienware 18

Rooftops **28206** and **28546** only. Never **6220**. Identity **Bordine, Glenn**.

## Need on the PC

- .NET 8 desktop runtime
- Google Chrome

This cloud agent cannot copy files onto the Alienware. Do the install in a new Command Prompt, not in the Cursor worker window (`C:\Windows\system32`).

## Install

The win-x64 publish is already in `dist\titan-chief-win-x64`. In cmd.exe:

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

After Verify, leave Chrome open. `tick` on the Alienware attaches to that same Chrome and does not close it. Do not run `C:\Windows\system32\Titan.cmd`.

`INSTALL.cmd` and `kill.switch=armed` are not Verify and not send authorization.
