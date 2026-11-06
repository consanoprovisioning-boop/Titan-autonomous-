@echo off
setlocal
REM Enroll this Windows PC as a Titan COS cluster node.
REM Does not start Titan.ChiefOfStaff.exe. Does not open Eleads. Does not type a password.

set "NODE=%~1"
if "%NODE%"=="" (
  echo Usage: ENROLL-NODE.cmd Alienware-18^|Desktop3^|Desktop4^|Desktop5^|Desktop6^|Node3^|Node4^|Node5^|Node6^|Node7^|Node8
  echo Run this on the PC you are enrolling. Cloud Linux cannot enroll a Windows PC.
  echo Desktop4 is named Node4. Desktop5 is named Node5. Desktop6 is named Node6.
  echo Node3 is still unnamed. Node7-Node8 are unnamed 8-PC overflow.
  if /i not "%TITAN_INSTALL_NOPAUSE%"=="1" pause
  exit /b 1
)

set ROOT=%LOCALAPPDATA%\TitanChief
mkdir "%ROOT%" 2>nul

if /i "%NODE%"=="Node4" set "NODE=Desktop4"
if /i "%NODE%"=="Node5" set "NODE=Desktop5"
if /i "%NODE%"=="Node6" set "NODE=Desktop6"
if /i "%NODE%"=="Alienware-18" set ROOF=28206
if /i "%NODE%"=="Desktop3" set ROOF=28546
if /i "%NODE%"=="Desktop4" set ROOF=28546
if /i "%NODE%"=="Desktop5" set ROOF=28206
if /i "%NODE%"=="Desktop6" set ROOF=28546
if /i "%NODE%"=="Node3" set ROOF=28206
if /i "%NODE%"=="Node7" set ROOF=28206+28546
if /i "%NODE%"=="Node8" set ROOF=28206+28546

if "%ROOF%"=="" (
  echo Unknown node %NODE%. Use Alienware-18, Desktop3, Desktop4, Desktop5, Desktop6, or Node3 through Node8.
  if /i not "%TITAN_INSTALL_NOPAUSE%"=="1" pause
  exit /b 1
)

> "%ROOT%\node.json" echo {"nodeId":"%NODE%","rooftop":"%ROOF%","identity":"Bordine, Glenn","forbiddenRooftop":"6220","clusterMain":"Alienware-18"}
if exist "%~dp0cluster-hold.json" copy /Y "%~dp0cluster-hold.json" "%ROOT%\cluster-hold.json" >nul
echo Enrolled %NODE% rooftop %ROOF%
echo File: %ROOT%\node.json
echo Cluster MAIN is Alienware-18. Lease host is Alienware-18, not this PC.
echo This is not Verify and not send authorization.
echo If this is Desktop4, next is START-WORKER.cmd from %%USERPROFILE%%. Desktop4 is released to join.
echo If this is Desktop5/Desktop6 or Node7-Node8, STOP: install-only until Glenn says so. Do not SCALE-OUT-8.cmd yet.
echo Never 6220. Never start the worker from C:\Windows\system32.
endlocal
