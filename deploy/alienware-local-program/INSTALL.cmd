@echo off
setlocal
REM Alienware 18 local install. cmd.exe only. No PowerShell. No execution-policy Bypass.
REM Does not start Eleads. Does not type a password.

set "SRC=%~1"
if "%SRC%"=="" set "SRC=%~dp0"
set APP=%LOCALAPPDATA%\TitanChief\app
set ROOT=%LOCALAPPDATA%\TitanChief

if not exist "%SRC%\Titan.ChiefOfStaff.exe" (
  echo Missing Titan.ChiefOfStaff.exe in %SRC%
  exit /b 1
)

mkdir "%APP%" 2>nul
mkdir "%ROOT%" 2>nul
xcopy /E /I /Y "%SRC%\*" "%APP%\"
REM Keep the one-click scripts from this folder. Do not require a second copy step.
for %%F in (COS.cmd CHECKIN.cmd TICK.cmd Titan.cmd START-HERE.txt) do (
  if exist "%~dp0%%F" copy /Y "%~dp0%%F" "%APP%\%%F" >nul
)
echo armed>"%ROOT%\kill.switch"
"%APP%\Titan.ChiefOfStaff.exe" install
type "%ROOT%\kill.switch"
certutil -hashfile "%APP%\Titan.ChiefOfStaff.exe" SHA256
echo.
echo Installed to %APP%
echo kill.switch is armed. This is not check-in and not send authorization.
echo Next: double-click COS.cmd in %APP%
echo Do not type verify by itself. Command Prompt owns that word.
endlocal
