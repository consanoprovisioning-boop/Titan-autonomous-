@echo off
setlocal
REM Windows COS live launcher. cmd.exe only. No PowerShell. No execution-policy Bypass.
REM Starts the Cursor worker on THIS PC. Does not open Eleads. Does not type a password.
REM Do not run from C:\Windows\system32. Another program named Titan.cmd is on PATH.

cd /d "%~dp0"
echo %CD% | find /I "\system32" >nul
if not errorlevel 1 (
  echo Do not run LAUNCH-COS-LIVE.cmd from C:\Windows\system32.
  echo Open dist\titan-chief-win-x64\ and double-click it there.
  pause
  exit /b 1
)

where agent >nul 2>nul
if errorlevel 1 (
  echo Cursor CLI "agent" is not on PATH.
  echo Install it on this Windows PC, run agent login, then double-click this file again.
  echo Do not use a PowerShell self-extractor from this script.
  pause
  exit /b 1
)

echo Starting Cursor worker in a new window. Leave that window open.
start "Titan Cursor worker" cmd /k "cd /d "%~dp0" && agent worker start --verbose"

if not exist "%LOCALAPPDATA%\TitanChief\app\Titan.ChiefOfStaff.exe" (
  if exist "%~dp0Titan.ChiefOfStaff.exe" (
    echo Titan is not installed yet. Running INSTALL.cmd once. This is not Verify.
    call "%~dp0INSTALL.cmd"
  ) else (
    echo Missing Titan.ChiefOfStaff.exe next to this launcher.
    pause
    exit /b 1
  )
)

echo.
echo Worker is starting on this PC.
echo Close every Chrome window, including the tray icon.
echo Then double-click CHECKIN.cmd, sign in as Bordine, Glenn on 28206 or 28546.
echo Double-click CHECKIN.cmd again to finish Verify. Leave Chrome open.
echo Do not type verify by itself. Command Prompt owns that word.
echo Titan will not type the password. Chrome debugging stays on 127.0.0.1:9222.
echo Verify one live session at a time.
endlocal
pause
