@echo off
setlocal
title Desktop4 clone playbook - cloud did not copy files here
REM Prints the Glenn-local clone steps. Does not open Eleads. Does not type a password.
REM This script cannot reach Desktop4 from a cloud Linux agent. Run it ON Desktop4.
REM Preferred: copy the working install from Alienware-18 on the LAN. GitHub is fallback.

cd /d "%USERPROFILE%"
echo.
echo Cloud did not clone Titan onto this PC.
echo Run these commands on Desktop4 as Glenn, from %USERPROFILE%.
echo Never copy %%LOCALAPPDATA%%\TitanChief from Alienware-18.
echo.
echo Preferred - git clone from Alienware-18:
echo   cd /d "%USERPROFILE%"
echo   git clone "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-" Titan-autonomous-
echo   cd Titan-autonomous-
echo   git checkout cursor/eleads-cluster-4pc-7dfe
echo.
echo If that share is missing, use:
echo   git clone "\\Alienware-18\C$\Users\Glenn R Bordine\Titan-autonomous-" Titan-autonomous-
echo.
echo Or robocopy the repo (includes .git), then checkout the cluster branch:
echo   robocopy "\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-" "%USERPROFILE%\Titan-autonomous-" /E /XD node_modules bin obj
echo.
echo Fallback - GitHub:
echo   git clone https://github.com/consanoprovisioning-boop/Titan-autonomous-.git Titan-autonomous-
echo   cd Titan-autonomous-
echo   git checkout cursor/eleads-cluster-4pc-7dfe
echo.
echo One-shot after this clone (install only, then STOP):
echo   deploy\desktop4\INSTALL-COS.cmd
echo That copies or publishes dist\titan-chief-win-x64, runs INSTALL.cmd,
echo and ENROLL-NODE.cmd Desktop4. STOP there. It refuses START-WORKER.
echo Do NOT run START-WORKER.cmd. Do NOT CHECKIN-to-send.
echo Alienware-18 is MAIN. Desktop4 lane is provisional 28546 P2/P3.
echo Full playbook: docs\deploy\desktop4.md
echo Never 6220. Worker must not start in C:\Windows\system32.
echo.
pause
endlocal
