from __future__ import annotations

import argparse
import logging
import subprocess
import sys
import tempfile
import textwrap
from datetime import date, datetime
from pathlib import Path

from titan.agents.appointments import AppointmentAgent
from titan.agents.approvals import ApprovalError, ApprovalService
from titan.agents.context import AgentContext
from titan.agents.orchestrator import Orchestrator
from titan.agents.reporting import Reporter
from titan.config import Settings, load_settings
from titan.crm.base import EleadsAdapter
from titan.models import MessageStatus
from titan.policy.clock import Clock
from titan.storage import Store

TASK_NAME = "TitanCrmAgent"


def _context(settings: Settings) -> AgentContext:
    return AgentContext(settings, Store(settings.storage.db_path), Clock(settings.time.timezone))


def _adapter_factory(settings: Settings, ctx: AgentContext):
    if settings.crm.adapter == "playwright":
        from titan.crm.playwright_adapter import PlaywrightEleadsAdapter

        return lambda rooftop_id: PlaywrightEleadsAdapter(settings.crm, ctx.clock.tz)
    from titan.crm.mock import MockCrm, MockEleadsAdapter

    crm = MockCrm(settings.identity.salesperson_name, now=ctx.clock.now)
    return lambda rooftop_id: MockEleadsAdapter(crm)


def _print_message(m) -> None:
    print(f"--- {m.id}  [{m.channel.value.upper()}] {m.customer_name} @ rooftop {m.rooftop_id} "
          f"({m.source.value}) -> {m.recipient}")
    if m.subject:
        print(f"Subject: {m.subject}")
    print(textwrap.indent(m.body, "    "))
    if m.error:
        print(f"    !! {m.error}")


def cmd_run(args, settings: Settings) -> int:
    ctx = _context(settings)
    orch = Orchestrator(ctx, _adapter_factory(settings, ctx))
    if args.once:
        for r in orch.run_once():
            print(r)
        return 0
    orch.start()
    orch.wait()
    return 0


def cmd_approvals(args, settings: Settings) -> int:
    svc = ApprovalService(_context(settings))
    pending = svc.pending()
    if not pending:
        print("No drafts awaiting approval.")
    for m in pending:
        _print_message(m)
        problems = svc.blocking_problems(m)
        if problems:
            print(f"    Must fix before approval: {'; '.join(problems)}")
    return 0


def cmd_approve(args, settings: Settings) -> int:
    svc = ApprovalService(_context(settings))
    try:
        done = svc.approve_batch(args.approver) if args.batch else svc.approve(args.ids, args.approver)
    except ApprovalError as exc:
        print(f"Not approved: {exc}", file=sys.stderr)
        return 2
    for m in done:
        print(f"Approved {m.id} ({m.channel.value} to {m.customer_name}); sends in the next open window.")
    return 0


def cmd_edit(args, settings: Settings) -> int:
    svc = ApprovalService(_context(settings))
    body = Path(args.body_file).read_text(encoding="utf-8") if args.body_file else args.body
    m = svc.edit(args.id, body=body, subject=args.subject)
    _print_message(m)
    print("Edited draft returned to pending approval.")
    return 0


def cmd_reject(args, settings: Settings) -> int:
    ApprovalService(_context(settings)).reject(args.id, args.reason)
    print("Rejected.")
    return 0


def cmd_appointment(args, settings: Settings) -> int:
    ctx = _context(settings)
    agent = AppointmentAgent(ctx)
    if args.action == "list":
        for a in ctx.store.appointments():
            print(f"{a.id} {a.status.value:16} {a.customer_name} {a.starts_at:%a %b %d %I:%M %p} "
                  f"{a.purpose} (confirm call {a.confirmation_call_at:%a %I:%M %p})")
        return 0
    if args.action == "approve":
        agent.approve(args.id, settings.identity.approver_name)
        print("Approved; it will be saved to Eleads on the next cycle.")
        return 0
    starts = datetime.fromisoformat(args.at).replace(tzinfo=ctx.clock.tz)
    p = agent.propose(rooftop_id=args.rooftop, customer_id=args.customer, opportunity_id=args.opportunity,
                      customer_name=args.name, starts_at=starts, purpose=args.purpose,
                      arrival_notes=args.notes or "")
    print(f"Proposed {p.id}: {p.customer_name} {p.starts_at:%a %b %d %I:%M %p}; "
          f"confirmation call {p.confirmation_call_at:%a %b %d %I:%M %p}. Awaiting approval.")
    return 0


def cmd_report(args, settings: Settings) -> int:
    ctx = _context(settings)
    day = date.fromisoformat(args.date) if args.date else ctx.clock.now().date()
    print(Reporter.render(Reporter(ctx).build(day), day, ctx.clock.now()))
    return 0


def cmd_demo(args, settings: Settings) -> int:
    from titan.crm.mock import MockCrm, MockEleadsAdapter
    from titan.demo import seed
    from titan.policy.clock import FrozenClock

    settings.storage.db_path = Path(tempfile.mkdtemp()) / "demo.db"
    at = datetime.fromisoformat(args.at) if args.at else datetime.now().replace(hour=10, minute=15)
    clock = FrozenClock(at, settings.time.timezone)
    ctx = AgentContext(settings, Store(settings.storage.db_path), clock)
    crm = MockCrm(settings.identity.salesperson_name, now=clock.now)
    seed(crm, clock.now())
    adapters: dict[str, EleadsAdapter] = {}

    def factory(rid: str) -> EleadsAdapter:
        adapters[rid] = MockEleadsAdapter(crm)
        return adapters[rid]

    orch = Orchestrator(ctx, factory)
    print(f"== Cycle 1 at {clock.now():%a %Y-%m-%d %H:%M %Z}: detect, inspect, draft ==")
    for r in orch.run_once():
        print(f"  {r}")
    svc = ApprovalService(ctx)
    for m in svc.pending():
        _print_message(m)
    if args.approve:
        print("\n== Demo approval of every lint-clean draft (simulating Glenn) ==")
        for m in svc.pending():
            if not svc.blocking_problems(m):
                svc.approve([m.id], "Glenn Bordine (demo)")
        clock.advance(minutes=1)
        print(f"== Cycle 2 at {clock.now():%H:%M}: send once, verify, complete tasks ==")
        for r in orch.run_once():
            print(f"  {r}")
    print()
    print(Reporter.render(Reporter(ctx).build(clock.now().date(), adapters), clock.now().date(), clock.now()))
    print(f"Max simultaneous profile tabs: {crm.max_concurrent_tabs}; open now: "
          f"{sum(a.open_tab_count() for a in adapters.values())}")
    return 0


def cmd_credentials(args, settings: Settings) -> int:
    import getpass

    import keyring

    username = input("Eleads username: ").strip()
    password = getpass.getpass("Eleads password: ")
    keyring.set_password(settings.crm.credential_service, "username", username)
    keyring.set_password(settings.crm.credential_service, username, password)
    print("Stored in Windows Credential Manager.")
    return 0


def cmd_startup(args, settings: Settings) -> int:
    if sys.platform != "win32":
        print("Startup registration is only supported on Windows.", file=sys.stderr)
        return 2
    if args.action == "install":
        exe = Path(sys.executable).with_name("pythonw.exe")
        cmd = f'"{exe}" -m titan.cli run'
        if args.config:
            cmd += f' --config "{Path(args.config).resolve()}"'
        subprocess.run(["schtasks", "/Create", "/F", "/SC", "ONLOGON", "/RL", "LIMITED", "/TN", TASK_NAME,
                        "/TR", cmd, "/DELAY", "0001:00"], check=True)
        print(f"Registered scheduled task '{TASK_NAME}' (runs at logon, 1 minute delay).")
    else:
        subprocess.run(["schtasks", "/Delete", "/F", "/TN", TASK_NAME], check=True)
        print(f"Removed scheduled task '{TASK_NAME}'.")
    return 0


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(prog="titan", description="Approval-gated Eleads CRM chief of staff")
    p.add_argument("--config", help="Path to config.toml")
    p.add_argument("-v", "--verbose", action="store_true")
    sub = p.add_subparsers(dest="cmd", required=True)

    r = sub.add_parser("run", help="Run the primary agent (both rooftops) continuously")
    r.add_argument("--once", action="store_true", help="Run a single cycle and exit")
    r.set_defaults(fn=cmd_run)

    sub.add_parser("approvals", help="List drafts awaiting Glenn's approval").set_defaults(fn=cmd_approvals)

    a = sub.add_parser("approve", help="Approve exact drafts by id")
    a.add_argument("ids", nargs="*")
    a.add_argument("--batch", action="store_true", help="Approve all lint-clean drafts (approval.mode=batch only)")
    a.add_argument("--approver", default=None)
    a.set_defaults(fn=cmd_approve)

    e = sub.add_parser("edit", help="Edit a draft (returns it to pending approval)")
    e.add_argument("id")
    e.add_argument("--subject")
    e.add_argument("--body")
    e.add_argument("--body-file")
    e.set_defaults(fn=cmd_edit)

    j = sub.add_parser("reject", help="Reject a draft")
    j.add_argument("id")
    j.add_argument("--reason", required=True)
    j.set_defaults(fn=cmd_reject)

    ap = sub.add_parser("appointment", help="Propose / approve / list appointments")
    ap.add_argument("action", choices=["propose", "approve", "list"])
    ap.add_argument("--id")
    ap.add_argument("--rooftop")
    ap.add_argument("--customer")
    ap.add_argument("--opportunity")
    ap.add_argument("--name")
    ap.add_argument("--at", help="Local time, e.g. 2026-10-16T11:00")
    ap.add_argument("--purpose", default="complimentary appraisal")
    ap.add_argument("--notes")
    ap.set_defaults(fn=cmd_appointment)

    rp = sub.add_parser("report", help="Management report")
    rp.add_argument("--date")
    rp.set_defaults(fn=cmd_report)

    d = sub.add_parser("demo", help="End-to-end dry run against fictional data (no real CRM)")
    d.add_argument("--at", help="Simulated local time, e.g. 2026-10-15T10:15")
    d.add_argument("--approve", action="store_true", help="Simulate Glenn approving lint-clean drafts")
    d.set_defaults(fn=cmd_demo)

    c = sub.add_parser("credentials", help="Store Eleads login in Windows Credential Manager")
    c.add_argument("action", choices=["set"])
    c.set_defaults(fn=cmd_credentials)

    s = sub.add_parser("startup", help="Register/unregister automatic start with Windows")
    s.add_argument("action", choices=["install", "uninstall"])
    s.set_defaults(fn=cmd_startup)
    return p


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    logging.basicConfig(level=logging.DEBUG if args.verbose else logging.INFO,
                        format="%(asctime)s %(levelname)s %(threadName)s %(name)s: %(message)s")
    settings = load_settings(args.config)
    if getattr(args, "approver", "x") is None:
        args.approver = settings.identity.approver_name
    if args.cmd == "appointment" and args.action == "approve" and not args.id:
        print("--id is required", file=sys.stderr)
        return 2
    return args.fn(args, settings)


if __name__ == "__main__":
    raise SystemExit(main())
