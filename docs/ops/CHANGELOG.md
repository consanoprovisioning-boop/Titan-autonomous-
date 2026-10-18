# Eleads Valdosta — decision / change log

## 2026-10-18 — Windows COS operator (separate from the Linux cloud agent)

A Linux cloud agent cannot switch OS or start Eleads. The Windows operator is a different agent: `.cursor/agents/windows-titan-cos.md` and `.cursor/skills/windows-titan-cos-host/SKILL.md`. Glenn double-clicks `LAUNCH-COS-LIVE.cmd` on the Windows PC. That starts `agent worker start --verbose` locally. It does not open Eleads and does not type a password. Then CHECKIN / Verify as before.

## 2026-10-18 — Host hardening and more than one Windows PC

Titan on disk is harder to steal, and Glenn can install it on more than Alienware-18.

- Chrome DevTools binds to `127.0.0.1:9222` only. Titan refuses a remote debugging URL.
- Each PC enrolls on INSTALL. `host.json` / `host.secret` / `verify.json` are machine-bound (Windows DPAPI for the current user). Do not copy `%LOCALAPPDATA%\TitanChief` between PCs.
- Work-log customer keys are stored as a short hash, not the visible name.
- Cloud attach accepts any enrolled Windows worker. One live send session. Alienware-18 stays the preferred host when more than one worker is online and the Verify receipt does not name another PC.
- Runbook: `docs/deploy/windows-hosts.md`.

Consent, rooftops 28206/28546, identity Bordine, Glenn, and the filed source packet are unchanged.

## 2026-10-16 — CHECKIN reads every CDP page and frame

CHECKIN was attached to port 9222 and still returned exit 6 because it inspected only the first page and the main frame. A later Eleads tab, or a child frame on that tab, never counted. A blank first tab was sent to the login URL.

CHECKIN now enumerates every CDP page on port 9222, prefers an `eleadcrm.com` page, and reads every frame. Logged in still requires an Eleads URL, no Username+Password form, rooftop 28206 or 28546, and Bordine or Glenn. An open 6220 page is still rejected. The login URL is opened only when no Eleads page exists. Success writes `%LOCALAPPDATA%\TitanChief\verify.json` (`glennVerified`, `VerifiedIdle`, `Bordine, Glenn`, rooftop). The cloud launcher still refuses to open Eleads. Glenn must run the new CHECKIN on the Alienware before Verify is claimed.

## 2026-10-16 — Launch path for the Alienware host and cloud attach

The Cursor COS loop is launchable once Alienware-18 is online and Glenn has completed `CHECKIN.cmd` and Verify.

- Host source: `src/Titan.ChiefOfStaff/`. Win-x64 publish: `scripts/publish-win-x64.sh` (cmd equivalent: `scripts/publish-win-x64.cmd`). `INSTALL.cmd` copies files and arms `kill.switch`. It does not start the host and does not open Eleads.
- Cloud attach: `scripts/launch-eleads-cos-loop.sh`. It reads the worker snapshot, refuses to open Eleads, and records ready or blocked in `/cursor/stores/self/eleads-valdosta-state.json`. It does not invent a numbered timer tick. A login claim is not Verify.
- Live checks covered by `tests/eleads_cos_launch_test.py` and `tests/Titan.ChiefOfStaff.Tests/LaunchGuardTests.cs`: rooftop 6220 rejected, ownership screen 180 days, SMS 9:00–19:00 America/New_York, Appointment Set requires a day and a time, cloud host refuses to open Eleads.

This entry does not change consent, rooftop, identity, or the filed source packet.

The original Customer Traffic and Appointment Growth packet remains filed at `docs/ops/sources/` (`Eleads-operating-instructions.md`, `Eleads_Project_Goal.pdf`, `Eleads_project_Instructions_Goal.txt`). `Eleads.rtf` was not in this repository at the V1.1 start. Do not rewrite those source files to match later authorization.

Governing skill and live operating law: `.cursor/skills/eleads-valdosta-chief/SKILL.md` and `docs/ops/eleads-valdosta.md`. Continual loop for a Cursor agent with deployed sub-agents: `.cursor/skills/running-eleads-cos-loop/SKILL.md`.

## 2026-10-16 — 180-day ownership screen; Cursor COS loop

**Decision (Glenn):** live ownership screen is **180 days**. This replaces Version 1.1’s 14 months and the source packet’s filed 12 months for live work. A recent inquiry still overrides. Model year never proves ownership duration.

**Source packet:** not rewritten. `docs/ops/sources/Eleads-operating-instructions.md`, `Eleads_project_Instructions_Goal.txt`, and the PDF still say 12 months as filed history.

**Loop:** `.cursor/skills/running-eleads-cos-loop/SKILL.md` is the Cursor agent skill for the Eleads chief-of-staff continual loop. One Core deploys a small specialist set. Workflow: TITAN-CHANGE-MULTIAGENT-001 v0.4.0, filed at `docs/ops/sources/TITAN-CHANGE-MULTIAGENT-001-v0.4.0.md`. Titan SDK connector output stays Draft or Blocked and does not cancel V1.1 Eleads sends after a live Glenn-verified session. The 51 roles are a logical catalog. Do not spawn 51 model calls.

The live operator is a Cursor agent. The prior overlay skill that named a different chat product as the operator is removed.

## 2026-10-05 — Version 1.1 standing authorization

**Decision (Glenn Bordine, written):** “include autonomous workflow no approvals required.”

**Effect:** Version 1.1 replaces per-message / per-batch / per-task / per-appointment exact-draft pauses in the governing skill and live operating law. After a live Glenn-verified Eleads session on Alienware-18 (CHECKIN.cmd, then Verify, Chrome CDP 9222), routine eligible email/SMS, CRM notes, task create/reschedule/complete, and customer-confirmed appointment saves execute automatically.

**Preserved (not waived):**

- Consent / DNC / DNT / Stop / opt-out / hard bounce
- Rooftop + user verification (Valdosta Nissan 28206, Valdosta Mitsubishi 28546, Bordine, Glenn; never TKO Autogroup 6220)
- 72-hour other-consultant hold; 96-hour zero-contact for others’ unassigned
- Customer day-and-time commitment required before Appointment Set / reassignment
- No invented customers, stock, sends, appointments, or fabricated completions
- Cloud/Linux host boundary (never open eleadcrm.com from this agent; never start Titan.ChiefOfStaff.exe; never type/store the Eleads password)

**V1.1 operating numbers (Oct 5 record; ownership duration superseded 2026-10-16; source packet left as filed):**

- Ownership screen: **14 months** on this date only. Superseded for live work by **180 days** (see 2026-10-16). Recent inquiry still overrides; model year never proves duration.
- Past-customer review: **48 hours**
- New-lead target while connected: **60s RTF / under 4 min PDF**; record actual timing
- SMS: 9:00 AM–7:00 PM America/New_York only (narrower than PDF)
- Email: any hour after a live verified session
- Appointments: Mon/Wed/Thu/Fri 9:00–6:30; Sat 9:00–5:30; never Sunday; Tuesday only if the customer asks

## 2026-09-30 — Packet alignment (superseded for send/appointment pauses)

Glenn restated the Customer Traffic and Appointment Growth operating packet. The record at that time followed the packet’s 12-month ownership screen and exact-draft send approval, and withdrew standing Qualified send. That send/appointment-pause model was superseded by Version 1.1. The 12-month figure is not live law (see 2026-10-16). The source packet on disk is unchanged.
