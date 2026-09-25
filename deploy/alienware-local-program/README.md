# Titan Autonomous on Alienware 18

Finished local suite. Chief of Staff is the host. Rooftops **28206** and **28546** only. Never **6220**.

## Need on the PC

- [.NET 8 desktop runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Google Chrome

This cloud agent cannot copy files onto the Alienware.

## Install

1. Download branch `cursor/titan-chief-host-7954` (PR #2) as a ZIP.
2. In cmd.exe:

```bat
deploy\alienware-local-program\INSTALL.cmd C:\path\to\dist\titan-chief-win-x64
```

That copies into `%LOCALAPPDATA%\TitanChief\app\`, arms `kill.switch`, and initializes the host store. It does not type a password.

## Check in

Command Prompt’s own command is named `verify`. Typing `verify` by itself never starts Titan.

After install, close every Chrome window and the tray icon, then double-click `CHECKIN.cmd` in the unzipped folder (or in `%LOCALAPPDATA%\TitanChief\app\`).

Sign into Eleads in that Chrome window as Bordine, Glenn on 28206 or 28546. Leave Chrome open. Double-click `CHECKIN.cmd` again.

Then:

```bat
"%LOCALAPPDATA%\TitanChief\app\Titan.cmd" status
"%LOCALAPPDATA%\TitanChief\app\Titan.cmd" tick
"%LOCALAPPDATA%\TitanChief\app\Titan.cmd" report
```

`tick` attaches to that same Chrome and does not close it.

`install` / `INSTALL.cmd` is not Verify and not send authorization.
