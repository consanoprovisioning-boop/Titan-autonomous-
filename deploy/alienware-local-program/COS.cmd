@echo off
setlocal EnableExtensions
REM Titan Autonomous Chief of Staff. One double-click. cmd.exe. ASCII.
REM Does not store or type the Eleads password. Does not read a 1Password secret.
REM Does not create a vault item. Does not kill Chrome. Does not kill a healthy worker.
REM Does not start a second Chief of Staff when the live session is already up.
REM Does not reinstall over an app folder that already has Titan.ChiefOfStaff.exe.

set "APP=%LOCALAPPDATA%\TitanChief\app"
set "ROOT=%LOCALAPPDATA%\TitanChief"
set "EXE=%APP%\Titan.ChiefOfStaff.exe"
set "RC=1"

if exist "%EXE%" goto :AppReady
if not exist "%~dp0Titan.ChiefOfStaff.exe" goto :TellInstall
if not exist "%~dp0INSTALL.cmd" goto :TellInstall
echo App folder is missing. Running INSTALL.cmd once from this suite folder.
call "%~dp0INSTALL.cmd" "%~dp0"
if exist "%EXE%" goto :AppReady
echo INSTALL.cmd did not create %EXE%
pause
endlocal & exit /b 1

:TellInstall
echo Titan Chief of Staff is not installed.
echo Run INSTALL.cmd once from the unzipped suite folder.
echo This launcher does not reinstall over an app folder that already has the exe.
pause
endlocal & exit /b 1

:AppReady
if /I "%~dp0"=="%APP%\" goto :ScriptsReady
if exist "%~dp0CHECKIN.cmd" copy /Y "%~dp0CHECKIN.cmd" "%APP%\CHECKIN.cmd" >nul
if exist "%~dp0COS.cmd" copy /Y "%~dp0COS.cmd" "%APP%\COS.cmd" >nul
:ScriptsReady
call :PinDesktop

tasklist /FI "IMAGENAME eq Titan.ChiefOfStaff.exe" /NH 2>nul | find /I "Titan.ChiefOfStaff.exe" >nul
if not errorlevel 1 (
  echo Chief of Staff is already running. Leaving Chrome and that process alone.
  endlocal & exit /b 0
)

call :EnsureWorker

"%EXE%" status > "%TEMP%\titan-cos-status.txt" 2>&1
set "LIVE=0"
findstr /I /C:"state=VerifiedIdle" /C:"state=TickNewLeads" /C:"state=TickPlanner" /C:"state=TickOverdue" /C:"state=TickDatabase" "%TEMP%\titan-cos-status.txt" >nul
if not errorlevel 1 set "LIVE=1"
del /q "%TEMP%\titan-cos-status.txt" >nul 2>&1

del /q "%TEMP%\titan-cos-cdp.txt" >nul 2>&1
set "CDP_STATE=unknown"
powershell.exe -NoProfile -NonInteractive -Command "try { $c = New-Object System.Net.Sockets.TcpClient; $c.Connect('127.0.0.1',9222); $c.Close(); Set-Content -LiteralPath ($env:TEMP + '\titan-cos-cdp.txt') -Value 'UP' -Encoding Ascii } catch { Set-Content -LiteralPath ($env:TEMP + '\titan-cos-cdp.txt') -Value 'DOWN' -Encoding Ascii }"
findstr /X /C:"UP" "%TEMP%\titan-cos-cdp.txt" >nul 2>&1
if not errorlevel 1 set "CDP_STATE=up"
findstr /X /C:"DOWN" "%TEMP%\titan-cos-cdp.txt" >nul 2>&1
if not errorlevel 1 set "CDP_STATE=down"
del /q "%TEMP%\titan-cos-cdp.txt" >nul 2>&1

call :EnsureOnePasswordExtension
if not "%LIVE%"=="1" goto :DoCheckin
if "%CDP_STATE%"=="down" goto :DoCheckin
call :ClassifyEleadsPage
if "%PAGE%"=="LOGIN" goto :DoCheckin
if "%PAGE%"=="DROPPED" goto :DoCheckin
echo Chief of Staff is already the live session. Leaving Chrome and the worker alone.
timeout /t 3 /nobreak >nul
endlocal & exit /b 0

:DoCheckin
if not exist "%APP%\CHECKIN.cmd" (
  echo CHECKIN.cmd is missing from %APP%
  pause
  endlocal & exit /b 1
)
set "TRIES=0"
set "DIDFILL=0"

:CheckinLoop
call "%APP%\CHECKIN.cmd" nopause > "%TEMP%\titan-cos-checkin.txt" 2>&1
set "RC=%ERRORLEVEL%"
findstr /I /C:"FORBIDDEN" "%TEMP%\titan-cos-checkin.txt" >nul
if not errorlevel 1 goto :Blocked
if "%RC%"=="0" goto :ShowThenDone
if "%RC%"=="4" goto :Busy
if not "%RC%"=="6" goto :Failed
if "%DIDFILL%"=="1" goto :WaitFill
type "%TEMP%\titan-cos-checkin.txt"
call :AutofillDroppedTab
set "DIDFILL=1"
if not "%OP_PRESENT%"=="1" goto :NeedYou
:WaitFill
set /a TRIES+=1
if %TRIES% GEQ 4 goto :NeedYou
echo Leave Chrome open. Waiting for the 1Password extension fill. No password is typed.
timeout /t 8 /nobreak >nul
goto :CheckinLoop

:ShowThenDone
type "%TEMP%\titan-cos-checkin.txt"
del /q "%TEMP%\titan-cos-checkin.txt" >nul 2>&1
echo Chief of Staff is up. Leave Chrome open.
timeout /t 3 /nobreak >nul
endlocal & exit /b 0

:Blocked
type "%TEMP%\titan-cos-checkin.txt"
del /q "%TEMP%\titan-cos-checkin.txt" >nul 2>&1
echo Rooftop 6220 is blocked. Chrome was not closed.
pause
endlocal & exit /b 7

:Busy
echo Another Chief of Staff session is already running. This click will not start a second one.
del /q "%TEMP%\titan-cos-checkin.txt" >nul 2>&1
endlocal & exit /b 4

:Failed
type "%TEMP%\titan-cos-checkin.txt"
del /q "%TEMP%\titan-cos-checkin.txt" >nul 2>&1
echo Check-in did not finish. Chrome was not closed. The worker was not closed.
pause
endlocal & exit /b %RC%

:NeedYou
del /q "%TEMP%\titan-cos-checkin.txt" >nul 2>&1
echo Eleads is not signed in. If 1Password is locked, the unlock box was left as it is.
echo This is not a live session. Titan did not type the password.
pause
endlocal & exit /b 6

:EnsureWorker
REM One cursor worker start. Do not stop Chrome or a healthy worker. A live second click leaves that worker alone.
set "WORKER_UP=0"
call :WorkerAlreadyRunning
if "%WORKER_UP%"=="1" (
  echo Cursor worker is already running. Leaving it.
  goto :eof
)
if not exist "%ROOT%" mkdir "%ROOT%" >nul 2>&1
if exist "%ROOT%\worker-start.lock" (
  call :LockFreshness
  if "%LOCK_FRESH%"=="1" (
    echo Cursor worker start is already in progress. Not starting a second one.
    call :WaitForWorker
    if "%WORKER_UP%"=="1" echo Cursor worker is up. Leaving that process running.
    if not "%WORKER_UP%"=="1" echo Cursor worker was not confirmed within 30 seconds. Not starting a second one. Check-in will still run.
    goto :eof
  )
  rmdir "%ROOT%\worker-start.lock" 2>nul
)
mkdir "%ROOT%\worker-start.lock" 2>nul
if errorlevel 1 (
  echo Cursor worker start is already in progress. Not starting a second one.
  call :WaitForWorker
  if "%WORKER_UP%"=="1" echo Cursor worker is up. Leaving that process running.
  if not "%WORKER_UP%"=="1" echo Cursor worker was not confirmed within 30 seconds. Not starting a second one. Check-in will still run.
  goto :eof
)
call :WorkerAlreadyRunning
if "%WORKER_UP%"=="1" (
  rmdir "%ROOT%\worker-start.lock" 2>nul
  echo Cursor worker is already running. Leaving it.
  goto :eof
)
set "CURSOR_CMD="
call :ResolveCursor
if not defined CURSOR_CMD (
  rmdir "%ROOT%\worker-start.lock" 2>nul
  echo cursor was not found. The worker was not started. Check-in will still run.
  echo When cursor is on PATH, double-click COS again. It runs: cursor worker start
  goto :eof
)
set "RUNNER=%ROOT%\start-cursor-worker.cmd"
> "%RUNNER%" echo @echo off
>> "%RUNNER%" echo title Titan Cursor Worker
>> "%RUNNER%" echo cd /d "%USERPROFILE%"
>> "%RUNNER%" echo call "%CURSOR_CMD%" worker start
echo Starting one cursor worker.
start "Titan Cursor Worker" /MIN cmd.exe /c "%RUNNER%"
call :WaitForWorker
rmdir "%ROOT%\worker-start.lock" 2>nul
if "%WORKER_UP%"=="1" (
  echo Cursor worker is up. Leaving that process running.
  goto :eof
)
echo Cursor worker was not confirmed within 30 seconds. Check-in will still run. This click did not start another worker.
goto :eof

:WaitForWorker
set "WAITED=0"
:WaitWorkerLoop
call :WorkerAlreadyRunning
if "%WORKER_UP%"=="1" goto :eof
if %WAITED% GEQ 30 goto :eof
timeout /t 2 /nobreak >nul
set /a WAITED+=2
goto :WaitWorkerLoop

:LockFreshness
set "LOCK_FRESH=0"
del /q "%TEMP%\titan-cos-lock.txt" >nul 2>&1
powershell.exe -NoProfile -NonInteractive -Command " $s = 'STALE'; try { $p = $env:LOCALAPPDATA + '\TitanChief\worker-start.lock'; $age = ((Get-Date) - (Get-Item -LiteralPath $p).CreationTime).TotalSeconds; if ($age -lt 40) { $s = 'FRESH' } } catch {}; Set-Content -LiteralPath ($env:TEMP + '\titan-cos-lock.txt') -Value $s -Encoding Ascii"
findstr /X /C:"FRESH" "%TEMP%\titan-cos-lock.txt" >nul 2>&1
if not errorlevel 1 set "LOCK_FRESH=1"
del /q "%TEMP%\titan-cos-lock.txt" >nul 2>&1
goto :eof

:WorkerAlreadyRunning
REM Up means a process command line contains worker start. Do not treat the query itself as the worker.
set "WORKER_UP=0"
powershell.exe -NoProfile -NonInteractive -Command "if (Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*worker start*' -and $_.CommandLine -notlike '*Win32_Process*' }) { exit 0 } else { exit 1 }"
if not errorlevel 1 set "WORKER_UP=1"
goto :eof

:ResolveCursor
set "CURSOR_CMD="
where cursor > "%TEMP%\titan-cos-where.txt" 2>nul
if exist "%TEMP%\titan-cos-where.txt" (
  for /f "usebackq delims=" %%C in ("%TEMP%\titan-cos-where.txt") do (
    if not defined CURSOR_CMD set "CURSOR_CMD=%%C"
  )
)
del /q "%TEMP%\titan-cos-where.txt" >nul 2>&1
if defined CURSOR_CMD goto :eof
if exist "%LOCALAPPDATA%\Programs\cursor\resources\app\bin\cursor.cmd" set "CURSOR_CMD=%LOCALAPPDATA%\Programs\cursor\resources\app\bin\cursor.cmd"
if defined CURSOR_CMD goto :eof
if exist "%LOCALAPPDATA%\cursor-agent\cursor.cmd" set "CURSOR_CMD=%LOCALAPPDATA%\cursor-agent\cursor.cmd"
if defined CURSOR_CMD goto :eof
if exist "%USERPROFILE%\.local\bin\cursor.cmd" set "CURSOR_CMD=%USERPROFILE%\.local\bin\cursor.cmd"
if defined CURSOR_CMD goto :eof
if exist "%USERPROFILE%\.local\bin\agent.cmd" set "CURSOR_CMD=%USERPROFILE%\.local\bin\agent.cmd"
goto :eof

:EnsureOnePasswordExtension
set "OP_APP="
if exist "%LOCALAPPDATA%\1Password\app\8\1Password.exe" set "OP_APP=%LOCALAPPDATA%\1Password\app\8\1Password.exe"
if not defined OP_APP if exist "%LOCALAPPDATA%\1Password\app\1Password.exe" set "OP_APP=%LOCALAPPDATA%\1Password\app\1Password.exe"
if not defined OP_APP if exist "%ProgramFiles%\1Password\1Password.exe" set "OP_APP=%ProgramFiles%\1Password\1Password.exe"
set "OP_PRESENT=0"
if defined OP_APP set "OP_PRESENT=1"
where op >nul 2>&1
if not errorlevel 1 set "OP_PRESENT=1"
if exist "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Extensions\aeblfdkhhhdcdjpifhhbdiojplfjncoa" set "OP_PRESENT=1"
if exist "%LOCALAPPDATA%\TitanChief\browser\Default\Extensions\aeblfdkhhhdcdjpifhhbdiojplfjncoa" set "OP_PRESENT=1"
if not "%OP_PRESENT%"=="1" goto :eof
REM Official 1Password Chrome extension. No secret. Applies on the next Chrome start, including the Titan profile.
reg add "HKCU\Software\Policies\Google\Chrome\ExtensionInstallForcelist" /v titan-cos-1password /t REG_SZ /d "aeblfdkhhhdcdjpifhhbdiojplfjncoa;https://clients2.google.com/service/update2/crx" /f >nul 2>&1
reg add "HKCU\Software\Google\Chrome\Extensions\aeblfdkhhhdcdjpifhhbdiojplfjncoa" /v update_url /t REG_SZ /d "https://clients2.google.com/service/update2/crx" /f >nul 2>&1
goto :eof

:ClassifyEleadsPage
set "PAGE=UNKNOWN"
del /q "%TEMP%\titan-cos-page.txt" >nul 2>&1
powershell.exe -NoProfile -NonInteractive -Command "try { $pages = @(Invoke-RestMethod 'http://127.0.0.1:9222/json/list' -TimeoutSec 3); $eleads = @($pages | Where-Object { $_.type -eq 'page' -and $_.url -like '*eleadcrm.com*' }); $login = @($eleads | Where-Object { $_.url -like '*login.asp*' }); if ($eleads.Count -eq 0) { $s = 'DROPPED' } elseif ($login.Count -eq $eleads.Count) { $s = 'LOGIN' } else { $s = 'STORE' } } catch { $s = 'UNKNOWN' }; Set-Content -LiteralPath ($env:TEMP + '\titan-cos-page.txt') -Value $s -Encoding Ascii"
findstr /X /C:"LOGIN" "%TEMP%\titan-cos-page.txt" >nul 2>&1
if not errorlevel 1 set "PAGE=LOGIN"
findstr /X /C:"DROPPED" "%TEMP%\titan-cos-page.txt" >nul 2>&1
if not errorlevel 1 set "PAGE=DROPPED"
findstr /X /C:"STORE" "%TEMP%\titan-cos-page.txt" >nul 2>&1
if not errorlevel 1 set "PAGE=STORE"
del /q "%TEMP%\titan-cos-page.txt" >nul 2>&1
goto :eof

:AutofillDroppedTab
REM Dropped Eleads tab. Trigger once: 1Password Fill in Browser, Ctrl+Backslash, to Titan Chrome on port 9222.
REM The Chrome extension aeblfdkhhhdcdjpifhhbdiojplfjncoa fills the login. If 1Password is locked, leave the unlock box.
call :EnsureOnePasswordExtension
if not "%OP_PRESENT%"=="1" (
  echo 1Password is not installed. The Eleads password box stays empty.
  echo Titan will not type the password.
  goto :eof
)
echo Sending 1Password Fill in Browser to the Titan Chrome window.
echo If 1Password is locked, the unlock box stays. This window does not read the password.
if defined OP_APP start "" "%OP_APP%"
REM EncodedCommand focuses Chrome on port 9222 and sends Ctrl+Backslash once. It does not print a secret.
powershell.exe -NoProfile -NonInteractive -EncodedCommand JABFAHIAcgBvAHIAQQBjAHQAaQBvAG4AUAByAGUAZgBlAHIAZQBuAGMAZQAgAD0AIAAnAFMAaQBsAGUAbgB0AGwAeQBDAG8AbgB0AGkAbgB1AGUAJwAKAHQAcgB5ACAAewAgACQAcABhAGcAZQBzACAAPQAgAEAAKABJAG4AdgBvAGsAZQAtAFIAZQBzAHQATQBlAHQAaABvAGQAIAAnAGgAdAB0AHAAOgAvAC8AMQAyADcALgAwAC4AMAAuADEAOgA5ADIAMgAyAC8AagBzAG8AbgAvAGwAaQBzAHQAJwAgAC0AVABpAG0AZQBvAHUAdABTAGUAYwAgADMAKQAgAH0AIABjAGEAdABjAGgAIAB7ACAAZQB4AGkAdAAgADIAIAB9AAoAJABsAG8AZwBpAG4AIAA9ACAAJABwAGEAZwBlAHMAIAB8ACAAVwBoAGUAcgBlAC0ATwBiAGoAZQBjAHQAIAB7ACAAJABfAC4AdAB5AHAAZQAgAC0AZQBxACAAJwBwAGEAZwBlACcAIAAtAGEAbgBkACAAJABfAC4AdQByAGwAIAAtAGwAaQBrAGUAIAAnACoAZQBsAGUAYQBkAGMAcgBtAC4AYwBvAG0AKgAnACAALQBhAG4AZAAgACQAXwAuAHUAcgBsACAALQBsAGkAawBlACAAJwAqAGwAbwBnAGkAbgAuAGEAcwBwACoAJwAgAH0AIAB8ACAAUwBlAGwAZQBjAHQALQBPAGIAagBlAGMAdAAgAC0ARgBpAHIAcwB0ACAAMQAKACQAYQBuAHkAIAA9ACAAJABwAGEAZwBlAHMAIAB8ACAAVwBoAGUAcgBlAC0ATwBiAGoAZQBjAHQAIAB7ACAAJABfAC4AdAB5AHAAZQAgAC0AZQBxACAAJwBwAGEAZwBlACcAIAAtAGEAbgBkACAAJABfAC4AdQByAGwAIAAtAGwAaQBrAGUAIAAnACoAZQBsAGUAYQBkAGMAcgBtAC4AYwBvAG0AKgAnACAAfQAgAHwAIABTAGUAbABlAGMAdAAtAE8AYgBqAGUAYwB0ACAALQBGAGkAcgBzAHQAIAAxAAoAJABwAGEAZwBlACAAPQAgACQAcABhAGcAZQBzACAAfAAgAFcAaABlAHIAZQAtAE8AYgBqAGUAYwB0ACAAewAgACQAXwAuAHQAeQBwAGUAIAAtAGUAcQAgACcAcABhAGcAZQAnACAAfQAgAHwAIABTAGUAbABlAGMAdAAtAE8AYgBqAGUAYwB0ACAALQBGAGkAcgBzAHQAIAAxAAoAJAB0AGEAcgBnAGUAdAAgAD0AIAAkAHAAYQBnAGUACgBpAGYAIAAoACQAYQBuAHkAKQAgAHsAIAAkAHQAYQByAGcAZQB0ACAAPQAgACQAYQBuAHkAIAB9AAoAaQBmACAAKAAkAGwAbwBnAGkAbgApACAAewAgACQAdABhAHIAZwBlAHQAIAA9ACAAJABsAG8AZwBpAG4AIAB9AAoAaQBmACAAKAAtAG4AbwB0ACAAJAB0AGEAcgBnAGUAdAApACAAewAgAGUAeABpAHQAIAAzACAAfQAKAHQAcgB5ACAAewAgAEkAbgB2AG8AawBlAC0AUgBlAHMAdABNAGUAdABoAG8AZAAgACgAJwBoAHQAdABwADoALwAvADEAMgA3AC4AMAAuADAALgAxADoAOQAyADIAMgAvAGoAcwBvAG4ALwBhAGMAdABpAHYAYQB0AGUALwAnACAAKwAgACQAdABhAHIAZwBlAHQALgBpAGQAKQAgAC0AVABpAG0AZQBvAHUAdABTAGUAYwAgADMAIAB8ACAATwB1AHQALQBOAHUAbABsACAAfQAgAGMAYQB0AGMAaAAgAHsAfQAKAFMAdABhAHIAdAAtAFMAbABlAGUAcAAgAC0ATQBpAGwAbABpAHMAZQBjAG8AbgBkAHMAIAAyADAAMAAwAAoAJABiAHIAbwB3AHMAZQByACAAPQAgAEcAZQB0AC0AQwBpAG0ASQBuAHMAdABhAG4AYwBlACAAVwBpAG4AMwAyAF8AUAByAG8AYwBlAHMAcwAgAC0ARgBpAGwAdABlAHIAIAAiAE4AYQBtAGUAIAA9ACAAJwBjAGgAcgBvAG0AZQAuAGUAeABlACcAIgAgAHwAIABXAGgAZQByAGUALQBPAGIAagBlAGMAdAAgAHsAIAAkAF8ALgBDAG8AbQBtAGEAbgBkAEwAaQBuAGUAIAAtAGwAaQBrAGUAIAAnACoAcgBlAG0AbwB0AGUALQBkAGUAYgB1AGcAZwBpAG4AZwAtAHAAbwByAHQAPQA5ADIAMgAyACoAJwAgAH0AIAB8ACAAUwBlAGwAZQBjAHQALQBPAGIAagBlAGMAdAAgAC0ARgBpAHIAcwB0ACAAMQAKACQAaABhAG4AZABsAGUAcwAgAD0AIABAACgAKQAKAGkAZgAgACgAJABiAHIAbwB3AHMAZQByACkAIAB7AAoAIAAgACQAaABhAG4AZABsAGUAcwAgACsAPQAgACgARwBlAHQALQBQAHIAbwBjAGUAcwBzACAALQBJAGQAIAAkAGIAcgBvAHcAcwBlAHIALgBQAHIAbwBjAGUAcwBzAEkAZAAgAC0ARQByAHIAbwByAEEAYwB0AGkAbwBuACAAUwBpAGwAZQBuAHQAbAB5AEMAbwBuAHQAaQBuAHUAZQApAC4ATQBhAGkAbgBXAGkAbgBkAG8AdwBIAGEAbgBkAGwAZQAKACAAIABHAGUAdAAtAEMAaQBtAEkAbgBzAHQAYQBuAGMAZQAgAFcAaQBuADMAMgBfAFAAcgBvAGMAZQBzAHMAIAAtAEYAaQBsAHQAZQByACAAIgBOAGEAbQBlACAAPQAgACcAYwBoAHIAbwBtAGUALgBlAHgAZQAnACIAIAB8ACAAVwBoAGUAcgBlAC0ATwBiAGoAZQBjAHQAIAB7ACAAJABfAC4AUABhAHIAZQBuAHQAUAByAG8AYwBlAHMAcwBJAGQAIAAtAGUAcQAgACQAYgByAG8AdwBzAGUAcgAuAFAAcgBvAGMAZQBzAHMASQBkACAAfQAgAHwAIABGAG8AcgBFAGEAYwBoAC0ATwBiAGoAZQBjAHQAIAB7AAoAIAAgACAAIAAkAGgAYQBuAGQAbABlAHMAIAArAD0AIAAoAEcAZQB0AC0AUAByAG8AYwBlAHMAcwAgAC0ASQBkACAAJABfAC4AUAByAG8AYwBlAHMAcwBJAGQAIAAtAEUAcgByAG8AcgBBAGMAdABpAG8AbgAgAFMAaQBsAGUAbgB0AGwAeQBDAG8AbgB0AGkAbgB1AGUAKQAuAE0AYQBpAG4AVwBpAG4AZABvAHcASABhAG4AZABsAGUACgAgACAAfQAKAH0ACgAkAGgAIAA9ACAAJABoAGEAbgBkAGwAZQBzACAAfAAgAFcAaABlAHIAZQAtAE8AYgBqAGUAYwB0ACAAewAgACQAXwAgAC0AbgBlACAAMAAgAH0AIAB8ACAAUwBlAGwAZQBjAHQALQBPAGIAagBlAGMAdAAgAC0ARgBpAHIAcwB0ACAAMQAKAGkAZgAgACgALQBuAG8AdAAgACQAaAApACAAewAgAGUAeABpAHQAIAA0ACAAfQAKACQAbgBhAHQAaQB2AGUAIAA9ACAAJwBbAEQAbABsAEkAbQBwAG8AcgB0ACgAIgB1AHMAZQByADMAMgAuAGQAbABsACIAKQBdACAAcAB1AGIAbABpAGMAIABzAHQAYQB0AGkAYwAgAGUAeAB0AGUAcgBuACAAYgBvAG8AbAAgAFMAZQB0AEYAbwByAGUAZwByAG8AdQBuAGQAVwBpAG4AZABvAHcAKABJAG4AdABQAHQAcgAgAGgAVwBuAGQAKQA7ACAAWwBEAGwAbABJAG0AcABvAHIAdAAoACIAdQBzAGUAcgAzADIALgBkAGwAbAAiACkAXQAgAHAAdQBiAGwAaQBjACAAcwB0AGEAdABpAGMAIABlAHgAdABlAHIAbgAgAGIAbwBvAGwAIABTAGgAbwB3AFcAaQBuAGQAbwB3ACgASQBuAHQAUAB0AHIAIABoAFcAbgBkACwAIABpAG4AdAAgAG4AQwBtAGQAUwBoAG8AdwApADsAJwAKAEEAZABkAC0AVAB5AHAAZQAgAC0ATgBhAG0AZQBzAHAAYQBjAGUAIABDAG8AcwBXAGkAbgAgAC0ATgBhAG0AZQAgAE4AYQB0AGkAdgBlACAALQBNAGUAbQBiAGUAcgBEAGUAZgBpAG4AaQB0AGkAbwBuACAAJABuAGEAdABpAHYAZQAKAFsAQwBvAHMAVwBpAG4ALgBOAGEAdABpAHYAZQBdADoAOgBTAGgAbwB3AFcAaQBuAGQAbwB3ACgAJABoACwAIAA5ACkAIAB8ACAATwB1AHQALQBOAHUAbABsAAoAaQBmACAAKAAtAG4AbwB0ACAAWwBDAG8AcwBXAGkAbgAuAE4AYQB0AGkAdgBlAF0AOgA6AFMAZQB0AEYAbwByAGUAZwByAG8AdQBuAGQAVwBpAG4AZABvAHcAKAAkAGgAKQApACAAewAgAGUAeABpAHQAIAA1ACAAfQAKAFMAdABhAHIAdAAtAFMAbABlAGUAcAAgAC0ATQBpAGwAbABpAHMAZQBjAG8AbgBkAHMAIAAzADAAMAAKAEEAZABkAC0AVAB5AHAAZQAgAC0AQQBzAHMAZQBtAGIAbAB5AE4AYQBtAGUAIABTAHkAcwB0AGUAbQAuAFcAaQBuAGQAbwB3AHMALgBGAG8AcgBtAHMACgAjACAAMQBQAGEAcwBzAHcAbwByAGQAIABGAGkAbABsACAAaQBuACAAQgByAG8AdwBzAGUAcgA6ACAAQwB0AHIAbAArAEIAYQBjAGsAcwBsAGEAcwBoAC4AIABFAHgAdABlAG4AcwBpAG8AbgAgAGYAaQBsAGwAcwAgAHQAaABlACAAZgBvAGMAdQBzAGUAZAAgAEMAaAByAG8AbQBlACAAdABhAGIALgAKAFsAUwB5AHMAdABlAG0ALgBXAGkAbgBkAG8AdwBzAC4ARgBvAHIAbQBzAC4AUwBlAG4AZABLAGUAeQBzAF0AOgA6AFMAZQBuAGQAVwBhAGkAdAAoACgAWwBzAHQAcgBpAG4AZwBdAFsAYwBoAGEAcgBdADkANAApACAAKwAgACgAWwBzAHQAcgBpAG4AZwBdAFsAYwBoAGEAcgBdADkAMgApACkACgBlAHgAaQB0ACAAMAAKAA==
goto :eof

:PinDesktop
set "VBS=%TEMP%\titan-cos-pin.vbs"
> "%VBS%" echo Set sh = CreateObject("WScript.Shell")
>> "%VBS%" echo Set sc = sh.CreateShortcut(sh.SpecialFolders("Desktop") ^& "\COS.lnk")
>> "%VBS%" echo sc.TargetPath = "%APP%\COS.cmd"
>> "%VBS%" echo sc.WorkingDirectory = "%APP%"
>> "%VBS%" echo sc.Description = "Titan Autonomous Chief of Staff"
>> "%VBS%" echo sc.Save
cscript //nologo "%VBS%" >nul 2>&1
if errorlevel 1 echo Could not create the desktop shortcut. Pin %APP%\COS.cmd
del /q "%VBS%" >nul 2>&1
goto :eof
