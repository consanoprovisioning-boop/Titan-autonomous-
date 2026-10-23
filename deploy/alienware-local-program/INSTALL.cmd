@echo off
setlocal
REM Alienware 18 local install. cmd.exe only. No PowerShell. No execution-policy Bypass.
REM Copies files and arms the kill switch. Does not open Eleads. Does not type a password.
REM After this, double-click TitanCOS.exe to put COS to work.

set "SRC=%~1"
if "%SRC%"=="" set "SRC=%~dp0"
set APP=%LOCALAPPDATA%\TitanChief\app
set ROOT=%LOCALAPPDATA%\TitanChief

if not exist "%SRC%\Titan.ChiefOfStaff.exe" if not exist "%SRC%\TitanCOS.exe" (
  echo Missing TitanCOS.exe / Titan.ChiefOfStaff.exe in %SRC%
  pause
  exit /b 1
)

mkdir "%APP%" 2>nul
mkdir "%ROOT%" 2>nul
xcopy /E /I /Y "%SRC%\*" "%APP%\"
echo armed>"%ROOT%\kill.switch"
type "%ROOT%\kill.switch"
if exist "%APP%\TitanCOS.exe" (
  certutil -hashfile "%APP%\TitanCOS.exe" SHA256
) else (
  certutil -hashfile "%APP%\Titan.ChiefOfStaff.exe" SHA256
)
echo.
echo Installed to %APP%
echo kill.switch is armed. This is not Verify and not send authorization.
echo Double-click TitanCOS.exe to put COS to work.
echo Sign in as Bordine, Glenn if Eleads asks. Titan will not type the password.
endlocal
pause
