#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
cd "$ROOT"
dotnet test Titan.ChiefOfStaff.sln -c Release
dotnet publish src/Titan.ChiefOfStaff/Titan.ChiefOfStaff.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o "$ROOT/dist/titan-chief-win-x64"
dotnet publish src/Titan.Launcher/Titan.Launcher.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "$ROOT/dist/titan-chief-win-x64"
find "$ROOT/dist/titan-chief-win-x64" -name '*.ps1' -delete
cp -f "$ROOT/deploy/alienware-local-program/INSTALL.cmd" "$ROOT/dist/titan-chief-win-x64/INSTALL.cmd"
cp -f "$ROOT/deploy/alienware-local-program/Titan.cmd" "$ROOT/dist/titan-chief-win-x64/Titan.cmd"
cp -f "$ROOT/deploy/alienware-local-program/CHECKIN.cmd" "$ROOT/dist/titan-chief-win-x64/CHECKIN.cmd"
cp -f "$ROOT/deploy/alienware-local-program/TICK.cmd" "$ROOT/dist/titan-chief-win-x64/TICK.cmd"
cp -f "$ROOT/deploy/alienware-local-program/LOOP.cmd" "$ROOT/dist/titan-chief-win-x64/LOOP.cmd"
cp -f "$ROOT/deploy/alienware-local-program/Titan COS.cmd" "$ROOT/dist/titan-chief-win-x64/Titan COS.cmd"
cp -f "$ROOT/deploy/alienware-local-program/CREATE-HOME-SCREEN-APP.cmd" "$ROOT/dist/titan-chief-win-x64/CREATE-HOME-SCREEN-APP.cmd"
cp -f "$ROOT/deploy/alienware-local-program/START-HERE.txt" "$ROOT/dist/titan-chief-win-x64/START-HERE.txt"
(
  cd "$ROOT/dist/titan-chief-win-x64"
  sha256sum Titan.ChiefOfStaff.exe Titan.ChiefOfStaff.dll Titan.ChiefOfStaff.deps.json Titan.ChiefOfStaff.runtimeconfig.json "Titan COS.exe" > SHA256SUMS.txt
  if [ -f e_sqlite3.dll ]; then
    sha256sum e_sqlite3.dll >> SHA256SUMS.txt
  fi
  if [ -f "Titan COS.dll" ]; then
    sha256sum "Titan COS.dll" >> SHA256SUMS.txt
  fi
)
echo "Published $ROOT/dist/titan-chief-win-x64"
echo "Do not start Titan.ChiefOfStaff.exe on this cloud host."
