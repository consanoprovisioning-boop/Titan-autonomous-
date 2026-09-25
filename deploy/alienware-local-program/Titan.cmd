@echo off
REM Titan Autonomous suite launcher. cmd.exe only.
REM Do not type verify by itself in Command Prompt. Use CHECKIN.cmd.
"%~dp0Titan.ChiefOfStaff.exe" %*
if "%~1"=="" pause
