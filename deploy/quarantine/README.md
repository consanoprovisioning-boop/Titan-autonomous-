# Quarantine — do not run

These uploads are **not** the Glenn-approved Alienware signed install.

| Upload | SHA-256 | What it is |
| --- | --- | --- |
| `titanchiefofstaff_8f46.txt` | `1b42827a31f20ceb0d0faae9f732e4fe40dad545935180d08591b0b50a8157e1` | PowerShell self-extractor. Sets process ExecutionPolicy Bypass. Writes `%LOCALAPPDATA%\TitanAgent\app` and expands `TitanAgent-ChiefHost.zip` (Unblock-Eleads, Hide-EleadsOverlay, Launch-Titan.bat, FIX.ps1). |
| `titan_0921_Powershell_doc_0657.txt` | `3526a395ec5bf22c961c4d124c4673500c667f75819089ce578178ba5dc3c45f` | Same class of extractor (shorter zip). Same Bypass + TitanAgent path. |

`docs/deploy/alienware-18.md` forbids PowerShell self-extractors, execution-policy Bypass, launch, Eleads, and password typing. The live host path is `%LOCALAPPDATA%\TitanChief\`, not `TitanAgent`.

Do not decode, expand, or execute either file on Alienware or in this environment. Do not create Desktop or Start Menu shortcuts from them.

The signed publish (when complete) is `deploy/alienware-install-fd/` and still missing `Titan.ChiefOfStaff.exe` and dependency DLLs as of the last receipt.
