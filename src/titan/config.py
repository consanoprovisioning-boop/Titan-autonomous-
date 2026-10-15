from __future__ import annotations

import os
import sys
import tomllib
from dataclasses import dataclass, field, fields, is_dataclass
from datetime import time
from pathlib import Path
from typing import Any


def _t(value: str) -> time:
    hh, mm = value.split(":")
    return time(int(hh), int(mm))


def default_data_dir() -> Path:
    if sys.platform == "win32":
        return Path(os.environ.get("APPDATA", Path.home())) / "Titan"
    return Path.home() / ".titan"


@dataclass
class IdentityConfig:
    salesperson_name: str = "Bordine, Glenn"
    allowed_rooftops: dict[str, str] = field(
        default_factory=lambda: {"28206": "Valdosta Nissan", "28546": "Valdosta Mitsubishi"}
    )
    blocked_rooftops: list[str] = field(default_factory=lambda: ["6220"])
    storewide_authorized: bool = False
    approver_name: str = "Glenn Bordine"


@dataclass
class TimeConfig:
    timezone: str = "America/New_York"
    send_window_start: time = time(9, 0)
    send_window_end: time = time(19, 0)
    sms_window_start: time = time(7, 0)
    sms_window_end: time = time(21, 59)
    dealership_hours: dict[int, tuple[time, time]] = field(
        default_factory=lambda: {
            **{d: (time(9, 0), time(19, 0)) for d in range(5)},
            5: (time(9, 0), time(18, 0)),
        }
    )


@dataclass
class ApprovalConfig:
    mode: str = "every_send"


@dataclass
class SlaConfig:
    new_lead_draft_seconds: int = 60
    new_lead_total_seconds: int = 240


@dataclass
class CadenceConfig:
    past_purchase_min_months: int = 14
    reengagement_interval_hours: int = 48
    own_customer_min_interval_hours: int = 12
    other_consultant_hold_hours: int = 72
    task_spacing_minutes: int = 5
    inactive_followup_working_days: int = 4
    confirmation_lead_hours: int = 24
    recent_inquiry_days: int = 30
    new_lead_lookback_hours: int = 24


@dataclass
class MessagingConfig:
    email_max_words: int = 120
    sms_max_chars: int = 320


@dataclass
class ResourceConfig:
    max_open_profiles: int = 2
    ram_soft_limit_mb: int = 600
    ram_hard_limit_mb: int = 900
    poll_interval_seconds: int = 20
    history_scan_batch_size: int = 25


@dataclass
class StorageConfig:
    db_path: Path = field(default_factory=lambda: default_data_dir() / "titan.db")


@dataclass
class CrmConfig:
    adapter: str = "mock"
    base_url: str = "https://www.eleadcrm.com"
    headless: bool = True
    selectors_file: str = "selectors.eleads.toml"
    write_strategy_notes: bool = True
    credential_service: str = "titan-eleads"


@dataclass
class Settings:
    identity: IdentityConfig = field(default_factory=IdentityConfig)
    time: TimeConfig = field(default_factory=TimeConfig)
    approval: ApprovalConfig = field(default_factory=ApprovalConfig)
    sla: SlaConfig = field(default_factory=SlaConfig)
    cadence: CadenceConfig = field(default_factory=CadenceConfig)
    messaging: MessagingConfig = field(default_factory=MessagingConfig)
    resources: ResourceConfig = field(default_factory=ResourceConfig)
    storage: StorageConfig = field(default_factory=StorageConfig)
    crm: CrmConfig = field(default_factory=CrmConfig)

    def validate(self) -> None:
        if self.approval.mode not in {"every_send", "batch"}:
            raise ValueError("approval.mode must be 'every_send' or 'batch'")
        overlap = set(self.identity.allowed_rooftops) & set(self.identity.blocked_rooftops)
        if overlap:
            raise ValueError(f"Rooftops cannot be both allowed and blocked: {sorted(overlap)}")
        if self.crm.adapter not in {"mock", "playwright"}:
            raise ValueError("crm.adapter must be 'mock' or 'playwright'")


def _coerce(target: Any, data: dict[str, Any]) -> None:
    for f in fields(target):
        if f.name not in data:
            continue
        raw = data[f.name]
        current = getattr(target, f.name)
        if is_dataclass(current):
            _coerce(current, raw)
        elif isinstance(current, time):
            setattr(target, f.name, _t(raw))
        elif isinstance(current, Path):
            setattr(target, f.name, Path(raw))
        elif f.name == "dealership_hours":
            setattr(target, f.name, {int(k): (_t(v[0]), _t(v[1])) for k, v in raw.items()})
        else:
            setattr(target, f.name, raw)


def load_settings(path: str | Path | None = None) -> Settings:
    settings = Settings()
    candidate = Path(path) if path else default_data_dir() / "config.toml"
    if candidate.exists():
        with candidate.open("rb") as fh:
            _coerce(settings, tomllib.load(fh))
    elif path:
        raise FileNotFoundError(candidate)
    selectors = Path(settings.crm.selectors_file)
    if not selectors.is_absolute():
        settings.crm.selectors_file = str(candidate.parent / selectors)
    settings.validate()
    return settings
