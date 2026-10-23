#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
cd "$ROOT"
dotnet test Titan.ChiefOfStaff.sln -c Release
OUT="$ROOT/dist/titan-chief-win-x64"
rm -rf "$OUT"
dotnet publish src/TitanCOS/TitanCOS.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "$OUT"
find "$OUT" -name '*.ps1' -delete
cp -f "$ROOT/deploy/alienware-local-program/INSTALL.cmd" "$OUT/INSTALL.cmd"
cp -f "$ROOT/deploy/alienware-local-program/Titan.cmd" "$OUT/Titan.cmd"
cp -f "$ROOT/deploy/alienware-local-program/CHECKIN.cmd" "$OUT/CHECKIN.cmd"
cp -f "$ROOT/deploy/alienware-local-program/TICK.cmd" "$OUT/TICK.cmd"
cp -f "$ROOT/deploy/alienware-local-program/COS.cmd" "$OUT/COS.cmd"
cp -f "$ROOT/deploy/alienware-local-program/START-HERE.txt" "$OUT/START-HERE.txt"
(
  cd "$OUT"
  sha256sum TitanCOS.exe Titan.ChiefOfStaff.exe > SHA256SUMS.txt
  if [ -f e_sqlite3.dll ]; then
    sha256sum e_sqlite3.dll >> SHA256SUMS.txt
  fi
)
echo "Published $OUT"
echo "Double-click TitanCOS.exe on Windows 10/11. Do not start it on this cloud host."
