@echo off
cd /d "%~dp0"
echo Titan check-in. Preferred launch is TitanCOS.exe. Do not type verify by itself.
echo.
"%~dp0Titan.ChiefOfStaff.exe" checkin
echo.
echo exit code %ERRORLEVEL%
echo Leave the Chrome window open. Sign in as Bordine, Glenn on 28206 or 28546 if you see the login page.
echo Double-click CHECKIN.cmd again to finish Verify, or use TitanCOS.exe for the live overlay loop.
echo Titan will not type the password.
pause
