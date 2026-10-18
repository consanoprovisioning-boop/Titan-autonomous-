# Titan Autonomous on Alienware 18

Finished local suite. Chief of Staff is the host. Rooftops **28206** and **28546** only. Never **6220**.

## One double-click

Double-click `COS.cmd`. The first run on the Alienware also puts `COS.lnk` on the desktop. Pin that shortcut.

That click:

- If `%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe` is missing and the suite exe is in the same folder, runs `INSTALL.cmd` once. It does not reinstall over an app folder that already has the exe. If the exe is missing and this file is not beside the suite, it tells you once to run `INSTALL.cmd`.
- Starts `cursor worker start` only when that worker is not already running, and leaves it running.
- Runs `CHECKIN.cmd` so the host and Chrome CDP 9222 come up the same way. It does not start a second host. A second double-click does not kill Chrome, does not kill a healthy worker, and does not start another Chief of Staff when that live session is already up.
- If Eleads asks for a password and 1Password is installed, opens the 1Password prompt for you to approve and fill. If 1Password is absent, the Eleads password box stays empty for you to fill.

Needs the .NET 8 desktop runtime, Google Chrome, and `cursor` on PATH for the worker.

`INSTALL.cmd` and `kill.switch=armed` are not check-in and not send authorization. Do not type `verify` by itself. Command Prompt owns that word. Do not run `C:\Windows\system32\Titan.cmd`.

`tick` attaches to that same Chrome and does not close it.

## 1Password, one time

Sign in to the 1Password app yourself, or run `op signin` in a window you open. COS does not run that for you, does not read the Eleads password, and does not create a vault item. The password is not in this repo.
