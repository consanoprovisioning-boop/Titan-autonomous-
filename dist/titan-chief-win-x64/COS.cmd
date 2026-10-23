@echo off
cd /d "%~dp0"
REM Double-click this or TitanCOS.exe. One click starts the live overlay loop.
if exist "%~dp0TitanCOS.exe" (
  start "" "%~dp0TitanCOS.exe"
  exit /b 0
)
start "" "%~dp0Titan.ChiefOfStaff.exe" launch
exit /b 0
