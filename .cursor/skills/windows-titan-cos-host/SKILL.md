---
name: windows-titan-cos-host
description: Use when a Cursor agent is running on Glenn’s Windows PC (My Machines worker) to install, CHECKIN, Verify, and tick Titan COS live. Do not use on a Linux cloud VM.
---

# Windows Titan COS host

This skill is for an agent whose tools execute **on Windows**. If `OS` is not Windows, stop. Do not open `eleadcrm.com`. Do not start `Titan.ChiefOfStaff.exe`.

The Linux cloud overseer is a different process. It uses `.cursor/skills/running-eleads-cos-loop/SKILL.md` and never launches the Windows host.

## Launch on this PC

Glenn double-clicks `LAUNCH-COS-LIVE.cmd` from `dist\titan-chief-win-x64\` or `deploy\alienware-local-program\`. That starts `agent worker start --verbose` in a new Command Prompt (not system32). It does not open Eleads and does not type a password.

Then:

1. Close every Chrome window, including the tray.
2. Double-click `CHECKIN.cmd`. Sign in as **Bordine, Glenn** on **28206** or **28546**.
3. Double-click `CHECKIN.cmd` again (Verify). Leave Chrome open on `127.0.0.1:9222`.
4. `TICK.cmd` attaches to that Chrome.

`INSTALL.cmd` and `kill.switch=armed` are not Verify.

## After Verify

Version 1.1 live law: `.cursor/skills/eleads-valdosta-chief/SKILL.md`. Rooftops 28206/28546 only. Never 6220.
