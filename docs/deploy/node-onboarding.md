# Cluster node onboarding template

Use this for the two additional unnamed PCs (PC3 and PC4). Do not invent hostnames. Enroll the real Windows computer name, then map it to Node3 or Node4.

Cloud does not clone onto the PC. Glenn runs these steps locally.

## Target lanes

| Enroll as | Rooftop | Lane | When it may send |
| --- | --- | --- | --- |
| Node3 | 28206 Valdosta Nissan | P2/P3 comb + tailored upgrade | After Nissan P1 is clear on Alienware-18 |
| Node4 | 28546 Valdosta Mitsubishi | P2/P3 comb + tailored upgrade | After Mitsubishi P1 is clear on Desktop3 |

## Need on the PC

- .NET 8 desktop runtime
- Google Chrome
- Cursor CLI (`agent`) signed in
- This repo cloned under `%USERPROFILE%` (same clone steps as `docs/deploy/desktop3.md`)

Worker start is always:

```bat
cd /d "%USERPROFILE%"
agent worker start --name "Node3" --verbose
```

Replace `Node3` with `Node4` on the Mitsubishi P2/P3 PC. Directory must not be `C:\Windows\system32`.

## Commands

```bat
cd /d "%USERPROFILE%\Titan-autonomous-"
scripts\publish-win-x64.cmd
dist\titan-chief-win-x64\INSTALL.cmd
deploy\cluster-node\ENROLL-NODE.cmd Node3
deploy\cluster-node\START-WORKER.cmd
```

Then CHECKIN twice as Bordine, Glenn on **that node’s rooftop**, leave Chrome on CDP 9222.

For Node4, enroll `Node4` and sign into **28546**.

## Comb-database job (P2)

1. Confirm P1 is clear for this rooftop (`DailyUpgradeQueue.CanStartOnRooftop`).
2. Open Daily Organizer / database views: equity, abandon, in-market, upgrade.
3. Open each customer. Wait for the full activity feed. No list-view send.
4. Record ownership (180-day screen), permissions, vehicle, VOI, timestamps.
5. Hold others’ leads (72h / 96h). Skip thin profiles.

## Tailored upgrade job (P3)

Only after P2 is complete on this rooftop. Compose from proven profile facts (`TailoredUpgradeEmail`). Never a generic blast. CRM history under Bordine, Glenn is proof.

## Chrome / CDP

Close every Chrome window first. CHECKIN attaches on `127.0.0.1:9222`. Do not expose DevTools on the LAN. One Eleads session per PC.
