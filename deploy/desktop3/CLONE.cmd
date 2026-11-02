@echo off
setlocal
title Desktop3 clone playbook - cloud did not copy files here
REM Prints the Glenn-local clone steps. Does not open Eleads. Does not type a password.
REM This script cannot reach Desktop3 from a cloud Linux agent. Run it ON Desktop3.

cd /d "%USERPROFILE%"
echo.
echo Cloud did not clone Titan onto this PC.
echo Run these commands on Desktop3 as Glenn, from %USERPROFILE%.
echo.
echo   cd /d "%USERPROFILE%"
echo   git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
echo   cd Titan-autonomous-
echo   git checkout cursor/eleads-cluster-4pc-7dfe
echo.
echo Then publish or copy dist\titan-chief-win-x64, run INSTALL.cmd,
echo ENROLL-NODE.cmd Desktop3, and START-WORKER.cmd.
echo Full playbook: docs\deploy\desktop3.md
echo Rooftop 28546 only. Never 6220. Worker must not start in C:\Windows\system32.
echo.
pause
endlocal
