@echo off
REM Titan Autonomous suite launcher. cmd.exe only.
REM Do not type verify by itself in Command Prompt. Use CHECKIN.cmd.
REM Do not run this from C:\Windows\system32. Another program named Titan.cmd is on PATH.
echo Titan Chief of Staff.
"%~dp0Titan.ChiefOfStaff.exe" %*
if "%~1"=="" pause
