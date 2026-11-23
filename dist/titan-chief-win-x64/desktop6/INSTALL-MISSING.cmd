@echo off
setlocal EnableExtensions
title Desktop6 Titan COS missing tools - DeskTop6ACER
REM One-shot prereq + settings installer for Desktop6 (DeskTop6ACER).
REM cmd.exe only. No PowerShell. No Eleads. No password. No TICK.cmd.
REM Cloud Linux cannot winget onto this Windows box. Glenn runs this file here.
REM Checks Git, .NET 8, Google Chrome. Installs only what is missing via winget.
REM Pulls cursor/eleads-cluster-4pc-7dfe from Alienware-18 or GitHub.
REM INSTALL.cmd + ENROLL-NODE.cmd Desktop6. Sets TITAN_LEASE_DIR on Alienware-18.
REM START-WORKER only if no Cursor worker is already running on this PC.
REM Does not start a second worker. Does not start Titan.ChiefOfStaff.exe.

if /i "%~1"=="TICK" goto :refuse_tick
if /i "%~1"=="TICK.cmd" goto :refuse_tick

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
echo Prefer %%USERPROFILE%%, not OneDrive Desktop, not C:\Windows\system32.
echo.

set "TITAN_INSTALL_NOPAUSE=1"
set "TITAN_COS_NOPAUSE=1"
set "REPO=%USERPROFILE%\Titan-autonomous-"
set "BRANCH=cursor/eleads-cluster-4pc-7dfe"
set "LEASEDIR=\\Alienware-18\Users\Glenn R Bordine\TitanChief-leases"
set "PRESENT_LIST="
set "INSTALLED_LIST="
set "SKIPPED_LIST="
set "MISSING_LIST="
set "WORKER_ACTION=skipped-already-running-or-unchecked"
set "CDP_STATE=unchecked"
set "CHROME_EXE="

echo ============================================================
echo Desktop6 / DeskTop6ACER missing-tools pass
echo Cluster MAIN is Alienware-18. Rooftop 28546 only on this PC. Never 6220.
echo Identity Bordine, Glenn. SMS 08:00-21:00 ET. Ownership 180 days.
echo Chrome CDP 9222. nodeId=Desktop6. TITAN_LEASE_DIR on Alienware-18.
echo This is not Verify and not a send. Will not run TICK.cmd.
echo Glenn claimed logged in. Will not run a second CHECKIN.cmd.
echo ============================================================
echo.

call :ensure_winget
call :ensure_git
call :ensure_dotnet
call :ensure_chrome
call :refresh_tool_path

echo.
echo --- repo / INSTALL / ENROLL ---
if not exist "%~dp0INSTALL-COS.cmd" (
  echo ERROR: INSTALL-COS.cmd not beside this file.
  echo Run this from deploy\desktop6 after cloning the cluster branch.
  goto :fail
)
call "%~dp0INSTALL-COS.cmd"
if errorlevel 1 (
  echo ERROR: INSTALL-COS.cmd failed. Missing-tools pass stopped.
  goto :fail
)
call :note_present "INSTALL.cmd+ENROLL-NODE Desktop6"

call :ensure_settings
call :ensure_cdp
call :ensure_worker

echo.
echo ============================================================
echo MISSING-TOOLS PASS COMPLETE. DESKTOP6 SETTINGS ON THIS PC.
echo Already present:%PRESENT_LIST%
echo Installed this run:%INSTALLED_LIST%
if not "%SKIPPED_LIST%"=="" echo Skipped:%SKIPPED_LIST%
if not "%MISSING_LIST%"=="" echo Still missing:%MISSING_LIST%
echo nodeId=Desktop6  rooftop=28546  identity=Bordine, Glenn
echo SMS=08:00-21:00 ET  ownership=180 days  forbidden=6220
echo CDP 9222: %CDP_STATE%
echo TITAN_LEASE_DIR=%LEASEDIR%
echo Worker: %WORKER_ACTION%
echo DID NOT run TICK.cmd. DID NOT open Eleads. DID NOT type a password.
echo DID NOT start Titan.ChiefOfStaff.exe from this cloud-written script.
echo Cluster MAIN is Alienware-18. releasedNodes includes Desktop6.
echo If COS is not yet ticking on this PC, next is:
echo   %%LOCALAPPDATA%%\TitanChief\app\TICK.cmd
echo Do not run C:\Windows\system32\Titan.cmd.
echo Cloud Linux did not winget onto this PC and cannot start COS here.
echo ============================================================
echo.
pause
endlocal
exit /b 0

REM ---------- helpers ----------

:ensure_winget
where winget >nul 2>nul
if not errorlevel 1 (
  call :note_present winget
  goto :eof
)
if exist "%LOCALAPPDATA%\Microsoft\WindowsApps\winget.exe" (
  set "PATH=%LOCALAPPDATA%\Microsoft\WindowsApps;%PATH%"
  call :note_present winget
  goto :eof
)
echo WARNING: winget is not on PATH. Cannot auto-install Git / .NET / Chrome.
echo Install App Installer from Microsoft Store, then rerun INSTALL-MISSING.cmd.
call :note_missing winget
goto :eof

:ensure_git
call :refresh_tool_path
where git >nul 2>nul
if not errorlevel 1 (
  call :note_present Git
  goto :eof
)
echo Git missing. Installing Git.Git via winget...
where winget >nul 2>nul
if errorlevel 1 (
  echo WARNING: Cannot install Git without winget.
  call :note_missing Git
  goto :eof
)
winget install -e --id Git.Git --accept-package-agreements --accept-source-agreements
call :refresh_tool_path
where git >nul 2>nul
if errorlevel 1 (
  echo WARNING: Git still not on PATH after winget. Open a new Command Prompt if clone fails.
  call :note_missing Git
  goto :eof
)
call :note_installed Git
goto :eof

:ensure_dotnet
call :refresh_tool_path
where dotnet >nul 2>nul
if not errorlevel 1 goto :dotnet_have_cmd
echo .NET missing. Installing Microsoft.DotNet.DesktopRuntime.8 via winget...
where winget >nul 2>nul
if errorlevel 1 (
  echo WARNING: Cannot install .NET 8 without winget. Copy dist\titan-chief-win-x64 from Alienware-18.
  call :note_missing "NET8-runtime"
  goto :eof
)
winget install -e --id Microsoft.DotNet.DesktopRuntime.8 --accept-package-agreements --accept-source-agreements
call :refresh_tool_path
where dotnet >nul 2>nul
if errorlevel 1 (
  echo WARNING: dotnet still not on PATH. Titan host is framework-dependent net8.0.
  call :note_missing "NET8-runtime"
  goto :eof
)
call :note_installed "NET8-desktop-runtime"
goto :dotnet_have_cmd

:dotnet_have_cmd
dotnet --list-runtimes 2>nul | findstr /i /c:"Microsoft.NETCore.App 8." >nul
if errorlevel 1 (
  echo .NET 8 runtime missing. Installing Microsoft.DotNet.DesktopRuntime.8...
  where winget >nul 2>nul
  if errorlevel 1 (
    call :note_missing "NET8-runtime"
    goto :eof
  )
  winget install -e --id Microsoft.DotNet.DesktopRuntime.8 --accept-package-agreements --accept-source-agreements
  call :note_installed "NET8-desktop-runtime"
) else (
  call :note_present "NET8-runtime"
)
REM SDK only if this PC must publish (exe missing later). Do not force SDK when Alienware supplies dist.
where dotnet >nul 2>nul
if errorlevel 1 goto :eof
dotnet --list-sdks 2>nul | findstr /b /c:"8." >nul
if errorlevel 1 (
  call :note_present "NET8-SDK-not-required-if-dist-copied"
) else (
  call :note_present "NET8-SDK"
)
goto :eof

:ensure_chrome
if exist "%ProgramFiles%\Google\Chrome\Application\chrome.exe" set "CHROME_EXE=%ProgramFiles%\Google\Chrome\Application\chrome.exe"
if exist "%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe" set "CHROME_EXE=%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"
if exist "%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe" set "CHROME_EXE=%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"
if not "%CHROME_EXE%"=="" (
  call :note_present "Google-Chrome"
  goto :eof
)
echo Google Chrome missing. Installing Google.Chrome via winget...
where winget >nul 2>nul
if errorlevel 1 (
  echo WARNING: Cannot install Chrome without winget. CHECKIN.cmd needs Google Chrome for CDP 9222.
  call :note_missing "Google-Chrome"
  goto :eof
)
winget install -e --id Google.Chrome --accept-package-agreements --accept-source-agreements
if exist "%ProgramFiles%\Google\Chrome\Application\chrome.exe" set "CHROME_EXE=%ProgramFiles%\Google\Chrome\Application\chrome.exe"
if exist "%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe" set "CHROME_EXE=%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"
if exist "%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe" set "CHROME_EXE=%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"
if "%CHROME_EXE%"=="" (
  call :note_missing "Google-Chrome"
  goto :eof
)
call :note_installed "Google-Chrome"
goto :eof

:refresh_tool_path
if exist "%ProgramFiles%\Git\cmd\git.exe" set "PATH=%ProgramFiles%\Git\cmd;%PATH%"
if exist "%LOCALAPPDATA%\Programs\Git\cmd\git.exe" set "PATH=%LOCALAPPDATA%\Programs\Git\cmd;%PATH%"
if exist "%ProgramFiles%\dotnet\dotnet.exe" set "PATH=%ProgramFiles%\dotnet;%PATH%"
if exist "%LOCALAPPDATA%\Microsoft\WindowsApps\winget.exe" set "PATH=%LOCALAPPDATA%\Microsoft\WindowsApps;%PATH%"
goto :eof

:ensure_settings
set "ROOT=%LOCALAPPDATA%\TitanChief"
mkdir "%ROOT%" 2>nul
> "%ROOT%\node.json" echo {"nodeId":"Desktop6","rooftop":"28546","identity":"Bordine, Glenn","forbiddenRooftop":"6220","clusterMain":"Alienware-18","hostnameHint":"DeskTop6ACER","smsWindowEt":"08:00-21:00","ownershipScreenDays":180,"leaseHost":"Alienware-18","leaseDirHint":"\\\\Alienware-18\\Users\\Glenn R Bordine\\TitanChief-leases","cdpPort":9222}
if exist "%REPO%\deploy\cluster-node\cluster-hold.json" copy /Y "%REPO%\deploy\cluster-node\cluster-hold.json" "%ROOT%\cluster-hold.json" >nul
echo Settings written: %ROOT%\node.json
call :note_present "nodeId=Desktop6,rooftop=28546,identity,SMS,180-day"

reg query "HKCU\Environment" /v TITAN_LEASE_DIR >nul 2>nul
if not errorlevel 1 (
  echo TITAN_LEASE_DIR already in user environment.
  call :note_present TITAN_LEASE_DIR
) else (
  setx TITAN_LEASE_DIR "%LEASEDIR%" >nul
  echo setx TITAN_LEASE_DIR=%LEASEDIR%
  call :note_installed TITAN_LEASE_DIR
)
set "TITAN_LEASE_DIR=%LEASEDIR%"
goto :eof

:ensure_cdp
REM Leave an existing 9222 session. Do not CHECKIN. Do not open Eleads.
curl.exe -s -m 2 http://127.0.0.1:9222/json/version >nul 2>nul
if not errorlevel 1 (
  echo Chrome CDP 9222 is listening. Leaving it open. No second CHECKIN.
  set "CDP_STATE=listening-left-open"
  call :note_present "Chrome-CDP-9222"
  goto :eof
)
netstat -ano | findstr /c:":9222" | findstr /i "LISTEN LISTENING" >nul
if not errorlevel 1 (
  echo Port 9222 is in use. Leaving it. No second CHECKIN.
  set "CDP_STATE=port-9222-in-use-left-open"
  call :note_present "port-9222"
  goto :eof
)

tasklist /FI "IMAGENAME eq chrome.exe" | findstr /i /c:"chrome.exe" >nul
if not errorlevel 1 (
  echo Chrome is open but CDP 9222 is not listening.
  echo Glenn claimed logged in. This installer will not close Chrome and will not CHECKIN.
  echo If this Chrome was started without remote debugging, close Chrome including the tray
  echo icon later, then double-click CHECKIN.cmd once. Do not type verify.
  set "CDP_STATE=chrome-open-without-9222-left-alone"
  call :note_skipped "CHECKIN-second-pass"
  goto :eof
)

if "%CHROME_EXE%"=="" (
  echo Chrome exe not found. Cannot start CDP 9222.
  set "CDP_STATE=chrome-missing"
  call :note_missing "Chrome-CDP-9222"
  goto :eof
)

echo Starting Google Chrome with remote debugging on 9222. Not opening Eleads.
mkdir "%LOCALAPPDATA%\TitanChief\browser" 2>nul
start "" "%CHROME_EXE%" --remote-debugging-port=9222 --user-data-dir="%LOCALAPPDATA%\TitanChief\browser" --no-first-run --no-default-browser-check
set "CDP_STATE=started-9222-no-eleads"
call :note_installed "Chrome-CDP-9222"
goto :eof

:ensure_worker
REM Do not start a second worker. Last known id 3b6886be-8df5-5399-a9f8-0b8c65e254b4.
REM Prefer %%USERPROFILE%%, never C:\Windows\system32, never a second OneDrive Desktop worker.

tasklist /FI "WINDOWTITLE eq Desktop6 Cursor Worker*" | findstr /i /c:"cmd.exe" >nul
if not errorlevel 1 goto :worker_present
tasklist /V | findstr /i /c:"Desktop6 Cursor Worker" >nul
if not errorlevel 1 goto :worker_present
tasklist /FI "IMAGENAME eq agent.exe" | findstr /i /c:"agent.exe" >nul
if not errorlevel 1 goto :worker_present
tasklist /FI "IMAGENAME eq cursor-agent.exe" | findstr /i /c:"cursor-agent.exe" >nul
if not errorlevel 1 goto :worker_present
where agent >nul 2>nul
if not errorlevel 1 goto :worker_check_done
if exist "%LOCALAPPDATA%\cursor-agent\agent.cmd" goto :worker_check_done
echo WARNING: Cursor agent CLI not found. Cannot START-WORKER from this script.
echo Install Cursor agent on this PC if the worker window is gone.
call :note_missing "Cursor-agent-CLI"
set "WORKER_ACTION=cursor-agent-cli-missing-did-not-start"
goto :eof

:worker_check_done
REM If agent.exe was not seen, start one worker from %%USERPROFILE%% in a new window.
tasklist /FI "IMAGENAME eq agent.exe" | findstr /i /c:"agent.exe" >nul
if not errorlevel 1 goto :worker_present
tasklist /V | findstr /i /c:"agent worker" >nul
if not errorlevel 1 goto :worker_present

echo No Cursor worker process listed on this PC. Starting one from %%USERPROFILE%%.
echo This is not a second worker. Cloud Linux cannot start this.
if not exist "%REPO%\deploy\desktop6\START-WORKER.cmd" (
  echo ERROR: START-WORKER.cmd missing after install.
  set "WORKER_ACTION=start-script-missing"
  call :note_missing START-WORKER.cmd
  goto :eof
)
start "Desktop6 Cursor Worker" cmd /k cd /d "%USERPROFILE%" ^& call "%REPO%\deploy\desktop6\START-WORKER.cmd"
set "WORKER_ACTION=started-one-from-USERPROFILE-new-window"
call :note_installed "Cursor-worker-Desktop6"
goto :eof

:worker_present
echo Cursor worker already running on this PC. Not starting a second one.
echo Last known worker id 3b6886be-8df5-5399-a9f8-0b8c65e254b4 from %%USERPROFILE%%.
set "WORKER_ACTION=already-running-skipped-second"
call :note_present "Cursor-worker"
call :note_skipped "second-START-WORKER"
goto :eof

:note_present
set "PRESENT_LIST=%PRESENT_LIST% %~1"
goto :eof

:note_installed
set "INSTALLED_LIST=%INSTALLED_LIST% %~1"
goto :eof

:note_skipped
set "SKIPPED_LIST=%SKIPPED_LIST% %~1"
goto :eof

:note_missing
set "MISSING_LIST=%MISSING_LIST% %~1"
goto :eof

:refuse_tick
echo.
echo REFUSED: INSTALL-MISSING.cmd does not run TICK.cmd and does not send.
echo This pass installs tools and settings only.
echo.
pause
endlocal
exit /b 2

:refuse_system32
echo.
echo ERROR: Still in C:\Windows\System32. This installer requires %%USERPROFILE%%.
echo Close this window. On DeskTop6ACER open Command Prompt in %%USERPROFILE%%,
echo then run deploy\desktop6\INSTALL-MISSING.cmd. Never start from system32.
echo Did not start a second worker. Did not run TICK.cmd.
echo.
pause
endlocal
exit /b 1

:fail
echo.
echo INSTALL-MISSING failed. Did not run TICK.cmd. Did not open Eleads.
echo Did not start Titan.ChiefOfStaff.exe. Never 6220.
echo Cluster MAIN is Alienware-18.
echo.
pause
endlocal
exit /b 1
