@echo off
setlocal
REM Reusable worker starter. Optional argument is the node name.
REM Default reads %LOCALAPPDATA%\TitanChief\node.json if present, else Node3.
REM Does NOT start Titan.ChiefOfStaff.exe and does NOT open Eleads.

set "NODE=%~1"
if "%NODE%"=="" (
  if exist "%LOCALAPPDATA%\TitanChief\node.json" (
    for /f "tokens=2 delims=:," %%A in ('findstr /i "nodeId" "%LOCALAPPDATA%\TitanChief\node.json"') do (
      set "NODE=%%~A"
    )
  )
)
if "%NODE%"=="" set "NODE=Node3"
set "NODE=%NODE: =%"
set "NODE=%NODE:"=%"

title %NODE% Cursor Worker - LEAVE THIS WINDOW OPEN
cd /d "%USERPROFILE%"
if /i "%CD%"=="C:\Windows\System32" (
  echo ERROR: Still in C:\Windows\System32. Do not run the worker from there.
  echo Close this window, then double-click START-WORKER.cmd again.
  pause
  exit /b 1
)
echo Worker directory: %CD%
echo Node: %NODE%
echo This directory must show %USERPROFILE% - not C:\Windows\system32.
echo Rooftops 28206 and 28546 only. Never 6220.
echo.

where agent >nul 2>nul
if not errorlevel 1 goto :run_agent_on_path
if exist "%LOCALAPPDATA%\cursor-agent\agent.cmd" goto :run_agent_fallback

echo ERROR: Cursor agent was not found.
echo Looked for "agent" on PATH and for "%LOCALAPPDATA%\cursor-agent\agent.cmd".
pause
exit /b 1

:run_agent_on_path
echo Leave this window open.
echo Starting: agent worker start --name "%NODE%" --verbose
echo.
agent worker start --name "%NODE%" --verbose
goto :worker_stopped

:run_agent_fallback
echo Leave this window open.
echo Starting: "%LOCALAPPDATA%\cursor-agent\agent.cmd" worker start --name "%NODE%" --verbose
echo.
call "%LOCALAPPDATA%\cursor-agent\agent.cmd" worker start --name "%NODE%" --verbose

:worker_stopped
echo.
echo The worker has stopped. %NODE% is offline until you run START-WORKER.cmd again.
pause
endlocal
