#!/usr/bin/env python3
"""Cloud attach for the Eleads COS loop.

Refuses to open Eleads. Does not start the Windows host.
Does not invent a numbered timer tick. A login claim is not Verify.
"""

from __future__ import annotations

import argparse
import json
import socket
from datetime import datetime, timedelta, timezone
from pathlib import Path
from zoneinfo import ZoneInfo

ALIENWARE_WORKER_ID = "1a1f1f15-72c4-4334-ad8b-f87d54efe73a"
RETIRED_ALIENWARE_WORKER_IDS = frozenset({"2bd89263-dc03-4936-9b04-6bc709799f66"})
OWNERSHIP_SCREEN_DAYS = 180
NY = ZoneInfo("America/New_York")
IDENTITY = "Bordine, Glenn"
ALLOWED_ROOFTOPS = frozenset({"28206", "28546"})
FORBIDDEN_ROOFTOP = "6220"
SPECIALISTS = (
    "queue",
    "profile",
    "drafting",
    "compliance",
    "appointment",
    "crm-proof",
    "audit",
)
DEFAULT_STATE = Path("/cursor/stores/self/eleads-valdosta-state.json")
DEFAULT_TICKLOG = Path("/cursor/stores/self/TICKLOG.md")
ELEADS_SESSION_NONE = "none-and-must-not-open-from-cloud"


class EleadsOpenRefused(RuntimeError):
    """This cloud host will not open Eleads."""


def open_eleads(*_args, **_kwargs) -> None:
    raise EleadsOpenRefused("Cloud host refuses to open Eleads.")


def rooftop_allowed(rooftop_id: str | None) -> bool:
    if rooftop_id is None:
        return False
    cleaned = rooftop_id.strip()
    if not cleaned or cleaned == FORBIDDEN_ROOFTOP or FORBIDDEN_ROOFTOP in cleaned:
        return False
    if "tko autogroup" in cleaned.casefold():
        return False
    return cleaned in ALLOWED_ROOFTOPS


def passes_ownership_screen(
    now: datetime,
    purchased_at: datetime | None,
    recent_inquiry: bool,
    model_year: int | None,
) -> bool:
    del model_year
    if recent_inquiry:
        return True
    if purchased_at is None:
        return False
    return (now - purchased_at) <= timedelta(days=OWNERSHIP_SCREEN_DAYS)


def sms_open(instant: datetime) -> bool:
    local = instant.astimezone(NY)
    minutes = local.hour * 60 + local.minute
    return 8 * 60 <= minutes < 21 * 60


def appointment_set_allowed(correspondence: str | None) -> bool:
    import re

    if correspondence is None or not correspondence.strip():
        return False
    has_day = re.search(
        r"\b(?:monday|tuesday|wednesday|thursday|friday|saturday|sunday|"
        r"mon|tue|tues|wed|thu|thur|thurs|fri|sat|sun)\b|"
        r"\b\d{1,2}/\d{1,2}(?:/\d{2,4})?\b|"
        r"\b(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\s+\d{1,2}\b",
        correspondence,
        re.IGNORECASE,
    )
    has_time = re.search(
        r"\b(?:[01]?\d|2[0-3]):[0-5]\d\b|"
        r"\b(?:1[0-2]|0?[1-9])(?::[0-5]\d)?\s*(?:a\.?m\.?|p\.?m\.?)\b|"
        r"\bat\s+(?:1[0-2]|0?[1-9])(?::[0-5]\d)?\b",
        correspondence,
        re.IGNORECASE,
    )
    return has_day is not None and has_time is not None


def alienware_worker(snapshot: dict) -> dict | None:
    workers = snapshot.get("workers") or []
    if not isinstance(workers, list):
        return None
    for row in workers:
        if not isinstance(row, dict):
            continue
        worker_id = str(row.get("workerId") or "")
        if worker_id == ALIENWARE_WORKER_ID or worker_id in RETIRED_ALIENWARE_WORKER_IDS:
            return row
        labels = " ".join(
            str(row.get(key) or "")
            for key in ("machineDisplayName", "displayName", "hostname")
        )
        if labels.startswith("Alienware-18") or "Alienware-18" in labels:
            return row
    return None


def worker_is_online(snapshot: dict) -> bool:
    return alienware_worker(snapshot) is not None


def worker_directory_is_system32(row: dict | None) -> bool:
    if not isinstance(row, dict):
        return False
    blob = " ".join(
        str(row.get(key) or "")
        for key in ("displayName", "directory", "workspacePath", "machineDisplayName")
    ).casefold()
    return "system32" in blob


def verify_receipt_ok(receipt: dict | None) -> bool:
    if not isinstance(receipt, dict):
        return False
    if receipt.get("loginClaim") and receipt.get("glennVerified") is not True:
        return False
    if receipt.get("glennVerified") is not True:
        return False
    if receipt.get("state") != "VerifiedIdle":
        return False
    if receipt.get("identity") != IDENTITY:
        return False
    return rooftop_allowed(receipt.get("rooftop") if isinstance(receipt.get("rooftop"), str) else None)


def load_json(path: Path | None) -> dict | None:
    if path is None or not path.is_file():
        return None
    return json.loads(path.read_text(encoding="utf-8"))


def specialist_results() -> list[dict]:
    return [
        {
            "specialist": name,
            "status": "not-run",
            "rooftop": "none",
            "items": [],
            "blocks": [],
            "evidence": [],
        }
        for name in SPECIALISTS
    ]


def build_state(snapshot: dict, receipt: dict | None, now: datetime) -> dict:
    row = alienware_worker(snapshot)
    online = row is not None
    seen_id = str(row.get("workerId") or ALIENWARE_WORKER_ID) if row else ALIENWARE_WORKER_ID
    system32 = worker_directory_is_system32(row)
    verified = online and verify_receipt_ok(receipt) and not system32
    workers = snapshot.get("workers") or []
    worker_count = len(workers) if isinstance(workers, list) else 0
    if verified:
        blockers: list[str] = []
        host = "alienware-verified"
        mode = "v1.1-execute"
        session = "alienware-cdp-9222"
    elif online and system32:
        blockers = [
            f"Alienware-18 worker {seen_id} is online in C:\\Windows\\system32. "
            "No agent will be placed there. Verify is not complete. "
            "Glenn must run CHECKIN.cmd from %LOCALAPPDATA%\\TitanChief\\app, "
            "sign in as Bordine, Glenn, run CHECKIN.cmd again, and leave Chrome open. "
            "This cloud host will not open Eleads."
        ]
        host = "cloud-linux"
        mode = "draft-blocked-local"
        session = ELEADS_SESSION_NONE
    elif online:
        blockers = [
            "Alienware-18 worker is online. Verify is not complete. "
            "Glenn must double-click CHECKIN.cmd, sign in as Bordine, Glenn, "
            "double-click CHECKIN.cmd again, and leave Chrome open. "
            "This cloud host will not open Eleads."
        ]
        host = "cloud-linux"
        mode = "draft-blocked-local"
        session = ELEADS_SESSION_NONE
    else:
        shown = "workers=[]" if worker_count == 0 else f"workers={worker_count}"
        blockers = [
            f"Alienware-18 worker {ALIENWARE_WORKER_ID} is offline ({shown}). "
            "Start it on the Alienware with agent worker start --verbose and leave that window open. "
            "This cloud host will not open Eleads."
        ]
        host = "cloud-linux"
        mode = "draft-blocked-local"
        session = ELEADS_SESSION_NONE

    stamp = now.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    return {
        "cycleId": f"{'ready' if verified else 'blocked'}-{stamp}",
        "openedAtUtc": stamp,
        "checkedAtUtc": stamp,
        "host": host,
        "launcherHost": socket.gethostname(),
        "verify": verified,
        "glennVerified": verified,
        "mode": mode,
        "spawnedSpecialists": 0,
        "catalogSize": 51,
        "workOrder": None,
        "queue": [],
        "inspected": [],
        "drafts": [],
        "sends": [],
        "appointments": [],
        "crmProof": [],
        "blockers": blockers,
        "specialistResults": specialist_results(),
        "persistedBy": "core",
        "ownershipScreenDays": OWNERSHIP_SCREEN_DAYS,
        "eleadsSession": session,
        "workerId": seen_id,
        "workerOnline": online,
        "timerTick": None,
        "openedFromThisProcess": False,
    }


def same_blocker(previous: dict | None, state: dict) -> bool:
    if not previous:
        return False
    return previous.get("blockers") == state.get("blockers") and previous.get("verify") == state.get("verify")


def write_state(path: Path, state: dict, previous: dict | None) -> dict:
    if same_blocker(previous, state) and previous is not None:
        state = dict(state)
        state["cycleId"] = previous.get("cycleId", state["cycleId"])
        state["openedAtUtc"] = previous.get("openedAtUtc", state["openedAtUtc"])
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")
    return state


def prepend_ticklog(path: Path, state: dict, previous: dict | None) -> None:
    if same_blocker(previous, state):
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    prior = path.read_text(encoding="utf-8") if path.is_file() else ""
    blocker = state["blockers"][0] if state["blockers"] else "Verified Alienware session. No send from this cloud host."
    note = (
        f"## {state['checkedAtUtc']} launch-check\n"
        f"{blocker} No timer fired. This is not a numbered tick. "
        f"Eleads was not opened. sends=[].\n\n"
    )
    path.write_text(note + prior, encoding="utf-8")


def launch(
    workers_file: Path | None,
    verify_receipt: Path | None,
    state_file: Path,
    ticklog_file: Path,
    navigate: str | None,
    now: datetime | None = None,
) -> dict:
    if navigate:
        open_eleads(navigate)
    snapshot = load_json(workers_file) or {"workers": [], "source": "missing-snapshot-treated-as-offline"}
    receipt = load_json(verify_receipt)
    moment = now or datetime.now(timezone.utc)
    state = build_state(snapshot, receipt, moment)
    previous = load_json(state_file)
    state = write_state(state_file, state, previous)
    prepend_ticklog(ticklog_file, state, previous)
    return state


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Attach the Eleads COS loop without opening Eleads.")
    parser.add_argument("--workers-file", type=Path, default=None)
    parser.add_argument("--verify-receipt", type=Path, default=None)
    parser.add_argument("--state-file", type=Path, default=DEFAULT_STATE)
    parser.add_argument("--ticklog-file", type=Path, default=DEFAULT_TICKLOG)
    parser.add_argument("--navigate", default=None)
    args = parser.parse_args(argv)
    try:
        state = launch(
            args.workers_file,
            args.verify_receipt,
            args.state_file,
            args.ticklog_file,
            args.navigate,
        )
    except EleadsOpenRefused as exc:
        print(str(exc))
        return 2
    print(json.dumps({
        "mode": state["mode"],
        "verify": state["verify"],
        "workerOnline": state["workerOnline"],
        "eleadsSession": state["eleadsSession"],
    }, indent=2))
    return 0 if state["verify"] else 3


if __name__ == "__main__":
    raise SystemExit(main())
