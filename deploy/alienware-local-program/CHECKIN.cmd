@echo off
cd /d "%~dp0"
echo Titan check-in. Do not type verify by itself. Command Prompt owns that word.
echo.
"%~dp0Titan.ChiefOfStaff.exe" checkin
echo.
echo exit code %ERRORLEVEL%
echo Leave the Chrome window open. Sign in as Bordine, Glenn on 28206 or 28546 if you see the login page.
echo Double-click CHECKIN.cmd again to finish Verify. Titan will not type the password.
pause
