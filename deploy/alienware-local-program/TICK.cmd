@echo off
cd /d "%~dp0"
echo Titan Chief of Staff tick. This file is not the other Titan.cmd on this PC.
echo.
"%~dp0Titan.ChiefOfStaff.exe" tick
echo.
echo exit code %ERRORLEVEL%
pause
