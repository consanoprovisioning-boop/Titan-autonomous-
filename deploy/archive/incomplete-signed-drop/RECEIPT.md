# Archived — superseded by `dist/titan-chief-win-x64/`

# Receipt — 2026-09-24 8:08 PM ET

Partial drop received by the cloud agent. **No launch. No Eleads. No password typed.**

## Verified (SHA-256 matches `SHA256SUMS.txt`)

| File | SHA-256 |
| --- | --- |
| `INSTALL-NOTES.md` | `7ae86e79e7ee16062e33c6bd640314b7a71a30a030677e3613a554a1a9250884` |
| `Titan.ChiefOfStaff.deps.json` | `8f119d98674b7ac97a526673a0ec69e691a5d0a02859433e943569f91f6d1873` |
| `Titan.ChiefOfStaff.runtimeconfig.json` | `97c9700542b659150b230c3578b29530fd76ab01ec66a92cd16945e0245713df` |

## Missing from this drop (listed in `SHA256SUMS.txt`, not received)

- `Titan.ChiefOfStaff.exe`
- `Titan.ChiefOfStaff.dll`
- `Titan.ChiefOfStaff.pdb`
- `e_sqlite3.dll`
- `Microsoft.Bcl.AsyncInterfaces.dll`
- `Microsoft.Data.Sqlite.dll`
- `Microsoft.Playwright.dll`
- `SQLitePCLRaw.batteries_v2.dll`
- `SQLitePCLRaw.core.dll`
- `SQLitePCLRaw.provider.e_sqlite3.dll`

This folder is **not** a complete publish. Do not xcopy it onto Alienware until the signed binaries above are present and hash-checked.

## Not filed

An uploaded `LICENSE` text was the Linux kernel GPLv2 preamble (Linus notes). It is not treated as Titan’s project license and was not copied here.

`net8.0` / `win-x64` / Playwright 1.47.0 / Microsoft.Data.Sqlite 8.0.8 per `Titan.ChiefOfStaff.deps.json`. Framework-dependent — Alienware needs the .NET 8 runtime already installed.
