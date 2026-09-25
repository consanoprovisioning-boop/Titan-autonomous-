@echo off
setlocal
REM Alienware 18 local install. cmd.exe only. No PowerShell. No execution-policy Bypass.
REM Does not start Eleads. Does not type a password.

if "%~1"=="" (
  echo Usage: INSTALL.cmd [path-to-publish-folder]
  echo Example: INSTALL.cmd C:\Users\Glenn\Downloads\titan-chief-win-x64
  exit /b 1
)

set SRC=%~1
set APP=%LOCALAPPDATA%\TitanChief\app
set ROOT=%LOCALAPPDATA%\TitanChief

if not exist "%SRC%\Titan.ChiefOfStaff.exe" (
  echo Missing Titan.ChiefOfStaff.exe in %SRC%
  exit /b 1
)

mkdir "%APP%" 2>nul
mkdir "%ROOT%" 2>nul
xcopy /E /I /Y "%SRC%\*" "%APP%\"
echo armed>"%ROOT%\kill.switch"
"%APP%\Titan.ChiefOfStaff.exe" install
type "%ROOT%\kill.switch"
certutil -hashfile "%APP%\Titan.ChiefOfStaff.exe" SHA256
echo.
echo Installed to %APP%
echo kill.switch is armed. This is not Glenn Verify and not send authorization.
echo Next: "%APP%\Titan.cmd" status
echo Sign into Eleads in Chrome as Bordine, Glenn on 28206 or 28546, then: "%APP%\Titan.cmd" verify
endlocal
