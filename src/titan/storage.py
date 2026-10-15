from __future__ import annotations

import json
import sqlite3
import threading
from contextlib import contextmanager
from dataclasses import asdict, fields
from datetime import datetime
from enum import Enum
from pathlib import Path
from typing import Any, Iterator

from titan.models import (
    AppointmentProposal,
    AppointmentStatus,
    Channel,
    CustomerDigest,
    Disposition,
    MessageSource,
    MessageStatus,
    OutboundMessage,
    PlannedTask,
    TaskKind,
    TaskStatus,
)

SCHEMA = """
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
CREATE TABLE IF NOT EXISTS messages (
    id TEXT PRIMARY KEY, customer_id TEXT, opportunity_id TEXT, rooftop_id TEXT, channel TEXT,
    recipient TEXT, subject TEXT, body TEXT, source TEXT, status TEXT, created_at TEXT, detected_at TEXT,
    approved_at TEXT, approved_by TEXT, approved_hash TEXT, batch_id TEXT, sent_at TEXT, verified_at TEXT,
    completed_at TEXT, crm_activity_id TEXT, crm_task_id TEXT, error TEXT, customer_name TEXT
);
CREATE INDEX IF NOT EXISTS ix_messages_status ON messages(status);
CREATE INDEX IF NOT EXISTS ix_messages_customer ON messages(customer_id, channel);
CREATE TABLE IF NOT EXISTS tasks (
    id TEXT PRIMARY KEY, customer_id TEXT, opportunity_id TEXT, rooftop_id TEXT, kind TEXT, due_at TEXT,
    priority INTEGER, reason TEXT, status TEXT, crm_task_id TEXT
);
CREATE INDEX IF NOT EXISTS ix_tasks_due ON tasks(status, due_at);
CREATE TABLE IF NOT EXISTS digests (
    customer_id TEXT, rooftop_id TEXT, summary TEXT, next_best_action TEXT, sms_template TEXT,
    email_subject TEXT, email_template TEXT, disposition TEXT, disposition_reasons TEXT, updated_at TEXT,
    last_outreach_at TEXT, PRIMARY KEY (customer_id, rooftop_id)
);
CREATE TABLE IF NOT EXISTS appointments (
    id TEXT PRIMARY KEY, customer_id TEXT, opportunity_id TEXT, rooftop_id TEXT, customer_name TEXT,
    starts_at TEXT, purpose TEXT, arrival_notes TEXT, confirmation_call_at TEXT, status TEXT,
    crm_appointment_id TEXT, crm_confirmation_task_id TEXT
);
CREATE TABLE IF NOT EXISTS seen_leads (
    rooftop_id TEXT, opportunity_id TEXT, detected_at TEXT, PRIMARY KEY (rooftop_id, opportunity_id)
);
CREATE TABLE IF NOT EXISTS cursors (name TEXT PRIMARY KEY, value TEXT);
CREATE TABLE IF NOT EXISTS audit (
    id INTEGER PRIMARY KEY AUTOINCREMENT, at TEXT, actor TEXT, rooftop_id TEXT, customer_id TEXT,
    event TEXT, detail TEXT
);
CREATE INDEX IF NOT EXISTS ix_audit_at ON audit(at);
"""

_ENUMS: dict[str, type[Enum]] = {
    "channel": Channel,
    "source": MessageSource,
    "status": MessageStatus,
}


def _enc(value: Any) -> Any:
    if isinstance(value, datetime):
        return value.isoformat()
    if isinstance(value, Enum):
        return value.value
    if isinstance(value, (tuple, list)):
        return json.dumps(list(value))
    return value


def _dt(value: str | None) -> datetime | None:
    return datetime.fromisoformat(value) if value else None


class Store:
    """Local, single-file audit database. CRM remains the authoritative record; this tracks the agent's own work."""

    def __init__(self, path: Path | str):
        self.path = Path(path)
        if str(path) != ":memory:":
            self.path.parent.mkdir(parents=True, exist_ok=True)
        self._lock = threading.RLock()
        self._conn = sqlite3.connect(str(path), check_same_thread=False, isolation_level=None)
        self._conn.row_factory = sqlite3.Row
        self._conn.executescript(SCHEMA)

    def close(self) -> None:
        with self._lock:
            self._conn.close()

    @contextmanager
    def tx(self) -> Iterator[sqlite3.Connection]:
        with self._lock:
            self._conn.execute("BEGIN IMMEDIATE")
            try:
                yield self._conn
                self._conn.execute("COMMIT")
            except BaseException:
                self._conn.execute("ROLLBACK")
                raise

    def _query(self, sql: str, args: tuple = ()) -> list[sqlite3.Row]:
        with self._lock:
            return list(self._conn.execute(sql, args))

    # --- audit -------------------------------------------------------------
    def audit(self, at: datetime, actor: str, event: str, *, rooftop_id: str | None = None,
              customer_id: str | None = None, **detail: Any) -> None:
        with self.tx() as c:
            c.execute(
                "INSERT INTO audit(at, actor, rooftop_id, customer_id, event, detail) VALUES (?,?,?,?,?,?)",
                (at.isoformat(), actor, rooftop_id, customer_id, event, json.dumps(detail, default=str)),
            )

    def audit_events(self, since: datetime | None = None) -> list[dict[str, Any]]:
        rows = self._query(
            "SELECT * FROM audit WHERE at >= ? ORDER BY id", ((since.isoformat() if since else ""),)
        )
        return [dict(r) | {"detail": json.loads(r["detail"])} for r in rows]

    # --- messages ----------------------------------------------------------
    def save_message(self, m: OutboundMessage) -> None:
        data = {f.name: _enc(getattr(m, f.name)) for f in fields(m)}
        cols = ",".join(data)
        marks = ",".join("?" for _ in data)
        with self.tx() as c:
            c.execute(f"INSERT OR REPLACE INTO messages({cols}) VALUES ({marks})", tuple(data.values()))

    def _row_to_message(self, r: sqlite3.Row) -> OutboundMessage:
        kw: dict[str, Any] = dict(r)
        for k, enum in _ENUMS.items():
            kw[k] = enum(kw[k])
        for k in ("created_at", "detected_at", "approved_at", "sent_at", "verified_at", "completed_at"):
            kw[k] = _dt(kw[k])
        return OutboundMessage(**kw)

    def get_message(self, message_id: str) -> OutboundMessage | None:
        rows = self._query("SELECT * FROM messages WHERE id = ?", (message_id,))
        return self._row_to_message(rows[0]) if rows else None

    def messages(self, *statuses: MessageStatus, rooftop_id: str | None = None) -> list[OutboundMessage]:
        sql = "SELECT * FROM messages"
        clauses, args = [], []
        if statuses:
            clauses.append(f"status IN ({','.join('?' for _ in statuses)})")
            args.extend(s.value for s in statuses)
        if rooftop_id:
            clauses.append("rooftop_id = ?")
            args.append(rooftop_id)
        if clauses:
            sql += " WHERE " + " AND ".join(clauses)
        return [self._row_to_message(r) for r in self._query(sql + " ORDER BY created_at", tuple(args))]

    def open_message_for(self, customer_id: str, channel: Channel) -> OutboundMessage | None:
        active = (MessageStatus.PENDING_APPROVAL, MessageStatus.APPROVED, MessageStatus.SENDING,
                  MessageStatus.SEND_UNCERTAIN, MessageStatus.SENT_VERIFIED)
        rows = self._query(
            f"SELECT * FROM messages WHERE customer_id = ? AND channel = ? AND status IN "
            f"({','.join('?' for _ in active)}) ORDER BY created_at DESC LIMIT 1",
            (customer_id, channel.value, *(s.value for s in active)),
        )
        return self._row_to_message(rows[0]) if rows else None

    # --- tasks -------------------------------------------------------------
    def save_task(self, t: PlannedTask) -> None:
        data = {f.name: _enc(getattr(t, f.name)) for f in fields(t)}
        with self.tx() as c:
            c.execute(
                f"INSERT OR REPLACE INTO tasks({','.join(data)}) VALUES ({','.join('?' for _ in data)})",
                tuple(data.values()),
            )

    def tasks(self, *statuses: TaskStatus) -> list[PlannedTask]:
        sql, args = "SELECT * FROM tasks", ()
        if statuses:
            sql += f" WHERE status IN ({','.join('?' for _ in statuses)})"
            args = tuple(s.value for s in statuses)
        out = []
        for r in self._query(sql + " ORDER BY due_at, priority", args):
            kw = dict(r)
            kw["kind"] = TaskKind(kw["kind"])
            kw["status"] = TaskStatus(kw["status"])
            kw["due_at"] = _dt(kw["due_at"])
            out.append(PlannedTask(**kw))
        return out

    def has_open_task(self, customer_id: str, kind: TaskKind) -> bool:
        return bool(self._query(
            "SELECT 1 FROM tasks WHERE customer_id = ? AND kind = ? AND status IN (?, ?)",
            (customer_id, kind.value, TaskStatus.SCHEDULED.value, TaskStatus.CRM_CREATED.value),
        ))

    # --- digests -----------------------------------------------------------
    def save_digest(self, d: CustomerDigest) -> None:
        data = {k: _enc(v) for k, v in asdict(d).items()}
        with self.tx() as c:
            c.execute(
                f"INSERT OR REPLACE INTO digests({','.join(data)}) VALUES ({','.join('?' for _ in data)})",
                tuple(data.values()),
            )

    def get_digest(self, customer_id: str, rooftop_id: str) -> CustomerDigest | None:
        rows = self._query("SELECT * FROM digests WHERE customer_id = ? AND rooftop_id = ?", (customer_id, rooftop_id))
        if not rows:
            return None
        kw = dict(rows[0])
        kw["disposition"] = Disposition(kw["disposition"])
        kw["disposition_reasons"] = tuple(json.loads(kw["disposition_reasons"]))
        kw["updated_at"] = _dt(kw["updated_at"])
        kw["last_outreach_at"] = _dt(kw["last_outreach_at"])
        return CustomerDigest(**kw)

    def digests(self) -> list[CustomerDigest]:
        rows = self._query("SELECT customer_id, rooftop_id FROM digests")
        return [d for r in rows if (d := self.get_digest(r["customer_id"], r["rooftop_id"]))]

    # --- appointments ------------------------------------------------------
    def save_appointment(self, a: AppointmentProposal) -> None:
        data = {f.name: _enc(getattr(a, f.name)) for f in fields(a)}
        with self.tx() as c:
            c.execute(
                f"INSERT OR REPLACE INTO appointments({','.join(data)}) VALUES ({','.join('?' for _ in data)})",
                tuple(data.values()),
            )

    def appointments(self, *statuses: AppointmentStatus) -> list[AppointmentProposal]:
        sql, args = "SELECT * FROM appointments", ()
        if statuses:
            sql += f" WHERE status IN ({','.join('?' for _ in statuses)})"
            args = tuple(s.value for s in statuses)
        out = []
        for r in self._query(sql, args):
            kw = dict(r)
            kw["status"] = AppointmentStatus(kw["status"])
            kw["starts_at"] = _dt(kw["starts_at"])
            kw["confirmation_call_at"] = _dt(kw["confirmation_call_at"])
            out.append(AppointmentProposal(**kw))
        return out

    def get_appointment(self, appointment_id: str) -> AppointmentProposal | None:
        return next((a for a in self.appointments() if a.id == appointment_id), None)

    # --- lead tracking / cursors ------------------------------------------
    def mark_lead_seen(self, rooftop_id: str, opportunity_id: str, at: datetime) -> bool:
        """Returns True only the first time a lead is seen (idempotent across restarts)."""
        with self.tx() as c:
            cur = c.execute(
                "INSERT OR IGNORE INTO seen_leads(rooftop_id, opportunity_id, detected_at) VALUES (?,?,?)",
                (rooftop_id, opportunity_id, at.isoformat()),
            )
            return cur.rowcount == 1

    def get_cursor(self, name: str) -> str | None:
        rows = self._query("SELECT value FROM cursors WHERE name = ?", (name,))
        return rows[0]["value"] if rows else None

    def set_cursor(self, name: str, value: str | None) -> None:
        with self.tx() as c:
            c.execute("INSERT OR REPLACE INTO cursors(name, value) VALUES (?, ?)", (name, value))
