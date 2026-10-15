# Titan-autonomous-
Autonomous CRM Chief of Staff

Titan handles Glenn Bordine's Eleads customer-contact workflow at Valdosta Nissan (28206) and Valdosta Mitsubishi (28546). It runs in the background on Windows 10/11 and:

- watches both rooftops in parallel for new Glenn-assigned leads,
- inspects each full profile, checks permissions, 72-hour holds, the 14-month screen, and opt-outs,
- drafts a personalized email and SMS with two appointment choices,
- keeps a summary, a next-best action, and SMS/email templates for every past customer,
- sends **only messages Glenn has approved**, and only inside the send window,
- verifies each send in Eleads, then completes a separate email or SMS task,
- reports CRM-confirmed activity to management.

The full design, including how the build brief was reconciled with the project goal document, is in [`docs/SPECIFICATION.md`](docs/SPECIFICATION.md).

## Quick start (dry run, no CRM access needed)

```bash
pip install -e .[dev]
titan demo --approve          # fictional customers: detect → draft → approve → send → verify → report
pytest
```

## Windows install

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install.ps1
titan credentials set         # stores the Eleads login in Windows Credential Manager
```

Then edit `%APPDATA%\Titan\config.toml`. Set `crm.adapter = "playwright"` **only after** two things are done:

1. The dealership has confirmed that Eleads browser automation is permitted.
2. `selectors.eleads.toml` has been calibrated against the live UI.

## Daily use

| Command | Purpose |
|---|---|
| `titan approvals` | Show the exact drafts waiting for approval |
| `titan approve <id>` | Approve one exact message; it sends in the next open window |
| `titan edit <id> --body-file reply.txt` | Edit a draft (sends it back for approval) |
| `titan reject <id> --reason "..."` | Reject a draft |
| `titan appointment propose ...` / `approve --id ...` | Appointment approval gate |
| `titan report` | Management report: email and SMS counted separately, CRM-confirmed |
