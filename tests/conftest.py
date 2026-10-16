from __future__ import annotations

from datetime import datetime
from zoneinfo import ZoneInfo

import pytest

from titan.agents.context import AgentContext
from titan.agents.orchestrator import Orchestrator
from titan.config import Settings
from titan.crm.mock import MockCrm, MockEleadsAdapter
from titan.models import CustomerProfile, OpportunityType, Permissions
from titan.policy.clock import FrozenClock
from titan.storage import Store

TZ = ZoneInfo("America/New_York")
ME = "Bordine, Glenn"
NISSAN = "28206"
MITSU = "28546"
FULL = Permissions(email_valid=True, phone_valid=True, sms_opt_in=True)


def at(y, mo, d, h=10, mi=0) -> datetime:
    return datetime(y, mo, d, h, mi, tzinfo=TZ)


class Silent:
    def __init__(self):
        self.sent = []

    def notify(self, title, message):
        self.sent.append((title, message))


@pytest.fixture
def clock():
    return FrozenClock(at(2026, 10, 15, 10, 0))  # Thursday


@pytest.fixture
def settings(tmp_path):
    s = Settings()
    s.storage.db_path = tmp_path / "t.db"
    return s


@pytest.fixture
def ctx(settings, clock):
    return AgentContext(settings, Store(":memory:"), clock, notifier=Silent())


@pytest.fixture
def crm(clock):
    return MockCrm(ME, now=clock.now)


@pytest.fixture
def orch(ctx, crm):
    adapters = {}

    def factory(rid):
        adapters[rid] = MockEleadsAdapter(crm)
        return adapters[rid]

    o = Orchestrator(ctx, factory)
    o.adapters = adapters
    return o


def profile(cid="C1", rooftop=NISSAN, otype=OpportunityType.NEW_LEAD, **kw) -> CustomerProfile:
    base = dict(
        customer_id=cid, opportunity_id="O" + cid, rooftop_id=rooftop, full_name="Dana Whitfield",
        first_name="Dana", assigned_salesperson=ME, opportunity_type=otype, email=f"{cid}@example.com",
        phone="+12295550100", permissions=FULL, vehicle_of_interest="2026 Nissan Rogue SV", history_loaded=True,
    )
    base.update(kw)
    return CustomerProfile(**base)
