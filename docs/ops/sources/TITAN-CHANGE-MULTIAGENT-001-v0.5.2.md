# TITAN-CHANGE-MULTIAGENT-001 v0.5.2

Filed overlay naming **Desktop4** as the Node4 cluster PC. Document ID stays **TITAN-CHANGE-MULTIAGENT-001**. This file does not rewrite v0.5.1 (`TITAN-CHANGE-MULTIAGENT-001-v0.5.1.md`), v0.5.0, v0.4.0, or the Eleads source packet.

---

CONTROLLED CHANGE RECORD
Titan Autonomous + Titan Chief of Staff — Desktop4 named Node4
Document ID: TITAN-CHANGE-MULTIAGENT-001
Version: 0.5.2
Effective date: November 6, 2026
Product owner: Glenn Bordine / Bordine Systems — Project Titan
Source authority: Glenn statement that PC # DeskTop4 of the new PC cluster installs Titan Clone from Alienware-18
Status: Desktop4 slotted into the live 4-PC map as named Node4. 8-PC scale-out unchanged. Physical clone and Verify remain Glenn-local. Cloud Linux never opens Eleads.

## 1. Outcome

Desktop4 is a first-class named node, same pattern as Desktop3. It occupies the existing PC4 / Node4 lane:

- Rooftop **Valdosta Mitsubishi 28546**
- Lane **P2/P3** — comb the Mitsubishi database, then tailored upgrades after Mitsubishi P1 on Desktop3 is clear
- Enroll `Desktop4` or `Node4` — both write `nodeId=Desktop4` and map to that lane
- Preferred install: local copy from Alienware-18 (`%USERPROFILE%\Titan-autonomous-` via UNC git clone or robocopy), then `git checkout cursor/eleads-cluster-4pc-7dfe`
- Fallback: `git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git` and checkout that cluster branch

Alienware-18 stays Nissan P1. Desktop3 stays Mitsubishi P1. Node3 stays Nissan P2/P3. Nodes 5–8 stay overflow.

Cloud writes the playbook. Glenn runs it on Desktop4. This record does not claim Desktop4 is online.

## 2. Hard constraints (unchanged)

- Rooftops **28206** and **28546** only. Never **6220**.
- Identity **Bordine, Glenn**.
- SMS **08:00–21:00** America/New_York.
- Ownership screen **180 days**.
- Cloud Linux never opens Eleads, never runs `Titan.ChiefOfStaff.exe`, never types the password, never fabricates workers or sends.
- Workers start from `%USERPROFILE%`. Never `C:\Windows\system32`.
- CHECKIN.cmd, not `verify`. Chrome CDP **9222**.
- P1 before P2/P3 on each rooftop.
- Offline workers: `sends=[]`.

## 3. 4-PC map after this overlay

| PC | Node | Rooftop | Lane |
| --- | --- | --- | --- |
| PC1 | Alienware-18 | 28206 | P1 organizer |
| PC2 | Desktop3 | 28546 | P1 organizer |
| PC3 | Node3 | 28206 | P2/P3 |
| PC4 | Desktop4 (Node4) | 28546 | P2/P3 |

The 8-PC scale-out in v0.5.1 is unchanged: PC1/PC2 still narrow to email when `SCALE-OUT-8.cmd` is armed. Desktop4 stays P2/P3.

## 4. Playbook

Glenn-local: `docs/deploy/desktop4.md`, `deploy/desktop4/START-HERE.txt`, `deploy/desktop4/START-WORKER.cmd`, `deploy/desktop4/ENROLL.cmd`. Do not copy `%LOCALAPPDATA%\TitanChief` from Alienware-18.
