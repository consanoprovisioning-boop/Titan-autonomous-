@echo off
setlocal
REM Enroll this Windows PC as a Titan COS cluster node.
REM Does not start Titan.ChiefOfStaff.exe. Does not open Eleads. Does not type a password.

set "NODE=%~1"
if "%NODE%"=="" (
  echo Usage: ENROLL-NODE.cmd Alienware-18^|Desktop3^|Desktop4^|Node3^|Node4^|Node5^|Node6^|Node7^|Node8
  echo Run this on the PC you are enrolling. Cloud Linux cannot enroll a Windows PC.
  echo Desktop4 is named Node4. Node3 is still unnamed. Node5-Node8 are 8-PC overflow.
  pause
  exit /b 1
)

set ROOT=%LOCALAPPDATA%\TitanChief
mkdir "%ROOT%" 2>nul

if /i "%NODE%"=="Node4" set "NODE=Desktop4"
if /i "%NODE%"=="Alienware-18" set ROOF=28206
if /i "%NODE%"=="Desktop3" set ROOF=28546
if /i "%NODE%"=="Desktop4" set ROOF=28546
if /i "%NODE%"=="Node3" set ROOF=28206
if /i "%NODE%"=="Node5" set ROOF=28206
if /i "%NODE%"=="Node6" set ROOF=28546
if /i "%NODE%"=="Node7" set ROOF=28206+28546
if /i "%NODE%"=="Node8" set ROOF=28206+28546

if "%ROOF%"=="" (
  echo Unknown node %NODE%. Use Alienware-18, Desktop3, Desktop4, or Node3 through Node8.
  pause
  exit /b 1
)

> "%ROOT%\node.json" echo {"nodeId":"%NODE%","rooftop":"%ROOF%","identity":"Bordine, Glenn","forbiddenRooftop":"6220"}
echo Enrolled %NODE% rooftop %ROOF%
echo File: %ROOT%\node.json
echo This is not Verify and not send authorization.
echo Next: START-WORKER.cmd from %%USERPROFILE%%, then CHECKIN.cmd twice.
echo If this is Node5-Node8, run SCALE-OUT-8.cmd on every PC and set TITAN_LEASE_DIR.
echo Never 6220. Never start the worker from C:\Windows\system32.
endlocal
