# Titan Autonomous Chief of Staff

Live Alienware 18 local program for Glenn Bordine’s Eleads work at **Valdosta Nissan (28206)** and **Valdosta Mitsubishi (28546)**. Never **The TKO Autogroup (6220)**.

Titan never stores or types the Eleads password.

## Alienware download and install

See `deploy/alienware-local-program/README.md`. Publish output: `dist/titan-chief-win-x64/`.

```bat
INSTALL.cmd C:\path\to\dist\titan-chief-win-x64
"%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe" status
```

`install` / `INSTALL.cmd` only copies files and arms `kill.switch`. That is not Glenn Verify and not send authorization. Glenn signs into Eleads in Chrome, then runs `verify`, then `tick`.

## Law

`docs/ops/eleads-valdosta.md`  
Skill: `.cursor/skills/eleads-valdosta-chief/SKILL.md`
