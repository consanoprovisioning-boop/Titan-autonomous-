# Alienware-18 — Titan.ChiefOfStaff install-without-launch

Glenn-approved signed-install copy only. **NO launch. NO live Eleads.**

This is the source runbook for the framework-dependent `win-x64` Release publish of `Titan.ChiefOfStaff` (`net8.0`). The Alienware host must already have the .NET 8 runtime (or .NET 8 desktop runtime) installed.

The finished publish is `dist/titan-chief-win-x64/`. Install with `deploy/alienware-local-program/INSTALL.cmd`. An earlier incomplete signed folder is archived at `deploy/archive/incomplete-signed-drop/`. Do not use PowerShell self-extractors. Do not set execution policy to bypass. Do not create Desktop or Start Menu shortcuts.

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

1. Review `deploy/alienware-install-fd/` and `SHA256SUMS.txt` on a trusted machine.
2. Create the app directory:

```bat
mkdir "%LOCALAPPDATA%\TitanChief\app"
```

3. Copy every file from the complete publish folder into the app directory (adjust the source path to the folder you received):

```bat
xcopy /E /I /Y "Z:\path\to\alienware-install-fd\*" "%LOCALAPPDATA%\TitanChief\app\"
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

Expected exe SHA-256: `82c2a986af7473244fdb57be971e7448b2ae14a8f010b04a74402ed96332ee82`

## STOP

- Do **not** start `Titan.ChiefOfStaff.exe`.
- Do **not** run `dotnet`, `dotnet run`, or any host command.
- Do **not** open Eleads.
- Do **not** create Desktop or Start Menu shortcuts from this install-without-launch procedure. Glenn's double-click of `COS.cmd` may place `COS.lnk` on his desktop when he runs it.
- Do **not** use a PowerShell self-extractor.
- Do **not** set execution policy to bypass.
- Do **not** type an Eleads password (Titan never stores it and never types it).
- Reset of a tripped kill switch later still requires Glenn to write `armed` and a new successful Verify. This install step only copies files and arms the switch. It is not Verify and not a send authorization.

## Cloud / Linux agents

This publish is `win-x64`. Do not install it on a Linux cloud VM. Do not open `eleadcrm.com` from a cloud agent to “stand in” for Titan. One live Eleads session belongs to the Alienware Titan host after Glenn Verify, not to Cursor Playwright.

## Local program (this repo)

The finished host source is `src/Titan.ChiefOfStaff/`. The win-x64 publish is `dist/titan-chief-win-x64/`. On the Alienware, double-click `deploy/alienware-local-program/COS.cmd` (see that folder's README). Do not type `verify` by itself — Command Prompt owns that word. Glenn signs into Eleads in that Chrome window. Titan never types the password.

## Memory budget

Glenn stated this Alienware 18 has 32GB of RAM and dual GPUs. He did not name the GPU models, the CPU, or the Windows version. These caps are build settings. This repo change did not read live RAM counters from that PC.

| What | Cap | What it limits |
| --- | --- | --- |
| Titan.ChiefOfStaff GC heap | 2GB (`System.GC.HeapHardLimit` = 2147483648) | Hard ceiling on the .NET GC heap. It is not memory reserved up front. The host keeps one SQLite heartbeat, a short log, and the text of one organizer page, so a larger heap would not make Eleads faster. |
| Server GC | on (`System.GC.Server` = true, concurrent GC left on) | The host is a console `Exe` that runs a verb and exits, not a WinExe with a UI. |
| Chrome JavaScript heap | 4GB per isolate (`--js-flags=--max-old-space-size=4096`) | Old-space ceiling for the Chrome window Titan starts on port 9222. The page, an extension, and an iframe can each be an isolate, so the cap is per isolate. |
| Chrome disk cache | 1GB (`--disk-cache-size=1073741824`) | Disk cache of that Chrome profile. Repeat Eleads loads stay warm. This is not a RAM reservation. |
| Left for the rest of the machine | at least 8GB | Windows, the dual-GPU drivers, the desktop, and the Cursor worker. No Titan cap is placed on this memory. |

No heap or job limit is set above 20GB. The dedicated ceilings are the 2GB .NET heap and the 4GB Chrome JavaScript heap. Together with the 8GB left for the rest of the machine, they stay inside 32GB even if both ceilings are reached. The sandbox stays enabled. No GPU flag is passed.

The next `scripts/publish-win-x64.sh` run writes the GC settings into `Titan.ChiefOfStaff.runtimeconfig.json`. Chrome uses the new flags the next time that build's host starts Chrome. This change does not republish `dist/`.

The Alienware has two GPUs. Titan does not name a GPU, load a driver overclock, or start a CUDA workload. The Chrome window it already starts should keep Windows' normal high-performance graphics preference for that Chrome process.
