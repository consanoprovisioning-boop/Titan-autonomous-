# Alienware-18 install-without-launch (framework-dependent, slim)

Glenn-approved signed-install copy only. **NO launch. NO live Eleads.**

Source runbook: `docs/deploy/alienware-18.md`

This folder is a **framework-dependent** `win-x64` Release publish of `Titan.ChiefOfStaff` (`net8.0`). The Alienware host must already have the .NET 8 runtime (or .NET 8 desktop runtime) installed. The `.playwright` directory and all `*.ps1` files were omitted on purpose for install-without-launch. Playwright browser drivers are not required to copy files or arm the kill switch. It is not a PowerShell self-extractor. Do not set execution policy to bypass. Do not create Desktop or Start Menu shortcuts.

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

1. Review this folder and `SHA256SUMS.txt` on a trusted machine.
2. Create the app directory:

```bat
mkdir "%LOCALAPPDATA%\TitanChief\app"
```

3. Copy every file from this publish folder into the app directory (adjust `Z:\path\to\alienware-install-fd` to the folder you received):

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

## STOP

- Do **not** start `Titan.ChiefOfStaff.exe`.
- Do **not** run `dotnet`, `dotnet run`, or any host command.
- Do **not** open Eleads.
- Do **not** create Desktop or Start Menu shortcuts.
- Do **not** use a PowerShell self-extractor.
- Do **not** set execution policy to bypass.
- Do **not** type an Eleads password (Titan never stores it and never types it).
- Reset of a tripped kill switch later still requires Glenn to write `armed` and a new successful Verify. This install step only copies files and arms the switch. It is not Verify and not a send authorization.
