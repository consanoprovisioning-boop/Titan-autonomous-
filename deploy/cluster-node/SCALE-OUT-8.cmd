@echo off
setlocal
REM Arm the 8-PC scale-out on THIS PC. Does not open Eleads. Does not start Titan.
REM Narrows Alienware-18 / Desktop3 to P1 email after Nodes 5-8 exist.
REM Prefer a shared TITAN_LEASE_DIR so all eight PCs see the same leases.

set ROOT=%LOCALAPPDATA%\TitanChief
mkdir "%ROOT%" 2>nul
> "%ROOT%\cluster-mode.json" echo {"scaleOut":true,"size":8,"identity":"Bordine, Glenn","allowedRooftops":["28206","28546"],"forbiddenRooftop":"6220"}
echo Wrote %ROOT%\cluster-mode.json

if not "%TITAN_LEASE_DIR%"=="" (
  mkdir "%TITAN_LEASE_DIR%" 2>nul
  > "%TITAN_LEASE_DIR%\cluster-mode.json" echo {"scaleOut":true,"size":8,"identity":"Bordine, Glenn","allowedRooftops":["28206","28546"],"forbiddenRooftop":"6220"}
  echo Wrote %TITAN_LEASE_DIR%\cluster-mode.json
) else (
  echo NOTE: TITAN_LEASE_DIR is not set. At 8 PCs, set it to one shared folder
  echo so customer leases are visible to every node. Local-only leases cannot
  echo see each other.
)

echo.
echo Scale-out armed on this PC. 4-PC go-live node ids are unchanged.
echo PC1/PC2 now take P1 email only. Node5/6=calls, Node7=SMS, Node8=appointments.
echo This is not Verify and not send authorization. Never 6220.
echo Playbook: docs\deploy\cluster-8pc.md
endlocal
