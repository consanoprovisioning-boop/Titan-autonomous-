from __future__ import annotations

from datetime import datetime
from typing import Callable
from zoneinfo import ZoneInfo


class Clock:
    """Injectable time source so every timing rule is testable."""

    def __init__(self, tz: str = "America/New_York", now_fn: Callable[[], datetime] | None = None):
        self.tz = ZoneInfo(tz)
        self._now_fn = now_fn

    def now(self) -> datetime:
        if self._now_fn is not None:
            value = self._now_fn()
            return value if value.tzinfo else value.replace(tzinfo=self.tz)
        return datetime.now(self.tz)


class FrozenClock(Clock):
    def __init__(self, at: datetime, tz: str = "America/New_York"):
        super().__init__(tz)
        self._at = at if at.tzinfo else at.replace(tzinfo=self.tz)

    def now(self) -> datetime:
        return self._at

    def set(self, at: datetime) -> None:
        self._at = at if at.tzinfo else at.replace(tzinfo=self.tz)

    def advance(self, **delta) -> None:
        from datetime import timedelta

        self._at = self._at + timedelta(**delta)
