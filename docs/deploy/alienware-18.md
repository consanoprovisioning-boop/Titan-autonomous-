# Alienware-18 — Titan COS local overlay

This is the source runbook for the self-contained `win-x64` Release publish of Titan COS (`net8.0`). Windows 10 and Windows 11. No extra .NET install.

The launch publish is `dist/titan-chief-win-x64/`, produced by `scripts/publish-win-x64.sh` or `scripts/publish-win-x64.cmd`. **On the PC, double-click `TitanCOS.exe`.** That one click self-installs if needed, opens the overlay, starts Chrome, and puts COS to work once Bordine, Glenn is signed in. Titan never types the password.

`INSTALL.cmd` still copies files and arms `kill.switch` without starting COS if you want the files in place first. It does not open Eleads. An earlier incomplete signed folder is archived at `deploy/archive/incomplete-signed-drop/`. Do not use PowerShell self-extractors. Do not set execution policy to bypass.

## Destinations

| Role | Path |
| --- | --- |
| Binary / app files | `%LOCALAPPDATA%\TitanChief\app\` |
| Data root (stores, heartbeat, locks, overlay, learning) | `%LOCALAPPDATA%\TitanChief\` |
| Kill switch | `%LOCALAPPDATA%\TitanChief\kill.switch` |
| Browser profile (Titan COS Chrome) | `%LOCALAPPDATA%\TitanChief\browser` |

Do not use `ProgramData`. Do not read or write any prior agent data tree under `%LOCALAPPDATA%`.

## Copy steps (optional)

`TitanCOS.exe` self-installs on first double-click. If you want a manual copy first, do this in `cmd.exe`:

1. Review `dist/titan-chief-win-x64/` and `SHA256SUMS.txt` on a trusted machine. The incomplete archive under `deploy/archive/incomplete-signed-drop/` is not the launch build. Its old exe hash `82c2a986af7473244fdb57be971e7448b2ae14a8f010b04a74402ed96332ee82` is history only.
2. Double-click `INSTALL.cmd` inside the publish folder, or:

```bat
mkdir "%LOCALAPPDATA%\TitanChief\app"
xcopy /E /I /Y "Z:\path\to\dist\titan-chief-win-x64\*" "%LOCALAPPDATA%\TitanChief\app\"
echo armed>"%LOCALAPPDATA%\TitanChief\kill.switch"
certutil -hashfile "%LOCALAPPDATA%\TitanChief\app\TitanCOS.exe" SHA256
```

Hash `TitanCOS.exe` against `SHA256SUMS.txt` after each publish.

## Cloud / Linux agents

This publish is `win-x64`. Do not install it on a Linux cloud VM. Do not open `eleadcrm.com` from a cloud agent to stand in for Titan. Do not start `TitanCOS.exe` or `Titan.ChiefOfStaff.exe` from this cloud host. One live Eleads session belongs to the Alienware Titan host after Glenn Verify.

## Local program (this repo)

The host source is `src/Titan.ChiefOfStaff/` plus `src/TitanCOS/`. The win-x64 publish is `dist/titan-chief-win-x64/`.

## Launch sequence

Alienware-18, Glenn:

1. Double-click `TitanCOS.exe` (from the published folder, or from `%LOCALAPPDATA%\TitanChief\app\` after the first run).
2. If Eleads asks, sign in as Bordine, Glenn on 28206 or 28546. Titan never types the password.
3. Leave the overlay and that Chrome window up. COS works both rooftops in a continuous local loop.

`COS.cmd` is the same one-click start. `CHECKIN.cmd` remains as the older attach path. Do not type `verify` by itself. Do not type `Titan.cmd` from `C:\Windows\system32`.

Cloud Cursor agent:

```bash
scripts/launch-eleads-cos-loop.sh --workers-file /cursor/stores/self/worker-snapshot.json
```

Refresh `worker-snapshot.json` from `list-self-hosted-workers` `scope=all` `status=all` immediately before that command. The script does not open Eleads and does not start the Windows exe. Exit 3 records a blocked cycle. Exit 0 means the worker is online and Verify is on file.
