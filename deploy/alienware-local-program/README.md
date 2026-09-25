# Alienware 18 — Titan.ChiefOfStaff local program

This is the live local host for Glenn Bordine’s Eleads work at Valdosta Nissan (28206) and Valdosta Mitsubishi (28546). Never The TKO Autogroup (6220).

## Download on the Alienware

1. Open the `cursor/titan-chief-host-7954` branch on GitHub (or the pull request for that branch).
2. Download the repo ZIP (**Code → Download ZIP**) or clone the branch.
3. Copy `dist/titan-chief-win-x64/` onto the Alienware. That folder already contains `Titan.ChiefOfStaff.exe` and `INSTALL.cmd`.

This cloud environment cannot copy files onto the Alienware. There is no connected self-hosted worker. You download, then run `INSTALL.cmd` on that PC.

## Install (cmd.exe)

Alienware needs the **.NET 8 desktop runtime** for a framework-dependent publish: https://dotnet.microsoft.com/download/dotnet/8.0  
Google Chrome must already be installed.

```bat
cd /d %USERPROFILE%\Downloads\Titan-autonomous-\deploy\alienware-local-program
INSTALL.cmd C:\path\to\dist\titan-chief-win-x64
```

`INSTALL.cmd` copies into `%LOCALAPPDATA%\TitanChief\app\` and writes `armed` to `%LOCALAPPDATA%\TitanChief\kill.switch`. It does **not** start Eleads and does **not** create shortcuts.

## Run

```bat
"%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe" status
"%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe" verify
"%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe" tick
```

`verify` opens the persistent Chrome profile. **Glenn types the Eleads password in Chrome.** Titan never stores it and never types it.

`tick` runs only after verify succeeds on rooftop 28206 or 28546. SMS 9:00–7:00 PM ET; email any hour.

## Build this publish (from a machine with .NET 8 SDK)

```bat
dotnet test Titan.ChiefOfStaff.sln -c Release
dotnet publish src\Titan.ChiefOfStaff\Titan.ChiefOfStaff.csproj -c Release -r win-x64 --self-contained false -o dist\titan-chief-win-x64
```
