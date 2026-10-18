# Titan.ChiefOfStaff on Glenn’s Windows PCs

Install Titan on **each Windows PC** Glenn uses. Alienware-18 is one of those PCs, not the only one.

One **live** Eleads session at a time. Other PCs can stay installed. Do not Verify two PCs at once. Chrome debugging stays on `127.0.0.1:9222` and does not listen on the LAN.

Titan never stores or types the Eleads password. `INSTALL.cmd` and `kill.switch=armed` are not Verify and not send authorization.

## Need on each PC

- .NET 8 desktop runtime
- Google Chrome
- Cursor worker left running: `agent worker start --verbose`

Do not pass `--name`. Do not type `Titan.cmd` from `C:\Windows\system32`.

## Install (each PC)

Build with `scripts/publish-win-x64.sh` or, on a Windows PC, `scripts\publish-win-x64.cmd`. Then double-click `INSTALL.cmd` inside `dist\titan-chief-win-x64\`, or:

```bat
deploy\alienware-local-program\INSTALL.cmd C:\path\to\dist\titan-chief-win-x64
```

That copies into `%LOCALAPPDATA%\TitanChief\app\`, enrolls **this** PC (`host.json` is machine-bound), and arms `kill.switch`. It does not start the host and does not open Eleads.

Do not copy `%LOCALAPPDATA%\TitanChief` from one PC to another. Run INSTALL on the PC that will attach to Chrome.

## Check in (the PC that will send)

1. Leave the Cursor worker running on that PC.
2. Close every Chrome window and the tray icon.
3. Double-click `CHECKIN.cmd` next to `Titan.ChiefOfStaff.exe`.
4. Sign into Eleads in that Chrome window as Bordine, Glenn on 28206 or 28546.
5. Double-click `CHECKIN.cmd` again. That second run is Verify.
6. Leave that Chrome window open. DevTools is loopback-only (`127.0.0.1:9222`).

If another PC is already the live session, run `Titan.ChiefOfStaff.exe stop` there first, then Verify here.

## Cloud attach

The Linux cloud agent still refuses to open Eleads. It accepts **any enrolled Windows worker**, prefers Alienware-18 when more than one is online and the Verify receipt does not name another PC, and records a single live worker.

```bash
scripts/launch-eleads-cos-loop.sh --workers-file /cursor/stores/self/worker-snapshot.json
```

Refresh `worker-snapshot.json` from `list-self-hosted-workers` `scope=all` `status=all` immediately before that command.

Alienware-18 detail: `docs/deploy/alienware-18.md`.
