@echo off
REM Fail-closed cluster launch hold. Called from START-WORKER scripts.
REM %1 = cluster-hold.json (repo copy). %2 = node id (Desktop4, Desktop5, ...).
REM Does not start Titan.ChiefOfStaff.exe. Does not open Eleads. Does not type a password.
REM TITAN_CLUSTER_HOLD_LAUNCH=off is the only global override.
REM Per-node: if releasedNodes contains this node, START-WORKER is allowed even while holdLaunch=true.

if /i "%TITAN_CLUSTER_HOLD_LAUNCH%"=="off" exit /b 0
if /i "%TITAN_CLUSTER_HOLD_LAUNCH%"=="on" goto :held

set "REPOHOLD=%~1"
set "NODE=%~2"
if /i "%NODE%"=="Node4" set "NODE=Desktop4"
if /i "%NODE%"=="Node5" set "NODE=Desktop5"
if /i "%NODE%"=="Node6" set "NODE=Desktop6"
if "%REPOHOLD%"=="" set "REPOHOLD=%~dp0cluster-hold.json"
set "HOLDFILE=%REPOHOLD%"
if exist "%LOCALAPPDATA%\TitanChief\cluster-hold.json" set "HOLDFILE=%LOCALAPPDATA%\TitanChief\cluster-hold.json"

if exist "%HOLDFILE%" (
  findstr /i /c:"\"holdLaunch\": false" "%HOLDFILE%" >nul
  if not errorlevel 1 exit /b 0
)
if exist "%REPOHOLD%" (
  findstr /i /c:"\"holdLaunch\": false" "%REPOHOLD%" >nul
  if not errorlevel 1 exit /b 0
)

if "%NODE%"=="" goto :held
if exist "%HOLDFILE%" (
  findstr /i /c:"\"releasedNodes\"" "%HOLDFILE%" | findstr /i /c:"\"%NODE%\"" >nul
  if not errorlevel 1 goto :released
)
if exist "%REPOHOLD%" (
  findstr /i /c:"\"releasedNodes\"" "%REPOHOLD%" | findstr /i /c:"\"%NODE%\"" >nul
  if not errorlevel 1 goto :released
)
goto :held

:released
echo.
echo RELEASE: %NODE% may START-WORKER and join the cluster.
echo clusterHoldLaunch remains true for nodes Glenn has not released.
echo Desktop5 and Desktop6 stay install-only until he says so.
echo Alienware-18 is MAIN. Rooftops 28206 and 28546 only. Never 6220.
echo Identity Bordine, Glenn. Directory must be %%USERPROFILE%%, never C:\Windows\system32.
echo This is not Verify and not a send.
echo.
exit /b 0

:held
echo.
echo HOLD: Cluster launch is held for this PC. Glenn has not released this node.
echo Alienware-18 is the MAIN computer (coordinator, clone source, lease host).
echo Install and enroll only on unreleased nodes. Do NOT start the Cursor worker here.
echo Desktop4 is released separately. Desktop5 and Desktop6 stay install-only.
echo Do NOT CHECKIN-to-send. Do NOT place COS on a held node.
echo.
exit /b 2
