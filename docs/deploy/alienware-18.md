# Alienware-18 — Titan.ChiefOfStaff install-without-launch

Glenn-approved signed-install copy only. **NO launch. NO live Eleads.**

This is the source runbook for the framework-dependent `win-x64` Release publish of `Titan.ChiefOfStaff` (`net8.0`). The Alienware host must already have the .NET 8 runtime (or .NET 8 desktop runtime) installed.

The launch publish is `dist/titan-chief-win-x64/`, produced by `scripts/publish-win-x64.sh` on a build machine or `scripts/publish-win-x64.cmd` on the Alienware. Install with `deploy/alienware-local-program/INSTALL.cmd` (also copied into the publish folder). `INSTALL.cmd` does not start `Titan.ChiefOfStaff.exe` and does not open Eleads. An earlier incomplete signed folder is archived at `deploy/archive/incomplete-signed-drop/`. Do not use PowerShell self-extractors. Do not set execution policy to bypass. Do not create Desktop or Start Menu shortcuts.

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

1. Review `dist/titan-chief-win-x64/` and `SHA256SUMS.txt` on a trusted machine. The incomplete archive under `deploy/archive/incomplete-signed-drop/` is not the launch build. Its old exe hash `82c2a986af7473244fdb57be971e7448b2ae14a8f010b04a74402ed96332ee82` is history only.
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

Expected launch exe SHA-256: see the first line of `SHA256SUMS.txt` in the publish folder (regenerated on every publish; the 2026-10-22 local-loop publish is `7fe60a75ad588d28802a699e91716b5a081785ddf7434e0ae9f1c239967060f5`)

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

The host source is `src/Titan.ChiefOfStaff/`. The win-x64 publish is `dist/titan-chief-win-x64/`.

## Launch sequence

Alienware-18, Glenn:

1. Leave `agent worker start --verbose` running. Worker id `1a1f1f15-72c4-4334-ad8b-f87d54efe73a`. Do not pass `--name`. The Windows `cursor` command does not start this worker.
2. If the app is not installed, double-click `INSTALL.cmd` once (from `dist\titan-chief-win-x64\`, or `deploy\alienware-local-program\INSTALL.cmd` with that folder as the argument).
3. Double-click `CREATE-HOME-SCREEN-APP.cmd` once. Glenn (2026-10-22) asked for Titan COS launchable from an app on the home screen, so this one Desktop app (`Titan COS`) supersedes the install-without-launch no-shortcut note. `INSTALL.cmd` itself still creates no shortcuts.
4. Close every Chrome window and the tray icon.
5. Double-click the `Titan COS` app on the Desktop. Click **1 Check In / Verify** (same as `CHECKIN.cmd`).
6. Sign into Eleads in that Chrome window as Bordine, Glenn on 28206 or 28546. Titan never types the password.
7. Click **1 Check In / Verify** again. That second run is Verify. Do not type `verify` by itself — Command Prompt owns that word. Status must read VERIFIED.
8. Click **2 Start Live CRM Loop** (same as `Titan.ChiefOfStaff.exe loop`, or `LOOP.cmd`). The loop repeats ticks locally at 15/30/45-minute pace by ET window, stops on Stop/Ctrl+C or lost Verify, and leaves Chrome open on CDP 9222.

Cloud Cursor agent:

```bash
scripts/launch-eleads-cos-loop.sh --workers-file /cursor/stores/self/worker-snapshot.json
```

Refresh `worker-snapshot.json` from `list-self-hosted-workers` `scope=all` `status=all` immediately before that command. The script does not open Eleads. Exit 3 records a blocked cycle. Exit 0 means the worker is online and Verify is on file.
