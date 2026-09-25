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
- Do **not** create Desktop or Start Menu shortcuts.
- Do **not** use a PowerShell self-extractor.
- Do **not** set execution policy to bypass.
- Do **not** type an Eleads password (Titan never stores it and never types it).
- Reset of a tripped kill switch later still requires Glenn to write `armed` and a new successful Verify. This install step only copies files and arms the switch. It is not Verify and not a send authorization.

## Cloud / Linux agents

This publish is `win-x64`. Do not install it on a Linux cloud VM. Do not open `eleadcrm.com` from a cloud agent to “stand in” for Titan. One live Eleads session belongs to the Alienware Titan host after Glenn Verify, not to Cursor Playwright.

## Local program (this repo)

The finished host source is `src/Titan.ChiefOfStaff/`. The win-x64 publish is `dist/titan-chief-win-x64/`. On the Alienware, use `deploy/alienware-local-program/INSTALL.cmd` (cmd.exe only). Then Glenn signs into Eleads in Chrome and runs `verify`. Titan never types the password.
