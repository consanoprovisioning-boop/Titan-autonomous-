# Installs Titan for the current Windows user and registers it to start at logon.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $env:APPDATA "Titan"
$venv = Join-Path $dataDir "venv"

New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
if (-not (Test-Path $venv)) { py -3 -m venv $venv }
$py = Join-Path $venv "Scripts\python.exe"

& $py -m pip install --upgrade pip
& $py -m pip install "$root[browser]"
& $py -m playwright install chromium

$config = Join-Path $dataDir "config.toml"
if (-not (Test-Path $config)) {
    Copy-Item (Join-Path $root "config.example.toml") $config
    Write-Host "Created $config - review it before switching crm.adapter to 'playwright'."
}
$selectors = Join-Path $dataDir "selectors.eleads.toml"
if (-not (Test-Path $selectors)) {
    Copy-Item (Join-Path $root "selectors.eleads.example.toml") $selectors
}

& $py -m titan.cli --config $config startup install
Write-Host "Done. Store credentials with: $py -m titan.cli credentials set"
