from __future__ import annotations

import gc
import logging
import os
import threading
import time
from contextlib import contextmanager
from typing import Callable, Iterator

from titan.config import ResourceConfig
from titan.crm.base import EleadsAdapter, ProfileSession

log = logging.getLogger(__name__)


class ResourceMonitor:
    """Tracks process RSS (agent + child browser processes) against soft/hard limits."""

    def __init__(self, cfg: ResourceConfig, rss_fn: Callable[[], float] | None = None):
        self.cfg = cfg
        self._rss_fn = rss_fn or self._process_tree_rss_mb
        self._cleanup_hooks: list[Callable[[], None]] = []

    @staticmethod
    def _process_tree_rss_mb() -> float:
        try:
            import psutil
        except ImportError:
            return 0.0
        proc = psutil.Process(os.getpid())
        total = proc.memory_info().rss
        for child in proc.children(recursive=True):
            try:
                total += child.memory_info().rss
            except psutil.Error:
                continue
        return total / (1024 * 1024)

    def register_cleanup(self, hook: Callable[[], None]) -> None:
        self._cleanup_hooks.append(hook)

    def rss_mb(self) -> float:
        return self._rss_fn()

    def check(self) -> str:
        """Returns 'ok', 'soft' (cleanup ran) or 'hard' (caller must recycle the browser)."""
        rss = self.rss_mb()
        if rss < self.cfg.ram_soft_limit_mb:
            return "ok"
        log.warning("RAM %.0f MB over soft limit; running cleanup", rss)
        for hook in self._cleanup_hooks:
            try:
                hook()
            except Exception:
                log.exception("Cleanup hook failed")
        gc.collect()
        return "hard" if self.rss_mb() >= self.cfg.ram_hard_limit_mb else "soft"


class ProfileSessionManager:
    """The only way agents may open a customer profile. Caps concurrency and always closes the tab."""

    def __init__(self, adapter: EleadsAdapter, cfg: ResourceConfig, monitor: ResourceMonitor,
                 on_close: Callable[[str, float], None] | None = None):
        self.adapter = adapter
        self.cfg = cfg
        self.monitor = monitor
        self._slots = threading.BoundedSemaphore(cfg.max_open_profiles)
        self._on_close = on_close

    @contextmanager
    def profile(self, rooftop_id: str, customer_id: str, opportunity_id: str) -> Iterator[ProfileSession]:
        if self.monitor.check() == "hard":
            raise MemoryError("RAM hard limit reached; browser must be recycled before opening profiles")
        with self._slots:
            started = time.monotonic()
            session = self.adapter.open_profile(rooftop_id, customer_id, opportunity_id)
            try:
                yield session
            finally:
                try:
                    session.close()
                finally:
                    elapsed = time.monotonic() - started
                    del session
                    if self._on_close:
                        self._on_close(customer_id, elapsed)
