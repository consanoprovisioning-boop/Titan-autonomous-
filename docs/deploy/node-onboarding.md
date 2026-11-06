# Cluster node onboarding template

Use this for unnamed PCs. Do not invent hostnames. Enroll the real Windows computer name, then map it to Node3–Node8.

**Alienware-18 is MAIN.** Clone from Alienware-18. Lease host is Alienware-18. Do not make Desktop4 the coordinator.

**Launch is held** until Glenn says all PCs are ready. Clone, copy `dist\titan-chief-win-x64` from Alienware-18, `INSTALL.cmd`, enroll. Do **not** start the worker or CHECKIN-to-send yet.

Cloud does not clone onto the PC. Glenn runs these steps locally. Same path as Desktop3/Desktop4. Chrome CDP **9222** after Glenn releases the hold.

## Target lanes

| Enroll as | Rooftop | Lane | When it may send |
| --- | --- | --- | --- |
| Node3 | 28206 Valdosta Nissan | P2/P3 comb + tailored upgrade | After Nissan P1 is clear on Alienware-18 |
| Desktop4 (Node4, provisional) | 28546 Valdosta Mitsubishi | P2/P3 comb + tailored upgrade | After Mitsubishi P1 is clear on Desktop3. Named PC; enroll `Desktop4` or `Node4`. Final COS tasks wait for Glenn’s per-PC study |
| Node5 | 28206 | P1 overdue **calls** | 8-PC scale-out. Reschedule only; never mark complete without a call |
| Node6 | 28546 | P1 overdue **calls** | 8-PC scale-out |
| Node7 | 28206 and 28546 | SMS in window | 8-PC scale-out. 08:00–21:00 ET only |
| Node8 | 28206 and 28546 | Appointment-commit | 8-PC scale-out. Day and time already in correspondence |

Node3 and Desktop4 (named Node4) are part of today’s 4-PC go-live. Desktop4 clone: `docs/deploy/desktop4.md`. Node5–Node8 wait until those boxes exist, then run `SCALE-OUT-8.cmd` on every PC. See `docs/deploy/cluster-8pc.md`.

## Need on the PC

- .NET 8 desktop runtime
- Google Chrome
- Cursor CLI (`agent`) signed in
- This repo cloned under `%USERPROFILE%` (same clone steps as `docs/deploy/desktop3.md`)

Worker start is held. After Glenn releases the cluster:

```bat
cd /d "%USERPROFILE%"
agent worker start --name "Node5" --verbose
```

Replace the name with the enrolled node id. Directory must not be `C:\Windows\system32`. `START-WORKER.cmd` fail-closes while hold is on.

## Commands (install only)

```bat
cd /d "%USERPROFILE%\Titan-autonomous-"
dist\titan-chief-win-x64\INSTALL.cmd
deploy\cluster-node\ENROLL-NODE.cmd Node5
```

Do **not** run `START-WORKER.cmd` or CHECKIN twice until Glenn says all PCs are ready. Never **6220**.

For Node7 or Node8, either allowed rooftop is fine. Set `TITAN_LEASE_DIR` to the Alienware-18 lease folder (`\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases`) before ticking.

## Comb-database job (P2) — Node3 / Desktop4 (Node4)

1. Confirm P1 is clear for this rooftop (`DailyUpgradeQueue.CanStartOnRooftop`).
2. Open Daily Organizer / database views: equity, abandon, in-market, upgrade.
3. Open each customer. Wait for the full activity feed. No list-view send.
4. Record ownership (180-day screen), permissions, vehicle, VOI, timestamps.
5. Hold others’ leads (72h / 96h). Skip thin profiles.

## Tailored upgrade job (P3)

Only after P2 is complete on this rooftop. Compose from proven profile facts (`TailoredUpgradeEmail`). Never a generic blast. CRM history under Bordine, Glenn is proof.

## Chrome / CDP

Close every Chrome window first. CHECKIN attaches on `127.0.0.1:9222`. Do not expose DevTools on the LAN. One Eleads session per PC.
