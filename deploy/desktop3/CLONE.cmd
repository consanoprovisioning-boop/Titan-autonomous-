@echo off
setlocal
title Desktop3 clone playbook - cloud did not copy files here
REM Prints the Glenn-local clone steps. Does not open Eleads. Does not type a password.
REM This script cannot reach Desktop3 from a cloud Linux agent. Run it ON Desktop3.

cd /d "%USERPROFILE%"
echo.
echo Cloud did not clone Titan onto this PC.
echo Run these commands on Desktop3 as Glenn, from %USERPROFILE%.
echo Alienware-18 is MAIN. Clone from Alienware-18. Do not launch until Glenn says all PCs are ready.
echo.
echo Preferred - git clone from Alienware-18:
echo   git clone "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-" Titan-autonomous-
echo.
echo Fallback - GitHub:
echo   cd /d "%USERPROFILE%"
echo   git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
echo   cd Titan-autonomous-
echo   git checkout cursor/eleads-cluster-4pc-7dfe
echo.
echo Then copy dist from Alienware-18 or publish, run INSTALL.cmd,
echo and ENROLL-NODE.cmd Desktop3. Do NOT run START-WORKER.cmd yet.
echo Full playbook: docs\deploy\desktop3.md
echo Rooftop 28546 only. Never 6220. Worker must not start in C:\Windows\system32.
echo.
pause
endlocal
