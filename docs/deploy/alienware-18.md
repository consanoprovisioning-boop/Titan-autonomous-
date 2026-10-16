# Alienware-18 — Titan.ChiefOfStaff install-without-launch

Glenn-approved signed-install copy only. **NO launch. NO live Eleads.**

This is the source runbook for the framework-dependent `win-x64` Release publish of `Titan.ChiefOfStaff` (`net8.0`). The Alienware host must already have the .NET 8 runtime (or .NET 8 desktop runtime) installed.

The launch publish is already in `dist/titan-chief-win-x64/` on this branch (copied from `cursor/eleads-cos-launch-7dfe` at `1aeab5a`). Install with `deploy/alienware-local-program/INSTALL.cmd` (also copied into the publish folder). `INSTALL.cmd` does not start `Titan.ChiefOfStaff.exe` and does not open Eleads. Do not rebuild on the Alienware for this install. Do not use PowerShell self-extractors. Do not set execution policy to bypass. Do not create Desktop or Start Menu shortcuts.

## Destinations

| Role | Path |
| --- | --- |
| Binary / app files | `%LOCALAPPDATA%\TitanChief\app\` |
| Data root (stores, heartbeat, locks) | `%LOCALAPPDATA%\TitanChief\` |
| Kill switch | `%LOCALAPPDATA%\TitanChief\kill.switch` |
| Browser profile (do not create or use now) | `%LOCALAPPDATA%\TitanChief\browser` |

Do not use `ProgramData`. Do not read or write any prior agent data tree under `%LOCALAPPDATA%`.

## Copy steps (Windows Command Prompt)

Do this in `cmd.exe`. Do not use a script that launches the host.

1. Review `dist/titan-chief-win-x64/` and `SHA256SUMS.txt` on a trusted machine. The old incomplete-drop exe hash `82c2a986af7473244fdb57be971e7448b2ae14a8f010b04a74402ed96332ee82` is history only and is not this publish.
2. Create the app directory:

```bat
mkdir "%LOCALAPPDATA%\TitanChief\app"
```

3. Copy every file from the complete publish folder into the app directory (adjust the source path to the folder you received):

```bat
xcopy /E /I /Y "Z:\path\to\dist\titan-chief-win-x64\*" "%LOCALAPPDATA%\TitanChief\app\"
```

4. Arm the kill switch with exactly the payload `armed` (creates the data root if needed):

```bat
mkdir "%LOCALAPPDATA%\TitanChief"
echo armed>"%LOCALAPPDATA%\TitanChief\kill.switch"
```

5. Confirm the kill file is `armed` only:

```bat
type "%LOCALAPPDATA%\TitanChief\kill.switch"
```

Expected: a line that is `armed`.

6. Optional: verify `Titan.ChiefOfStaff.exe` SHA-256 against `SHA256SUMS.txt` (CertUtil, no start):

```bat
certutil -hashfile "%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe" SHA256
```

Expected launch exe SHA-256: `e346cebc255d7f283f759b3f18450bd5ef9bc010040b38470da25fee22a408a1`

That value is the first line of `dist/titan-chief-win-x64/SHA256SUMS.txt`. Do not compare the launch exe to the archived incomplete-drop hash.

## STOP

- Do **not** start `Titan.ChiefOfStaff.exe`.
- Do **not** run `dotnet`, `dotnet run`, or any host command.
- Do **not** open Eleads.
- Do **not** create Desktop or Start Menu shortcuts.
- Do **not** use a PowerShell self-extractor.
- Do **not** set execution policy to bypass.
- Do **not** type an Eleads password (Titan never stores it and never types it).
- Reset of a tripped kill switch later still requires Glenn to write `armed` and a new successful Verify. This install step only copies files and arms the switch. It is not Verify and not a send authorization.

## Cloud / Linux agents

This publish is `win-x64`. Do not install it on a Linux cloud VM. Do not open `eleadcrm.com` from a cloud agent to “stand in” for Titan. One live Eleads session belongs to the Alienware Titan host after Glenn Verify, not to Cursor Playwright.

## Local program (this repo)

The win-x64 publish is `dist/titan-chief-win-x64/`. Host source stays on `cursor/eleads-cos-launch-7dfe`. This branch does not start the host.

## Launch sequence

Alienware-18, Glenn. Use a new Command Prompt. Do not type these commands in the Cursor worker window. That window's directory is `C:\Windows\system32`. Do not run `C:\Windows\system32\Titan.cmd`.

1. Confirm `CHECKIN.cmd` is on disk before any double-click: `dir "%LOCALAPPDATA%\TitanChief\app\CHECKIN.cmd"` after install, or `dir dist\titan-chief-win-x64\CHECKIN.cmd` in the downloaded tree. If Command Prompt says it cannot find the file, stop.
2. If the app is not installed, run `INSTALL.cmd` once (from `dist\titan-chief-win-x64\`, or `deploy\alienware-local-program\INSTALL.cmd` with that folder as the argument). It copies files and writes `kill.switch` as `armed`. It does not start the host.
3. Close every Chrome window and the tray icon.
4. Double-click `%LOCALAPPDATA%\TitanChief\app\CHECKIN.cmd`.
5. Sign into Eleads in that Chrome window as Bordine, Glenn on 28206 or 28546. Titan never types the password.
6. Double-click `CHECKIN.cmd` again. That second run is Verify. Do not type `verify` by itself — Command Prompt owns that word.
7. Leave Chrome open on CDP 9222.

Do not place another agent on the Alienware worker. Do not run the cloud loop from this branch.
