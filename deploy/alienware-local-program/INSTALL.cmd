@echo off
setlocal
REM Alienware 18 local install. cmd.exe only. No PowerShell. No execution-policy Bypass.
REM Copies files and arms the kill switch. Does not start Titan.ChiefOfStaff.exe.
REM Does not open Eleads. Does not type a password.

set "SRC=%~1"
if "%SRC%"=="" set "SRC=%~dp0"
set APP=%LOCALAPPDATA%\TitanChief\app
set ROOT=%LOCALAPPDATA%\TitanChief

if not exist "%SRC%\Titan.ChiefOfStaff.exe" (
  echo Missing Titan.ChiefOfStaff.exe in %SRC%
  pause
  exit /b 1
)

mkdir "%APP%" 2>nul
mkdir "%ROOT%" 2>nul
xcopy /E /I /Y "%SRC%\*" "%APP%\"
echo armed>"%ROOT%\kill.switch"
type "%ROOT%\kill.switch"
certutil -hashfile "%APP%\Titan.ChiefOfStaff.exe" SHA256
echo.
echo Installed to %APP%
echo kill.switch is armed. This is not Verify and not send authorization.
echo Next: double-click CREATE-HOME-SCREEN-APP.cmd once for the Desktop app,
echo then launch Titan COS from the Desktop: 1 Check In / Verify, 2 Start Live CRM Loop.
echo Do not type verify by itself. Command Prompt owns that word.
endlocal
pause
