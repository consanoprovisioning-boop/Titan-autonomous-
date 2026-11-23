@echo off
setlocal
title Desktop6 Cursor Worker - LEAVE THIS WINDOW OPEN
REM Desktop6 worker starter. Run this one file from anywhere by double-clicking it.
REM This does NOT start Titan.ChiefOfStaff.exe and does NOT open Eleads or handle any password.
REM Desktop6 (DeskTop6ACER) is released to join the cluster. Desktop5 stays held.
REM If a Cursor worker is already running from %USERPROFILE% on this PC, do not start a second one.

call "%~dp0..\cluster-node\HOLD-LAUNCH.cmd" "%~dp0..\cluster-node\cluster-hold.json" Desktop6
if errorlevel 2 (
  pause
  exit /b 2
)

cd /d "%USERPROFILE%"
if /i "%CD%"=="C:\Windows\System32" (
  echo ERROR: Still in C:\Windows\System32. Do not run the worker from there.
  echo Close this window, then double-click START-WORKER.cmd again.
  pause
  exit /b 1
)
echo Worker directory: %CD%
echo This directory must show %USERPROFILE% - not C:\Windows\system32.
echo This PC is Desktop6 (Node6 / DeskTop6ACER). Rooftop 28546 P1 only. Never 6220.
echo Nissan 28206 stays on Alienware-18. Do not steal it.
echo Assigned jobs: overdue emails, overdue call reschedule, organizer remainder,
echo SMS 08:00-21:00 ET, appointment-commit fast path.
echo.

where agent >nul 2>nul
if not errorlevel 1 goto :run_agent_on_path
if exist "%LOCALAPPDATA%\cursor-agent\agent.cmd" goto :run_agent_fallback

echo ERROR: Cursor agent was not found.
echo Looked for "agent" on PATH and for "%LOCALAPPDATA%\cursor-agent\agent.cmd".
echo Install or repair the Cursor agent on this PC, then double-click START-WORKER.cmd again.
pause
exit /b 1

:run_agent_on_path
echo Leave this window open. Do not close it while CRM work is running.
echo Starting: agent worker start --name "Desktop6" --verbose
echo.
agent worker start --name "Desktop6" --verbose
goto :worker_stopped

:run_agent_fallback
echo Leave this window open. Do not close it while CRM work is running.
echo Starting: "%LOCALAPPDATA%\cursor-agent\agent.cmd" worker start --name "Desktop6" --verbose
echo.
call "%LOCALAPPDATA%\cursor-agent\agent.cmd" worker start --name "Desktop6" --verbose

:worker_stopped
echo.
echo The worker has stopped. Desktop6 is offline until you run START-WORKER.cmd again.
pause
endlocal
