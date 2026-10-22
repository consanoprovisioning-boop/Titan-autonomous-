@echo off
setlocal
REM Alienware 18 one-double-click setup. cmd.exe only. No PowerShell.
REM Copies files, arms kill switch, creates the Desktop "Titan COS" app,
REM then launches it. The app Verifies and starts the live loop by itself.
REM Titan does not type the Eleads password; Glenn signs in in Chrome.

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
certutil -hashfile "%APP%\Titan.ChiefOfStaff.exe" SHA256
echo.
echo Installed to %APP%

REM Home-screen app, created automatically (Glenn: fast and simple).
if exist "%APP%\CREATE-HOME-SCREEN-APP.cmd" call "%APP%\CREATE-HOME-SCREEN-APP.cmd" nopause

echo Launching Titan COS now. Sign in in Chrome if asked - the loop starts by itself.
if exist "%APP%\Titan COS.exe" start "" "%APP%\Titan COS.exe"
endlocal
