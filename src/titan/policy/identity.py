from __future__ import annotations

from titan.config import IdentityConfig


class ScopeViolation(RuntimeError):
    """Raised when an action would run under the wrong user or rooftop. Never caught silently."""


class IdentityGuard:
    def __init__(self, cfg: IdentityConfig):
        self.cfg = cfg

    def check_rooftop(self, rooftop_id: str) -> None:
        if rooftop_id in self.cfg.blocked_rooftops:
            raise ScopeViolation(f"Rooftop {rooftop_id} is blocked for this project")
        if rooftop_id not in self.cfg.allowed_rooftops:
            raise ScopeViolation(f"Rooftop {rooftop_id} is not an authorized rooftop")

    def verify_session(self, *, expected_rooftop: str, user_name: str, rooftop_id: str) -> None:
        self.check_rooftop(expected_rooftop)
        if rooftop_id != expected_rooftop:
            raise ScopeViolation(f"CRM shows rooftop {rooftop_id}; expected {expected_rooftop}")
        if user_name.strip().lower() != self.cfg.salesperson_name.lower():
            raise ScopeViolation(f"CRM user is '{user_name}'; expected '{self.cfg.salesperson_name}'")

    def is_own(self, assigned_salesperson: str) -> bool:
        return assigned_salesperson.strip().lower() == self.cfg.salesperson_name.lower()

    def rooftop_name(self, rooftop_id: str) -> str:
        return self.cfg.allowed_rooftops.get(rooftop_id, rooftop_id)
