#!/usr/bin/env python3
"""Focused launch guards for the Eleads COS loop."""

from __future__ import annotations

import json
import sys
import unittest
from datetime import datetime, timedelta, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))

import eleads_cos_cloud_launch as launch  # noqa: E402


class LaunchGuardTests(unittest.TestCase):
    def test_rooftop_guard_rejects_6220(self) -> None:
        self.assertFalse(launch.rooftop_allowed("6220"))
        self.assertFalse(launch.rooftop_allowed("The TKO Autogroup (6220)"))
        self.assertFalse(launch.rooftop_allowed(""))
        self.assertFalse(launch.rooftop_allowed(None))
        self.assertTrue(launch.rooftop_allowed("28206"))
        self.assertTrue(launch.rooftop_allowed("28546"))

    def test_ownership_screen_is_180_days(self) -> None:
        now = datetime(2026, 10, 16, 12, 0, tzinfo=launch.NY)
        self.assertEqual(180, launch.OWNERSHIP_SCREEN_DAYS)
        self.assertTrue(launch.passes_ownership_screen(now, now - timedelta(days=180), False, None))
        self.assertFalse(launch.passes_ownership_screen(now, now - timedelta(days=181), False, None))
        self.assertTrue(launch.passes_ownership_screen(now, now - timedelta(days=400), True, 2014))
        self.assertFalse(launch.passes_ownership_screen(now, None, False, 2024))

    def test_sms_window_is_8_to_21_america_new_york(self) -> None:
        self.assertFalse(launch.sms_open(datetime(2026, 10, 16, 7, 59, tzinfo=launch.NY)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 8, 0, tzinfo=launch.NY)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 19, 0, tzinfo=launch.NY)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 20, 59, tzinfo=launch.NY)))
        self.assertFalse(launch.sms_open(datetime(2026, 10, 16, 21, 0, tzinfo=launch.NY)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 12, 0, tzinfo=timezone.utc)))
        self.assertFalse(launch.sms_open(datetime(2026, 10, 17, 1, 0, tzinfo=timezone.utc)))
        self.assertEqual("America/New_York", launch.NY.key)

    def test_appointment_set_requires_day_and_time(self) -> None:
        self.assertFalse(launch.appointment_set_allowed("Maybe Saturday"))
        self.assertFalse(launch.appointment_set_allowed("Saturday"))
        self.assertFalse(launch.appointment_set_allowed("10:30 AM"))
        self.assertFalse(launch.appointment_set_allowed(""))
        self.assertFalse(launch.appointment_set_allowed(None))
        self.assertTrue(launch.appointment_set_allowed("Saturday at 10:30 AM"))
        self.assertTrue(launch.appointment_set_allowed("Wednesday at 2 pm"))

    def test_cloud_host_refuses_to_open_eleads(self) -> None:
        source = (ROOT / "scripts" / "eleads_cos_cloud_launch.py").read_text(encoding="utf-8")
        wrapper = (ROOT / "scripts" / "launch-eleads-cos-loop.sh").read_text(encoding="utf-8")
        for banned in ("urlopen", "webbrowser", "xdg-open", "curl ", "Titan.ChiefOfStaff.exe"):
            self.assertNotIn(banned, source)
            self.assertNotIn(banned, wrapper)
        with self.assertRaises(launch.EleadsOpenRefused):
            launch.open_eleads("https://www.eleadcrm.com/evo2/fresh/login.asp")
        code = launch.main([
            "--navigate",
            "https://www.eleadcrm.com/evo2/fresh/login.asp",
            "--state-file",
            "/tmp/eleads-must-not-write-state.json",
            "--ticklog-file",
            "/tmp/eleads-must-not-write-ticklog.md",
        ])
        self.assertEqual(2, code)
        self.assertFalse(Path("/tmp/eleads-must-not-write-state.json").exists())

    def test_offline_worker_records_blocked_without_a_numbered_tick(self) -> None:
        tmp = Path("/tmp/eleads-cos-launch-test")
        if tmp.exists():
            for child in tmp.iterdir():
                child.unlink()
        else:
            tmp.mkdir()
        workers = tmp / "workers.json"
        state_path = tmp / "state.json"
        ticklog = tmp / "TICKLOG.md"
        workers.write_text(json.dumps({"workers": [], "pools": []}), encoding="utf-8")
        code = launch.main([
            "--workers-file",
            str(workers),
            "--state-file",
            str(state_path),
            "--ticklog-file",
            str(ticklog),
        ])
        self.assertEqual(3, code)
        data = json.loads(state_path.read_text(encoding="utf-8"))
        self.assertFalse(data["verify"])
        self.assertFalse(data["glennVerified"])
        self.assertFalse(data["workerOnline"])
        self.assertEqual([], data["sends"])
        self.assertEqual([], data["queue"])
        self.assertEqual([], data["appointments"])
        self.assertEqual(180, data["ownershipScreenDays"])
        self.assertNotIn("ownershipScreenMonths", data)
        self.assertIsNone(data["timerTick"])
        self.assertEqual("none-and-must-not-open-from-cloud", data["eleadsSession"])
        self.assertEqual(0, data["spawnedSpecialists"])
        self.assertEqual(51, data["catalogSize"])
        self.assertEqual(7, len(data["specialistResults"]))
        self.assertTrue(all(row["status"] == "not-run" for row in data["specialistResults"]))
        self.assertIn("offline", data["blockers"][0])
        self.assertIn(launch.ALIENWARE_WORKER_ID, data["blockers"][0])
        text = ticklog.read_text(encoding="utf-8")
        self.assertIn("not a numbered tick", text)
        self.assertNotIn("tick 1", text.lower())
        again = launch.main([
            "--workers-file",
            str(workers),
            "--state-file",
            str(state_path),
            "--ticklog-file",
            str(ticklog),
        ])
        self.assertEqual(3, again)
        self.assertEqual(text, ticklog.read_text(encoding="utf-8"))
        self.assertEqual(data["cycleId"], json.loads(state_path.read_text(encoding="utf-8"))["cycleId"])

    def test_checkin_cmd_does_not_type_verify(self) -> None:
        checkin = (ROOT / "deploy/alienware-local-program/CHECKIN.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        install = (ROOT / "deploy/alienware-local-program/INSTALL.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        for line in checkin.splitlines():
            stripped = line.strip().lower()
            self.assertNotEqual("verify", stripped)
            self.assertFalse(stripped.startswith("verify "))
        self.assertIn("checkin", checkin.lower())
        self.assertNotIn(".exe verify", checkin.lower())
        self.assertNotIn(".exe install", install.lower())
        self.assertNotIn(".exe checkin", install.lower())

    def test_verify_receipt_matches_local_marker(self) -> None:
        self.assertTrue(launch.verify_receipt_ok({
            "glennVerified": True,
            "state": "VerifiedIdle",
            "identity": "Bordine, Glenn",
            "rooftop": "28206",
            "verifiedAt": "2026-10-16T12:00:00-04:00",
            "loginClaim": False,
        }))
        self.assertTrue(launch.verify_receipt_ok({
            "glennVerified": True,
            "state": "VerifiedIdle",
            "identity": "Bordine, Glenn",
            "rooftop": "28546",
            "loginClaim": False,
        }))
        self.assertFalse(launch.verify_receipt_ok({
            "glennVerified": False,
            "state": "AwaitingVerify",
            "identity": None,
            "rooftop": "28206",
            "loginClaim": False,
        }))

    def test_login_claim_is_not_verify(self) -> None:
        self.assertFalse(launch.verify_receipt_ok({
            "loginClaim": True,
            "loggedIn": True,
            "glennVerified": False,
            "state": "AwaitingVerify",
            "rooftop": "28206",
            "identity": "Bordine, Glenn",
        }))
        self.assertFalse(launch.verify_receipt_ok({
            "glennVerified": True,
            "state": "VerifiedIdle",
            "rooftop": "6220",
            "identity": "Bordine, Glenn",
        }))


if __name__ == "__main__":
    unittest.main()
