from __future__ import annotations

from datetime import datetime, time, timedelta

from titan.config import TimeConfig
from titan.models import Channel


class SendWindow:
    """Effective live-send window = RTF 9-7 window ∩ dealership hours ∩ (SMS) 7:00-21:59 quiet-hours rule."""

    def __init__(self, cfg: TimeConfig):
        self.cfg = cfg

    def _bounds(self, day: datetime, channel: Channel) -> tuple[time, time] | None:
        hours = self.cfg.dealership_hours.get(day.weekday())
        if hours is None:
            return None
        start = max(hours[0], self.cfg.send_window_start)
        end = min(hours[1], self.cfg.send_window_end)
        if channel is Channel.SMS:
            start = max(start, self.cfg.sms_window_start)
            end = min(end, self.cfg.sms_window_end)
        return (start, end) if start < end else None

    def is_open(self, channel: Channel, at: datetime) -> tuple[bool, str]:
        bounds = self._bounds(at, channel)
        if bounds is None:
            return False, f"No {channel.value} send window on {at:%A}"
        start, end = bounds
        if start <= at.time() < end:
            return True, "open"
        return False, f"Outside {channel.value} send window {start:%H:%M}-{end:%H:%M} ({at:%a %H:%M})"

    def next_opening(self, channel: Channel, at: datetime) -> datetime:
        for offset in range(8):
            day = at + timedelta(days=offset)
            bounds = self._bounds(day, channel)
            if bounds is None:
                continue
            start_dt = day.replace(hour=bounds[0].hour, minute=bounds[0].minute, second=0, microsecond=0)
            end_dt = day.replace(hour=bounds[1].hour, minute=bounds[1].minute, second=0, microsecond=0)
            if offset == 0:
                if at < start_dt:
                    return start_dt
                if at < end_dt:
                    return at
                continue
            return start_dt
        raise RuntimeError("No send window configured in the next week")

    def is_business_day(self, d: datetime) -> bool:
        return d.weekday() in self.cfg.dealership_hours

    def add_working_days(self, at: datetime, days: int) -> datetime:
        current = at
        remaining = days
        while remaining > 0:
            current += timedelta(days=1)
            if self.is_business_day(current):
                remaining -= 1
        return current

    def clamp_to_business_hours(self, at: datetime) -> datetime:
        """Move a proposed task time into dealership hours (task scheduling uses store hours, not send window)."""
        for _ in range(8):
            hours = self.cfg.dealership_hours.get(at.weekday())
            if hours:
                open_dt = at.replace(hour=hours[0].hour, minute=hours[0].minute, second=0, microsecond=0)
                close_dt = at.replace(hour=hours[1].hour, minute=hours[1].minute, second=0, microsecond=0)
                if at < open_dt:
                    return open_dt
                if at < close_dt:
                    return at
            at = (at + timedelta(days=1)).replace(hour=0, minute=0, second=0, microsecond=0)
        raise RuntimeError("No dealership hours configured")
