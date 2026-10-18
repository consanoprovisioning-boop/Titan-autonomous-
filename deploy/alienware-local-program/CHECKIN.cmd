@echo off
cd /d "%~dp0"
echo Titan check-in. Do not type verify by itself. Command Prompt owns that word.
echo.
"%~dp0Titan.ChiefOfStaff.exe" checkin
set "RC=%ERRORLEVEL%"
echo.
echo exit code %RC%
echo Leave the Chrome window open. Sign in as Bordine, Glenn on 28206 or 28546 if you see the login page.
echo Then run this file again. Titan will not type the password.
if /I "%~1"=="nopause" exit /b %RC%
pause
exit /b %RC%
