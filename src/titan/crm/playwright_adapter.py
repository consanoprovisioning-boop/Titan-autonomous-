"""Live Eleads adapter driven by Playwright (Chromium, headless by default).

Eleads does not expose a documented public API for these actions, so this adapter drives the web UI using a
selector map (selectors.eleads.toml) that must be calibrated against the dealership's Eleads build. Any action
whose selector is blank raises NotCalibrated instead of guessing. Confirm with dealership management / CDK that
browser automation of the Eleads account is permitted before enabling this adapter.
"""

from __future__ import annotations

import tomllib
from datetime import date, datetime
from pathlib import Path
from typing import Any

from titan.config import CrmConfig
from titan.crm.base import (
    CrmError,
    CrmTask,
    DashboardRow,
    DuplicateTaskWarning,
    EleadsAdapter,
    ProfileSession,
    SendReceipt,
    SessionExpired,
    WorkItem,
)
from titan.models import (
    Activity,
    ActivityKind,
    Channel,
    CustomerProfile,
    OpportunityType,
    Permissions,
    TaskKind,
)

BLOCKED_RESOURCE_TYPES = {"image", "media", "font"}


class NotCalibrated(CrmError):
    pass


class Selectors:
    def __init__(self, data: dict[str, dict[str, str]]):
        self._data = data

    @classmethod
    def load(cls, path: str | Path) -> "Selectors":
        with Path(path).open("rb") as fh:
            return cls(tomllib.load(fh))

    def __call__(self, section: str, key: str, **fmt: Any) -> str:
        value = self._data.get(section, {}).get(key, "")
        if not value:
            raise NotCalibrated(f"Selector [{section}].{key} is not calibrated")
        return value.format(**fmt) if fmt else value


def _credentials(service: str) -> tuple[str, str]:
    import keyring

    username = keyring.get_password(service, "username")
    password = keyring.get_password(service, username or "")
    if not username or not password:
        raise CrmError(f"Store Eleads credentials first: titan credentials set (service '{service}')")
    return username, password


class PlaywrightProfileSession(ProfileSession):
    def __init__(self, adapter: "PlaywrightEleadsAdapter", page, rooftop_id: str, customer_id: str,
                 opportunity_id: str):
        self.a = adapter
        self.s = adapter.sel
        self.page = page
        self.rooftop_id = rooftop_id
        self.customer_id = customer_id
        self.opportunity_id = opportunity_id

    def _text(self, section: str, key: str) -> str | None:
        loc = self.page.locator(self.s(section, key))
        return loc.first.inner_text().strip() if loc.count() else None

    def _flag(self, key: str) -> bool:
        return self.page.locator(self.s("profile", key)).count() > 0

    def refresh_activities(self) -> list[Activity]:
        self.page.reload(wait_until="domcontentloaded")
        self.page.wait_for_selector(self.s("profile", "loaded_marker"), timeout=20_000)
        return self._activities()

    def _activities(self) -> list[Activity]:
        out = []
        rows = self.page.locator(self.s("profile", "activity_row"))
        for i in range(rows.count()):
            row = rows.nth(i)
            kind = row.get_attribute(self.s("profile", "activity_kind_attr"))
            at = row.get_attribute(self.s("profile", "activity_time_attr"))
            try:
                activity_kind = ActivityKind(kind)
            except ValueError:
                continue
            out.append(Activity(
                activity_kind,
                datetime.fromisoformat(at).astimezone(self.a.tz),
                row.locator(self.s("profile", "activity_by")).inner_text().strip(),
                row.locator(self.s("profile", "activity_text")).inner_text().strip()[:500],
                row.get_attribute("data-activity-id"),
            ))
        return out

    def read_profile(self) -> CustomerProfile:
        loaded = True
        try:
            self.page.wait_for_selector(self.s("profile", "loaded_marker"), timeout=20_000)
        except Exception:
            loaded = False
        purchase = self._text("profile", "purchase_date")
        return CustomerProfile(
            customer_id=self.customer_id,
            opportunity_id=self.opportunity_id,
            rooftop_id=self.rooftop_id,
            full_name=self._text("profile", "full_name") or "",
            first_name=self._text("profile", "first_name") or "",
            assigned_salesperson=self._text("profile", "assigned") or "",
            opportunity_type=self.a.pending_types.get(self.opportunity_id, OpportunityType.DORMANT_LEAD),
            email=self._text("profile", "email"),
            phone=self._text("profile", "phone"),
            permissions=Permissions(
                email_valid=bool(self._text("profile", "email")),
                phone_valid=bool(self._text("profile", "phone")),
                email_opt_out=self._flag("flag_email_opt_out"),
                email_hard_bounce=self._flag("flag_email_bounce"),
                sms_opt_in=self._flag("flag_sms_opt_in"),
                do_not_call=self._flag("flag_do_not_call"),
                do_not_text=self._flag("flag_do_not_text"),
                do_not_contact=self._flag("flag_do_not_contact"),
            ),
            vehicle_of_interest=self._text("profile", "vehicle_of_interest"),
            owned_vehicle=self._text("profile", "owned_vehicle"),
            last_purchase_date=datetime.strptime(purchase, "%m/%d/%Y").date() if purchase else None,
            activities=self._activities() if loaded else [],
            notes=[n.strip() for n in self.page.locator(self.s("profile", "notes")).all_inner_texts()],
            history_loaded=loaded,
        )

    def _send(self, prefix: str, fields: dict[str, str]) -> SendReceipt:
        self.page.click(self.s("compose", f"{prefix}_open"))
        for key, value in fields.items():
            self.page.fill(self.s("compose", f"{prefix}_{key}"), value)
        self.page.click(self.s("compose", f"{prefix}_send"))
        banner = self.page.locator(self.s("compose", "result_banner"))
        try:
            banner.wait_for(timeout=15_000)
            return SendReceipt(True, banner.inner_text().strip())
        except Exception:
            return SendReceipt(True, "no confirmation banner")

    def send_email(self, subject: str, body: str, recipient: str) -> SendReceipt:
        return self._send("email", {"to": recipient, "subject": subject, "body": body})

    def send_sms(self, body: str, recipient: str) -> SendReceipt:
        return self._send("sms", {"to": recipient, "body": body})

    def open_tasks(self) -> list[CrmTask]:
        out = []
        rows = self.page.locator(self.s("tasks", "open_rows"))
        for i in range(rows.count()):
            row = rows.nth(i)
            out.append(CrmTask(
                row.get_attribute(self.s("tasks", "row_id_attr")) or "",
                row.get_attribute(self.s("tasks", "row_kind_attr")) or "",
                row.locator(self.s("tasks", "row_status")).inner_text().strip().lower(),
                row.locator(self.s("tasks", "row_assigned")).inner_text().strip(),
                None,
            ))
        return [t for t in out if t.status == "open"]

    def create_task(self, kind: TaskKind | Channel, due_at: datetime, note: str) -> CrmTask:
        self.page.click(self.s("tasks", "create_open"))
        self.page.select_option(self.s("tasks", "create_type"), kind.value)
        self.page.fill(self.s("tasks", "create_due"), due_at.strftime("%m/%d/%Y %I:%M %p"))
        self.page.fill(self.s("tasks", "create_note"), note)
        self.page.click(self.s("tasks", "create_save"))
        if self.page.locator(self.s("tasks", "duplicate_warning")).count():
            self.page.click(self.s("tasks", "duplicate_cancel"))
            existing = next((t for t in self.open_tasks() if t.kind == kind.value), None)
            if existing:
                raise DuplicateTaskWarning(existing)
        created = next((t for t in self.open_tasks() if t.kind == kind.value), None)
        if created is None:
            raise CrmError("Task creation not visible after save")
        return created

    def complete_task(self, crm_task_id: str, outcome_note: str) -> None:
        self.page.click(self.s("tasks", "complete_open", task_id=crm_task_id))
        self.page.select_option(self.s("tasks", "complete_outcome"), "Completed")
        self.page.fill(self.s("tasks", "complete_note"), outcome_note)
        self.page.click(self.s("tasks", "complete_save"))

    def task_status(self, crm_task_id: str) -> CrmTask:
        self.page.reload(wait_until="domcontentloaded")
        row = self.page.locator(f"[{self.s('tasks', 'row_id_attr')}='{crm_task_id}']")
        if not row.count():
            raise CrmError(f"Task {crm_task_id} not found")
        return CrmTask(crm_task_id, row.get_attribute(self.s("tasks", "row_kind_attr")) or "",
                       row.locator(self.s("tasks", "row_status")).inner_text().strip().lower(),
                       row.locator(self.s("tasks", "row_assigned")).inner_text().strip(), None)

    def add_note(self, text: str) -> None:
        self.page.click(self.s("tasks", "note_open"))
        self.page.fill(self.s("tasks", "note_text"), text)
        self.page.click(self.s("tasks", "note_save"))

    def create_sales_appointment(self, starts_at: datetime, assigned_to: str, notes: str) -> str:
        self.page.click(self.s("appointment", "open"))
        self.page.click(self.s("appointment", "type_sales"))
        self.page.fill(self.s("appointment", "datetime"), starts_at.strftime("%m/%d/%Y %I:%M %p"))
        self.page.select_option(self.s("appointment", "assigned"), label=assigned_to)
        self.page.fill(self.s("appointment", "notes"), notes)
        self.page.click(self.s("appointment", "save"))
        return f"{self.opportunity_id}@{starts_at.isoformat()}"

    def set_progress(self, status: str) -> None:
        self.page.select_option(self.s("appointment", "progress_select"), label=status)

    def progress(self) -> str:
        return self.page.locator(self.s("appointment", "progress_value")).inner_text().strip()

    def print_profile_page1(self) -> None:
        self.page.click(self.s("appointment", "print_open"))
        self.page.click(self.s("appointment", "print_page1"))

    def close(self) -> None:
        try:
            self.page.close()
        finally:
            self.a._pages.discard(self.page)


class PlaywrightEleadsAdapter(EleadsAdapter):
    def __init__(self, cfg: CrmConfig, tz):
        self.cfg = cfg
        self.tz = tz
        self.sel = Selectors.load(cfg.selectors_file)
        self._pw = None
        self._browser = None
        self._context = None
        self._main = None
        self._pages: set = set()
        self.pending_types: dict[str, OpportunityType] = {}

    def _start(self) -> None:
        if self._browser is not None:
            return
        from playwright.sync_api import sync_playwright

        self._pw = sync_playwright().start()
        self._browser = self._pw.chromium.launch(
            headless=self.cfg.headless,
            args=["--disable-extensions", "--disable-background-networking", "--disable-gpu",
                  "--js-flags=--max-old-space-size=256", "--renderer-process-limit=3"],
        )
        self._context = self._browser.new_context(viewport={"width": 1280, "height": 900})
        self._context.route("**/*", lambda route: route.abort()
                            if route.request.resource_type in BLOCKED_RESOURCE_TYPES else route.continue_())
        self._main = self._context.new_page()

    def _goto(self, path: str) -> None:
        self._main.goto(self.cfg.base_url.rstrip("/") + path, wait_until="domcontentloaded")
        if "login" in self._main.url.lower():
            raise SessionExpired("Redirected to login")

    def login(self) -> None:
        self._start()
        username, password = _credentials(self.cfg.credential_service)
        self._main.goto(self.cfg.base_url.rstrip("/") + self.sel("login", "url_path"))
        self._main.fill(self.sel("login", "username"), username)
        self._main.fill(self.sel("login", "password"), password)
        self._main.click(self.sel("login", "submit"))
        self._main.wait_for_selector(self.sel("login", "logged_in_marker"), timeout=30_000)

    def select_rooftop(self, rooftop_id: str) -> None:
        self._main.click(self.sel("identity", "rooftop_selector"))
        self._main.click(self.sel("identity", "rooftop_option", rooftop_id=rooftop_id))
        self._main.wait_for_load_state("domcontentloaded")

    def current_identity(self) -> tuple[str, str]:
        user = self._main.locator(self.sel("identity", "user_name")).first.inner_text().strip()
        rooftop = self._main.locator(self.sel("identity", "active_rooftop")).first
        return user, (rooftop.get_attribute("value") or rooftop.inner_text()).strip()

    def _rows(self, section: str, rooftop_id: str) -> list[WorkItem]:
        items = []
        rows = self._main.locator(self.sel(section, "row"))
        for i in range(rows.count()):
            row = rows.nth(i)
            try:
                otype = OpportunityType(row.locator(self.sel("organizer", "row_type")).inner_text().strip().lower())
            except ValueError:
                otype = OpportunityType.DORMANT_LEAD
            item = WorkItem(
                rooftop_id,
                row.get_attribute(self.sel("organizer", "row_customer_id_attr")) or "",
                row.get_attribute(self.sel("organizer", "row_opportunity_id_attr")) or "",
                row.locator(self.sel("organizer", "row_name")).inner_text().strip(),
                otype,
                row.locator(self.sel("organizer", "row_assigned")).inner_text().strip(),
            )
            self.pending_types[item.opportunity_id] = otype
            items.append(item)
        return items

    def daily_organizer(self, rooftop_id: str) -> list[WorkItem]:
        self._goto(self.sel("organizer", "url_path"))
        self._main.click(self.sel("organizer", "status_open"))
        self._main.click(self.sel("organizer", "contact_type_all"))
        return self._rows("organizer", rooftop_id)

    def new_leads(self, rooftop_id: str, salesperson: str, since: datetime) -> list[WorkItem]:
        self._goto(self.sel("leads", "url_path"))
        return [i for i in self._rows("leads", rooftop_id) if i.assigned_to.lower() == salesperson.lower()]

    def past_customers(self, rooftop_id: str, cursor: str | None, limit: int) -> tuple[list[WorkItem], str | None]:
        offset = int(cursor or 0)
        self._goto(self.sel("past_customers", "url_path", offset=offset, limit=limit))
        items = self._rows("past_customers", rooftop_id)
        return items, (str(offset + limit) if len(items) == limit else None)

    def open_profile(self, rooftop_id: str, customer_id: str, opportunity_id: str) -> ProfileSession:
        page = self._context.new_page()
        self._pages.add(page)
        try:
            page.goto(self.cfg.base_url.rstrip("/") + self.sel(
                "profile", "url_path", customer_id=customer_id, opportunity_id=opportunity_id),
                wait_until="domcontentloaded")
        except Exception:
            page.close()
            self._pages.discard(page)
            raise
        return PlaywrightProfileSession(self, page, rooftop_id, customer_id, opportunity_id)

    def daily_activity(self, rooftop_id: str, salesperson: str, day: date) -> list[DashboardRow]:
        self._goto(self.sel("dashboard", "url_path", date=day.strftime("%m/%d/%Y"), rooftop_id=rooftop_id))
        rows = self._main.locator(self.sel("dashboard", "row"))
        out = []
        for i in range(rows.count()):
            row = rows.nth(i)
            out.append(DashboardRow(
                rooftop_id, row.get_attribute("data-salesperson") or "", row.get_attribute("data-type") or "",
                row.get_attribute("data-customer-id") or "", row.get_attribute("data-task-id"),
                datetime.fromisoformat(row.get_attribute("data-completed") or datetime.now(self.tz).isoformat()),
            ))
        return [r for r in out if r.salesperson.lower() == salesperson.lower()]

    def open_tab_count(self) -> int:
        return len(self._pages)

    def shutdown(self) -> None:
        for page in list(self._pages):
            try:
                page.close()
            except Exception:
                pass
        self._pages.clear()
        for closer in (self._context, self._browser):
            try:
                if closer is not None:
                    closer.close()
            except Exception:
                pass
        if self._pw is not None:
            self._pw.stop()
        self._pw = self._browser = self._context = self._main = None
