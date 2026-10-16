#!/usr/bin/env bash
# Cloud attach for the Eleads COS loop.
# Does not open Eleads. Does not start the Windows host.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
exec python3 "$ROOT/eleads_cos_cloud_launch.py" "$@"
