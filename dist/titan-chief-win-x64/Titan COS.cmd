@echo off
REM Titan Autonomous COS home-screen launcher (Glenn, 2026-10-22).
REM Opens the Titan COS app window if installed; falls back to the console loop.
set APP=%LOCALAPPDATA%\TitanChief\app
if exist "%APP%\Titan COS.exe" (
  start "" "%APP%\Titan COS.exe"
  exit /b 0
)
if exist "%~dp0Titan COS.exe" (
  start "" "%~dp0Titan COS.exe"
  exit /b 0
)
echo Titan COS app not installed yet. Run INSTALL.cmd, then CREATE-HOME-SCREEN-APP.cmd.
pause
