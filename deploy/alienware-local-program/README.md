# Titan Autonomous on Alienware 18

Finished local suite. Chief of Staff is the host. Rooftops **28206** and **28546** only. Never **6220**.

## One double-click

Double-click `COS.cmd`. The first run on the Alienware also puts `COS.lnk` on the desktop. Pin that shortcut.

That click:

- If `%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe` is missing and the suite exe is in the same folder, runs `INSTALL.cmd` once. It does not reinstall over an app folder that already has the exe. If the exe is missing and this file is not beside the suite, it tells you once to run `INSTALL.cmd`.
- Starts exactly one `cursor worker start` when none is running, waits up to 30 seconds until that process is up, then continues check-in. A worker that is already running is left alone. A second double-click does not start another worker and does not kill a healthy worker.
- Runs `CHECKIN.cmd` so the host and Chrome CDP 9222 come up the same way. It does not start a second host. A second double-click while the session is live leaves that Chrome and that worker alone, and does not start another Chief of Staff.
- If the Eleads tab is missing or on the login page, focuses Titan Chrome and sends Ctrl+Backslash once. That is 1Password Fill in Browser. The Chrome extension fills the login in that profile. If 1Password is locked, the unlock box stays and the session is not live. If 1Password is absent, the password box stays empty.

Needs the .NET 8 desktop runtime, Google Chrome, and `cursor` on PATH for the worker.

`INSTALL.cmd` and `kill.switch=armed` are not check-in and not send authorization. Do not type `verify` by itself. Command Prompt owns that word. Do not run `C:\Windows\system32\Titan.cmd`.

`tick` attaches to that same Chrome and does not close it.

## 1Password

Sign in to the 1Password app yourself once. COS does not call `op`, does not read the Eleads password, and does not create a vault item. Unattended fill works only when 1Password is already unlocked. The extension id `aeblfdkhhhdcdjpifhhbdiojplfjncoa` is registered for Chrome, including the Titan profile, when 1Password is installed.
