@echo off
setlocal
title Desktop4 Titan COS install - then START-WORKER
REM One-shot INSTALL only for Desktop4. cmd.exe only. No PowerShell.
REM Clones from Alienware-18 if the repo is missing, checks out the cluster
REM branch, copies or publishes dist\titan-chief-win-x64, runs INSTALL.cmd,
REM runs ENROLL-NODE.cmd Desktop4, then STOPS.
REM Does not start Titan.ChiefOfStaff.exe. Does not open Eleads.
REM Does not type a password. Does not START-WORKER itself. After install, Glenn runs START-WORKER.cmd.

if /i "%~1"=="START-WORKER" goto :refuse_start
if /i "%~1"=="START-WORKER.cmd" goto :refuse_start
if /i "%~2"=="START-WORKER" goto :refuse_start
if /i "%~2"=="START-WORKER.cmd" goto :refuse_start
echo %* | findstr /i /c:"START-WORKER" >nul
if not errorlevel 1 goto :refuse_start

if "%USERPROFILE%"=="" (
  echo ERROR: USERPROFILE is empty. This installer requires %%USERPROFILE%%.
  echo Never start from C:\Windows\system32.
  goto :fail
)

cd /d "%USERPROFILE%"
if errorlevel 1 (
  echo ERROR: Could not cd to %%USERPROFILE%%.
  goto :fail
)
if /i "%CD%"=="C:\Windows\System32" goto :refuse_system32
echo Working directory: %CD%
echo This directory must show %USERPROFILE% - not C:\Windows\system32.
echo.

set "TITAN_INSTALL_NOPAUSE=1"
set "REPO=%USERPROFILE%\Titan-autonomous-"
set "BRANCH=cursor/eleads-cluster-4pc-7dfe"
set "AW_REPO=\\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-"
set "AW_REPO_ADMIN=\\Alienware-18\C$\Users\Glenn R Bordine\Titan-autonomous-"
set "GH=https://github.com/consanoprovisioning-boop/Titan-autonomous-.git"
set "SRC_REPO=%~dp0..\.."
set "DIST=%REPO%\dist\titan-chief-win-x64"
set "AW_DIST=%AW_REPO%\dist\titan-chief-win-x64"

echo Desktop4 Titan COS install. This PC is released to join the cluster.
echo Cluster MAIN is Alienware-18. Rooftops 28206 and 28546 only. Never 6220.
echo Identity Bordine, Glenn. holdLaunch=true for unreleased nodes. releasedNodes=Desktop4.
echo This installer does not start the worker. This is not Verify.
echo.

REM --- clone into %%USERPROFILE%% if the repo is missing ---
if exist "%REPO%\.git" goto :have_repo
if exist "%REPO%\deploy\cluster-node\ENROLL-NODE.cmd" goto :have_repo
if exist "%REPO%\Titan.ChiefOfStaff.sln" goto :have_repo

echo Repo missing at %REPO%. Cloning from Alienware-18...
where git >nul 2>nul
if errorlevel 1 goto :clone_robocopy

git clone --branch %BRANCH% "%AW_REPO%" "%REPO%"
if not errorlevel 1 goto :have_repo
git clone --branch %BRANCH% "%AW_REPO_ADMIN%" "%REPO%"
if not errorlevel 1 goto :have_repo
if exist "%SRC_REPO%\.git" (
  git clone --branch %BRANCH% "%SRC_REPO%" "%REPO%"
  if not errorlevel 1 goto :have_repo
)
echo Alienware-18 clone failed. GitHub fallback...
git clone --branch %BRANCH% "%GH%" "%REPO%"
if not errorlevel 1 goto :have_repo

:clone_robocopy
echo git clone failed or git is missing. Robocopy from Alienware-18...
if exist "%REPO%" (
  echo ERROR: %REPO% exists but is not a Titan checkout. Not deleting it.
  echo Fix or rename that folder, then rerun INSTALL-COS.cmd from %%USERPROFILE%%.
  goto :fail
)
robocopy "%AW_REPO%" "%REPO%" /E /XD node_modules bin obj
if exist "%REPO%\deploy\cluster-node\ENROLL-NODE.cmd" goto :have_repo
robocopy "%AW_REPO_ADMIN%" "%REPO%" /E /XD node_modules bin obj
if exist "%REPO%\deploy\cluster-node\ENROLL-NODE.cmd" goto :have_repo
if exist "%SRC_REPO%\deploy\cluster-node\ENROLL-NODE.cmd" (
  robocopy "%SRC_REPO%" "%REPO%" /E /XD node_modules bin obj
)
if not exist "%REPO%\deploy\cluster-node\ENROLL-NODE.cmd" (
  echo ERROR: Could not clone Titan onto this PC from Alienware-18 or GitHub.
  echo Preferred: git clone from \\Alienware-18\Users\Glenn R Bordine\Titan-autonomous-
  goto :fail
)

:have_repo
cd /d "%REPO%"
if errorlevel 1 (
  echo ERROR: Could not cd to %REPO%.
  goto :fail
)
if /i "%CD%"=="C:\Windows\System32" goto :refuse_system32

where git >nul 2>nul
if errorlevel 1 goto :have_branch
if exist "%REPO%\.git" (
  echo Checking out %BRANCH% ...
  git fetch origin %BRANCH%
  git checkout %BRANCH%
  if errorlevel 1 echo WARNING: git checkout %BRANCH% failed. Continuing with this working copy.
  git pull --ff-only origin %BRANCH%
  if errorlevel 1 echo WARNING: git pull %BRANCH% failed. Continuing with this working copy.
)

:have_branch
echo Repo: %CD%

REM --- dist: copy from Alienware-18, else publish ---
if exist "%DIST%\Titan.ChiefOfStaff.exe" goto :install_host
echo dist\titan-chief-win-x64 is missing Titan.ChiefOfStaff.exe. Copying from Alienware-18...
mkdir "%DIST%" 2>nul
robocopy "%AW_DIST%" "%DIST%" /E
if exist "%DIST%\Titan.ChiefOfStaff.exe" goto :install_host
robocopy "%AW_REPO_ADMIN%\dist\titan-chief-win-x64" "%DIST%" /E
if exist "%DIST%\Titan.ChiefOfStaff.exe" goto :install_host
if exist "%SRC_REPO%\dist\titan-chief-win-x64\Titan.ChiefOfStaff.exe" (
  robocopy "%SRC_REPO%\dist\titan-chief-win-x64" "%DIST%" /E
)
if exist "%DIST%\Titan.ChiefOfStaff.exe" goto :install_host

where dotnet >nul 2>nul
if errorlevel 1 goto :missing_dist
if not exist "%REPO%\scripts\publish-win-x64.cmd" goto :missing_dist
echo Publishing dist\titan-chief-win-x64 on this PC...
call "%REPO%\scripts\publish-win-x64.cmd"
if exist "%DIST%\Titan.ChiefOfStaff.exe" goto :install_host

:missing_dist
echo ERROR: Titan.ChiefOfStaff.exe not found in dist\titan-chief-win-x64.
echo Copy that folder from Alienware-18, then rerun INSTALL-COS.cmd.
echo Did not start the worker.
goto :fail

:install_host
echo Running INSTALL.cmd. Copies files and arms kill.switch. Does not start the host.
call "%REPO%\deploy\alienware-local-program\INSTALL.cmd" "%DIST%"
if errorlevel 1 (
  echo ERROR: INSTALL.cmd failed.
  goto :fail
)

echo Running ENROLL-NODE.cmd Desktop4. Does not start the worker.
call "%REPO%\deploy\cluster-node\ENROLL-NODE.cmd" Desktop4
if errorlevel 1 (
  echo ERROR: ENROLL-NODE.cmd Desktop4 failed.
  goto :fail
)

echo.
echo ============================================================
echo INSTALLED. DESKTOP4 RELEASED TO JOIN.
echo Titan COS is installed on Desktop4.
echo kill.switch is armed. This is not Verify and not send authorization.
echo Enrolled as Desktop4 (Node4), rooftop 28546 P2/P3 provisional.
echo Cluster MAIN is Alienware-18. holdLaunch=true for unreleased nodes.
echo releasedNodes=Desktop4. Desktop5 and Desktop6 stay install-only.
echo NEXT on this PC from %%USERPROFILE%%: deploy\desktop4\START-WORKER.cmd
echo Leave that window open. Then CHECKIN.cmd twice as Bordine, Glenn on 28546.
echo Leave Chrome on CDP 9222. Do not type verify. Never C:\Windows\system32.
echo Cloud Linux did not run this installer and cannot start the worker.
echo ============================================================
echo.
pause
endlocal
exit /b 0

:refuse_start
echo.
echo REFUSED: INSTALL-COS.cmd does not start the worker. Run START-WORKER.cmd next.
echo Desktop4 is released. Cluster MAIN is Alienware-18. Never 6220.
echo.
pause
endlocal
exit /b 2

:refuse_system32
echo.
echo ERROR: Still in C:\Windows\System32. This installer requires %%USERPROFILE%%.
echo Close this window. On Desktop4 open Command Prompt in %%USERPROFILE%%,
echo then run deploy\desktop4\INSTALL-COS.cmd. Never start from system32.
echo Launch remains held. Did not start the worker.
echo.
pause
endlocal
exit /b 1

:fail
echo.
echo INSTALL-COS failed. Did not start the worker.
echo Did not open Eleads. Did not start Titan.ChiefOfStaff.exe.
echo Desktop4 is still released; fix install, then START-WORKER. Cluster MAIN is Alienware-18. Never 6220.
echo.
pause
endlocal
exit /b 1
