@echo off
REM Fail-closed cluster launch hold. Called from START-WORKER scripts.
REM Does not start Titan.ChiefOfStaff.exe. Does not open Eleads. Does not type a password.
REM TITAN_CLUSTER_HOLD_LAUNCH=off is the only override after Glenn releases the cluster.

if /i "%TITAN_CLUSTER_HOLD_LAUNCH%"=="off" exit /b 0
if /i "%TITAN_CLUSTER_HOLD_LAUNCH%"=="on" goto :held

set "HOLDFILE=%~1"
if "%HOLDFILE%"=="" set "HOLDFILE=%~dp0cluster-hold.json"
if exist "%LOCALAPPDATA%\TitanChief\cluster-hold.json" set "HOLDFILE=%LOCALAPPDATA%\TitanChief\cluster-hold.json"

if exist "%HOLDFILE%" (
  findstr /i /c:"\"holdLaunch\": false" "%HOLDFILE%" >nul
  if not errorlevel 1 exit /b 0
)

:held
echo.
echo HOLD: Cluster launch is held. Glenn has not said all PCs are ready and online.
echo Alienware-18 is the MAIN computer (coordinator, clone source, lease host).
echo Install and enroll only. Do NOT start the Cursor worker.
echo Do NOT CHECKIN-to-send. Do NOT place COS. Desktop4 is not the coordinator.
echo Provisional Desktop4 lane is Mitsubishi 28546 P2/P3 after Glenn releases the cluster.
echo.
exit /b 2
