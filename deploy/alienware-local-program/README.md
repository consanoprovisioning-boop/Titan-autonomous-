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

## Run

```bat
"%LOCALAPPDATA%\TitanChief\app\Titan.cmd" status
"%LOCALAPPDATA%\TitanChief\app\Titan.cmd" verify
"%LOCALAPPDATA%\TitanChief\app\Titan.cmd" tick
"%LOCALAPPDATA%\TitanChief\app\Titan.cmd" report
```

`verify` starts Chrome with CDP port 9222 if needed. **You** sign in as Bordine, Glenn. Leave Chrome open. `tick` attaches to that same Chrome and does not close it.

`install` / `INSTALL.cmd` is not Verify and not send authorization.
