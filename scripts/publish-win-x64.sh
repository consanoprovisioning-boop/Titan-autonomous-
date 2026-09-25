#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
cd "$ROOT"
dotnet test Titan.ChiefOfStaff.sln -c Release
dotnet publish src/Titan.ChiefOfStaff/Titan.ChiefOfStaff.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o "$ROOT/dist/titan-chief-win-x64"
rm -rf "$ROOT/dist/titan-chief-win-x64/.playwright"
rm -f "$ROOT/dist/titan-chief-win-x64/"*.ps1
cp -f "$ROOT/deploy/alienware-local-program/INSTALL.cmd" "$ROOT/dist/titan-chief-win-x64/INSTALL.cmd"
cp -f "$ROOT/deploy/alienware-local-program/Titan.cmd" "$ROOT/dist/titan-chief-win-x64/Titan.cmd"
(
  cd "$ROOT/dist/titan-chief-win-x64"
  sha256sum Titan.ChiefOfStaff.exe Titan.ChiefOfStaff.dll Titan.ChiefOfStaff.deps.json Titan.ChiefOfStaff.runtimeconfig.json e_sqlite3.dll > SHA256SUMS.txt
)
echo "Published $ROOT/dist/titan-chief-win-x64"
