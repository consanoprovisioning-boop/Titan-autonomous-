@echo off
REM Titan Autonomous COS home-screen launcher (Glenn, 2026-10-22).
REM One double-click: app window opens, Verifies, live loop starts by itself.
set APP=%LOCALAPPDATA%\TitanChief\app
if exist "%APP%\Titan COS.exe" (
  start "" "%APP%\Titan COS.exe"
  exit /b 0
)
if exist "%~dp0Titan COS.exe" (
  start "" "%~dp0Titan COS.exe"
  exit /b 0
)
REM Fallback without the app window: console autostart does the same sequence.
if exist "%APP%\Titan.ChiefOfStaff.exe" (
  "%APP%\Titan.ChiefOfStaff.exe" autostart
  pause
  exit /b 0
)
echo Titan COS not installed yet. Double-click INSTALL.cmd once.
pause
