@echo off
cd /d "%~dp0"
echo Titan Autonomous COS - local live CRM loop. Ctrl+C stops. Leave Chrome open.
echo.
"%~dp0Titan.ChiefOfStaff.exe" loop
echo.
echo exit code %ERRORLEVEL%
pause
