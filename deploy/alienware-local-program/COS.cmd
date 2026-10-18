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

if not "%LIVE%"=="1" goto :DoCheckin
if "%CDP_STATE%"=="down" goto :DoCheckin
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

:CheckinLoop
call "%APP%\CHECKIN.cmd" nopause > "%TEMP%\titan-cos-checkin.txt" 2>&1
set "RC=%ERRORLEVEL%"
findstr /I /C:"FORBIDDEN" "%TEMP%\titan-cos-checkin.txt" >nul
if not errorlevel 1 goto :Blocked
if "%RC%"=="0" goto :ShowThenDone
if "%RC%"=="4" goto :Busy
if not "%RC%"=="6" goto :Failed
if "%TRIES%"=="0" type "%TEMP%\titan-cos-checkin.txt"
if "%TRIES%"=="0" call :OfferOnePassword
set /a TRIES+=1
if %TRIES% GEQ 12 goto :NeedYou
echo Leave Chrome open. Approve the 1Password prompt, or fill the empty Eleads box yourself.
timeout /t 10 /nobreak >nul
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
echo Sign-in is still open. Leave Chrome open. Double-click COS again after the store page is up.
echo Titan did not type the password.
pause
endlocal & exit /b 6

:EnsureWorker
set "WORKER_UP=0"
call :WorkerAlreadyRunning
if "%WORKER_UP%"=="1" (
  echo Cursor worker is already running. Leaving it.
  goto :eof
)
set "CURSOR_CMD="
call :ResolveCursor
if not defined CURSOR_CMD (
  echo cursor was not found. The worker was not started. Check-in will still run.
  echo When cursor is on PATH, double-click COS again. It runs: cursor worker start
  goto :eof
)
if not exist "%ROOT%" mkdir "%ROOT%" >nul 2>&1
set "RUNNER=%ROOT%\start-cursor-worker.cmd"
> "%RUNNER%" echo @echo off
>> "%RUNNER%" echo title Titan Cursor Worker
>> "%RUNNER%" echo cd /d "%USERPROFILE%"
>> "%RUNNER%" echo call "%CURSOR_CMD%" worker start
echo Starting cursor worker. Leave that window running.
start "Titan Cursor Worker" /MIN cmd.exe /c "%RUNNER%"
goto :eof

:WorkerAlreadyRunning
set "WORKER_UP=0"
powershell.exe -NoProfile -NonInteractive -Command "if (Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*worker start*' -and $_.CommandLine -notlike '*Win32_Process*' }) { exit 0 } else { exit 1 }"
if not errorlevel 1 set "WORKER_UP=1"
if "%WORKER_UP%"=="1" goto :eof
tasklist /FI "WINDOWTITLE eq Titan Cursor Worker" /NH 2>nul | find /I "cmd.exe" >nul
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

:OfferOnePassword
REM Opens the 1Password app so Glenn can approve a fill. Never prints a secret.
set "OP_APP="
if exist "%LOCALAPPDATA%\1Password\app\8\1Password.exe" set "OP_APP=%LOCALAPPDATA%\1Password\app\8\1Password.exe"
if not defined OP_APP if exist "%LOCALAPPDATA%\1Password\app\1Password.exe" set "OP_APP=%LOCALAPPDATA%\1Password\app\1Password.exe"
if not defined OP_APP if exist "%ProgramFiles%\1Password\1Password.exe" set "OP_APP=%ProgramFiles%\1Password\1Password.exe"
set "OP_CLI_FOUND=0"
where op >nul 2>&1
if not errorlevel 1 set "OP_CLI_FOUND=1"
if not defined OP_APP if "%OP_CLI_FOUND%"=="0" (
  echo 1Password is not installed. The Eleads password box stays empty for you to fill.
  echo Titan will not type the password.
  goto :eof
)
echo 1Password will ask you to approve the Eleads fill. This window does not read the password.
echo One-time: sign in to the 1Password app yourself. If you use op, run op signin in a window you open.
if defined OP_APP start "" "%OP_APP%"
if defined OP_APP start "" "onepassword://search/Eleads"
if not defined OP_APP echo Open the 1Password app and approve the fill. The Eleads box stays empty until you do.
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
