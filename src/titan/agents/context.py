from __future__ import annotations

import logging
import sys
from dataclasses import dataclass, field
from typing import Protocol

from titan.config import Settings
from titan.messaging import Composer, MessageLinter, TemplateComposer
from titan.policy.clock import Clock
from titan.policy.eligibility import EligibilityEngine
from titan.policy.identity import IdentityGuard
from titan.policy.send_window import SendWindow
from titan.storage import Store

log = logging.getLogger("titan")


class Notifier(Protocol):
    def notify(self, title: str, message: str) -> None: ...


class LogNotifier:
    def notify(self, title: str, message: str) -> None:
        log.info("[NOTIFY] %s: %s", title, message)
        if sys.platform == "win32":
            try:
                import winsound

                winsound.MessageBeep()
            except Exception:
                pass


@dataclass
class AgentContext:
    settings: Settings
    store: Store
    clock: Clock
    notifier: Notifier = field(default_factory=LogNotifier)
    composer: Composer = field(default_factory=TemplateComposer)

    def __post_init__(self) -> None:
        self.guard = IdentityGuard(self.settings.identity)
        self.window = SendWindow(self.settings.time)
        self.eligibility = EligibilityEngine(self.settings.cadence, self.guard)
        self.linter = MessageLinter(self.settings.messaging)

    @property
    def me(self) -> str:
        return self.settings.identity.salesperson_name

    def audit(self, event: str, *, rooftop_id: str | None = None, customer_id: str | None = None, **detail) -> None:
        self.store.audit(self.clock.now(), "titan", event, rooftop_id=rooftop_id, customer_id=customer_id, **detail)
