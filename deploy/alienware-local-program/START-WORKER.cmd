@echo off
setlocal
title Alienware-18 Cursor Worker - LEAVE THIS WINDOW OPEN
REM Alienware-18 worker starter. Run this one file from anywhere by double-clicking it.
REM This does NOT start Titan.ChiefOfStaff.exe and does NOT open Eleads or handle any password.

cd /d "%USERPROFILE%"
if /i "%CD%"=="C:\Windows\System32" (
  echo ERROR: Still in C:\Windows\System32. Do not run the worker from there.
  echo Close this window, then double-click START-WORKER.cmd again.
  pause
  exit /b 1
)
echo Worker directory: %CD%
echo This directory must show %USERPROFILE% - not C:\Windows\system32.
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
echo Starting: agent worker start --name "Alienware-18" --verbose
echo.
agent worker start --name "Alienware-18" --verbose
goto :worker_stopped

:run_agent_fallback
echo Leave this window open. Do not close it while CRM work is running.
echo Starting: "%LOCALAPPDATA%\cursor-agent\agent.cmd" worker start --name "Alienware-18" --verbose
echo.
call "%LOCALAPPDATA%\cursor-agent\agent.cmd" worker start --name "Alienware-18" --verbose

:worker_stopped
echo.
echo The worker has stopped. Alienware-18 is offline until you run START-WORKER.cmd again.
pause
endlocal
