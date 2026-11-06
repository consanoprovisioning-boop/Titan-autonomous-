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
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 20, 59, tzinfo=launch.NY)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 19, 0, tzinfo=launch.NY)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 20, 0, tzinfo=launch.NY)))
        self.assertFalse(launch.sms_open(datetime(2026, 10, 16, 21, 0, tzinfo=launch.NY)))
        self.assertFalse(launch.sms_open(datetime(2026, 10, 16, 11, 59, tzinfo=timezone.utc)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 12, 0, tzinfo=timezone.utc)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 16, 23, 0, tzinfo=timezone.utc)))
        self.assertTrue(launch.sms_open(datetime(2026, 10, 17, 0, 0, tzinfo=timezone.utc)))
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
        self.assertTrue(data["clusterHoldLaunch"])
        self.assertEqual("Alienware-18", data["clusterMain"])
        self.assertEqual("installing", data["desktop4"]["status"])
        self.assertEqual("installing", data["desktop5"]["status"])
        self.assertEqual("installing", data["desktop6"]["status"])
        self.assertEqual(["Desktop4", "Desktop5", "Desktop6"], data["wifiConnected"])
        self.assertIn("held", data["blockers"][0].lower())
        self.assertIn("MAIN", data["blockers"][0])
        self.assertNotIn("START-WORKER.cmd", data["blockers"][0])
        self.assertEqual(8, len(data["cluster"]))
        self.assertEqual(0, data["clusterOnlineCount"])
        self.assertTrue(all(node["status"] == "missing" and node["fabricated"] is False for node in data["cluster"]))
        self.assertEqual("Desktop3", data["cluster"][1]["nodeId"])
        self.assertEqual("missing", data["cluster"][1]["status"])
        self.assertEqual("Desktop4", data["cluster"][3]["nodeId"])
        self.assertEqual("missing", data["cluster"][3]["status"])
        self.assertEqual("Desktop5", data["cluster"][4]["nodeId"])
        self.assertEqual("missing", data["cluster"][4]["status"])
        self.assertEqual("Desktop6", data["cluster"][5]["nodeId"])
        self.assertEqual("missing", data["cluster"][5]["status"])
        self.assertEqual("Node8", data["cluster"][7]["nodeId"])
        self.assertEqual("missing", data["cluster"][7]["status"])
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

    def test_desktop3_worker_starts_from_userprofile(self) -> None:
        starter = (ROOT / "deploy/desktop3/START-WORKER.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        executable = "\n".join(
            line for line in starter.splitlines()
            if not line.lstrip().upper().startswith("REM ")
        )
        self.assertIn('cd /d "%USERPROFILE%"', starter)
        self.assertIn("Desktop3", starter)
        self.assertNotIn("eleadcrm.com", executable)
        self.assertNotIn("Titan.ChiefOfStaff.exe", executable)
        self.assertIn("does NOT start Titan.ChiefOfStaff.exe", starter)
        self.assertIn("system32", starter.lower())

    def test_desktop4_worker_starts_from_userprofile(self) -> None:
        starter = (ROOT / "deploy/desktop4/START-WORKER.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        enroll = (ROOT / "deploy/cluster-node/ENROLL-NODE.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        helper = (ROOT / "deploy/desktop4/ENROLL.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        executable = "\n".join(
            line for line in starter.splitlines()
            if not line.lstrip().upper().startswith("REM ")
        )
        self.assertIn('cd /d "%USERPROFILE%"', starter)
        self.assertIn("--name \"Desktop4\"", starter)
        self.assertNotIn("eleadcrm.com", executable)
        self.assertNotIn("Titan.ChiefOfStaff.exe", executable)
        self.assertIn("does NOT start Titan.ChiefOfStaff.exe", starter)
        self.assertIn("system32", starter.lower())
        self.assertIn("Desktop4", enroll)
        self.assertIn('ENROLL-NODE.cmd" Desktop4', helper)
        playbook = (ROOT / "docs/deploy/desktop4.md").read_text(encoding="utf-8")
        hold = (ROOT / "deploy/cluster-node/HOLD-LAUNCH.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        self.assertIn("\\\\Alienware-18\\Users\\Glenn R Bordine\\Titan-autonomous-", playbook)
        self.assertIn("git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git", playbook)
        self.assertIn("provisional", playbook.lower())
        self.assertIn("DO NOT launch", playbook)
        self.assertIn("HOLD-LAUNCH.cmd", starter)
        self.assertIn("exit /b 2", hold)
        self.assertIn("cluster hold is on", starter.lower())
        self.assertNotIn("C:\\Windows\\system32\\Titan.cmd", playbook)
        self.assertIn("INSTALL-COS.cmd", playbook)

    def test_desktop4_install_cos_is_install_only(self) -> None:
        script = (ROOT / "deploy/desktop4/INSTALL-COS.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        install = (ROOT / "deploy/alienware-local-program/INSTALL.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        hold = json.loads((ROOT / "deploy/cluster-node/cluster-hold.json").read_text(encoding="utf-8"))
        here = (ROOT / "deploy/desktop4/START-HERE.txt").read_text(encoding="utf-8")
        executable = "\n".join(
            line for line in script.splitlines()
            if not line.lstrip().upper().startswith("REM ")
        )
        call_lines = "\n".join(
            line for line in executable.splitlines()
            if line.lstrip().lower().startswith("call ")
        )
        self.assertTrue(hold["holdLaunch"])
        self.assertEqual("Alienware-18", hold["clusterMain"])
        self.assertEqual("installing", hold["desktop4"]["status"])
        self.assertIn('cd /d "%USERPROFILE%"', script)
        self.assertIn("system32", script.lower())
        self.assertIn("cursor/eleads-cluster-4pc-7dfe", script)
        self.assertIn("Alienware-18", script)
        self.assertIn("INSTALL.cmd", script)
        self.assertIn('ENROLL-NODE.cmd" Desktop4', script)
        self.assertIn("INSTALLED. LAUNCH HELD.", script)
        self.assertIn("holdLaunch=true", script)
        self.assertIn("refuse_start", script)
        self.assertIn('if /i "%~1"=="START-WORKER"', script)
        self.assertIn("TITAN_INSTALL_NOPAUSE", script)
        self.assertIn("TITAN_INSTALL_NOPAUSE", install)
        self.assertIn("INSTALL-COS.cmd", here)
        self.assertNotIn("eleadcrm.com", executable)
        self.assertNotIn("agent worker start", executable)
        self.assertNotIn("START-WORKER", call_lines)
        self.assertNotIn("CHECKIN.cmd", call_lines)
        self.assertNotIn("powershell", executable.lower())
        self.assertIn("does not start titan.chiefofstaff.exe", script.lower())

    def test_desktop5_and_desktop6_install_cos_is_install_only(self) -> None:
        hold = json.loads((ROOT / "deploy/cluster-node/cluster-hold.json").read_text(encoding="utf-8"))
        enroll = (ROOT / "deploy/cluster-node/ENROLL-NODE.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        self.assertTrue(hold["holdLaunch"])
        self.assertEqual("Alienware-18", hold["clusterMain"])
        self.assertEqual(["Desktop4", "Desktop5", "Desktop6"], hold["wifiConnected"])
        self.assertEqual("installing", hold["desktop5"]["status"])
        self.assertEqual("provisional-28206-P1Call", hold["desktop5"]["lane"])
        self.assertEqual("installing", hold["desktop6"]["status"])
        self.assertEqual("provisional-28546-P1Call", hold["desktop6"]["lane"])
        self.assertIn("Desktop5", enroll)
        self.assertIn("Desktop6", enroll)
        for name, node, rooftop_lane in (
            ("desktop5", "Desktop5", "rooftop 28206 P1 calls provisional"),
            ("desktop6", "Desktop6", "rooftop 28546 P1 calls provisional"),
        ):
            script = (ROOT / f"deploy/{name}/INSTALL-COS.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
            playbook = (ROOT / f"docs/deploy/{name}.md").read_text(encoding="utf-8")
            executable = "\n".join(
                line for line in script.splitlines()
                if not line.lstrip().upper().startswith("REM ")
            )
            call_lines = "\n".join(
                line for line in executable.splitlines()
                if line.lstrip().lower().startswith("call ")
            )
            self.assertIn('cd /d "%USERPROFILE%"', script)
            self.assertIn("system32", script.lower())
            self.assertIn("cursor/eleads-cluster-4pc-7dfe", script)
            self.assertIn("Alienware-18", script)
            self.assertIn("INSTALL.cmd", script)
            self.assertIn(f'ENROLL-NODE.cmd" {node}', script)
            self.assertIn("INSTALLED. LAUNCH HELD.", script)
            self.assertIn("holdLaunch=true", script)
            self.assertIn("refuse_start", script)
            self.assertIn('if /i "%~1"=="START-WORKER"', script)
            self.assertIn(rooftop_lane, script)
            self.assertIn("provisional", playbook.lower())
            self.assertIn("DO NOT launch", playbook)
            self.assertIn("INSTALL-COS.cmd", playbook)
            self.assertIn(f"\\\\Alienware-18\\Users\\Glenn R Bordine\\Titan-autonomous-\\deploy\\{name}\\INSTALL-COS.cmd", playbook)
            self.assertNotIn("eleadcrm.com", executable)
            self.assertNotIn("agent worker start", executable)
            self.assertNotIn("START-WORKER", call_lines)
            self.assertNotIn("CHECKIN.cmd", call_lines)
            self.assertNotIn("powershell", executable.lower())
            self.assertIn("does not start titan.chiefofstaff.exe", script.lower())

    def test_cluster_hold_blocks_ready_session(self) -> None:
        tmp = Path("/tmp/eleads-cos-hold-test")
        tmp.mkdir(exist_ok=True)
        workers = tmp / "workers.json"
        state_path = tmp / "state.json"
        ticklog = tmp / "TICKLOG.md"
        receipt = tmp / "verify.json"
        workers.write_text(json.dumps({
            "workers": [{
                "workerId": launch.ALIENWARE_WORKER_ID,
                "machineDisplayName": "Alienware-18",
                "displayName": "Alienware-18#C:\\Users\\Glenn R Bordine",
            }]
        }), encoding="utf-8")
        receipt.write_text(json.dumps({
            "glennVerified": True,
            "state": "VerifiedIdle",
            "identity": "Bordine, Glenn",
            "rooftop": "28206",
            "loginClaim": False,
        }), encoding="utf-8")
        code = launch.main([
            "--workers-file", str(workers),
            "--verify-receipt", str(receipt),
            "--state-file", str(state_path),
            "--ticklog-file", str(ticklog),
        ])
        self.assertEqual(3, code)
        data = json.loads(state_path.read_text(encoding="utf-8"))
        self.assertFalse(data["verify"])
        self.assertTrue(data["clusterHoldLaunch"])
        self.assertEqual("Alienware-18", data["clusterMain"])
        self.assertEqual([], data["sends"])
        self.assertEqual("draft-blocked-local", data["mode"])

    def test_scale_out_script_does_not_open_eleads(self) -> None:
        scale = (ROOT / "deploy/cluster-node/SCALE-OUT-8.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        enroll = (ROOT / "deploy/cluster-node/ENROLL-NODE.cmd").read_bytes().replace(b"\r\n", b"\n").decode("ascii")
        self.assertIn("Node8", enroll)
        self.assertNotIn("eleadcrm.com", scale)
        self.assertNotIn("Titan.ChiefOfStaff.exe", scale)
        self.assertIn("scaleOut", scale)

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

    def test_cluster_inventory_does_not_fabricate_desktop3(self) -> None:
        empty = launch.inventory_cluster({"workers": []})
        self.assertEqual(
            ["Alienware-18", "Desktop3", "Node3", "Desktop4", "Desktop5", "Desktop6", "Node7", "Node8"],
            [n["nodeId"] for n in empty],
        )
        self.assertTrue(all(n["status"] == "missing" and n["fabricated"] is False for n in empty))

        live = launch.inventory_cluster({
            "workers": [{
                "workerId": "abc",
                "machineDisplayName": "Alienware-18",
                "displayName": "Alienware-18#C:\\Users\\Glenn R Bordine",
            }]
        })
        self.assertEqual("online", live[0]["status"])
        self.assertEqual("missing", live[1]["status"])
        self.assertEqual("Desktop3", live[1]["nodeId"])
        self.assertEqual("missing", live[3]["status"])
        self.assertEqual("Desktop4", live[3]["nodeId"])

        named = launch.inventory_cluster({
            "workers": [{
                "workerId": "d4",
                "machineDisplayName": "DeskTop4",
                "displayName": "Desktop4#C:\\Users\\Glenn R Bordine",
            }]
        })
        self.assertEqual("Desktop4", named[3]["nodeId"])
        self.assertEqual("online", named[3]["status"])
        self.assertFalse(named[3]["fabricated"])

        alias = launch.inventory_cluster({
            "workers": [{
                "workerId": "n4",
                "machineDisplayName": "Node4",
                "displayName": "Node4",
            }]
        })
        self.assertEqual("Desktop4", alias[3]["nodeId"])
        self.assertEqual("online", alias[3]["status"])

        named5 = launch.inventory_cluster({
            "workers": [{
                "workerId": "d5",
                "machineDisplayName": "DeskTop5",
                "displayName": "Desktop5#C:\\Users\\Glenn R Bordine",
            }]
        })
        self.assertEqual("Desktop5", named5[4]["nodeId"])
        self.assertEqual("online", named5[4]["status"])
        self.assertFalse(named5[4]["fabricated"])

        alias6 = launch.inventory_cluster({
            "workers": [{
                "workerId": "n6",
                "machineDisplayName": "Node6",
                "displayName": "Node6",
            }]
        })
        self.assertEqual("Desktop6", alias6[5]["nodeId"])
        self.assertEqual("online", alias6[5]["status"])


if __name__ == "__main__":
    unittest.main()
