@echo off
setlocal
REM Glenn (2026-10-22): Titan COS must be launchable from an app on the home screen.
REM Creates a Desktop app shortcut named "Titan COS" for Titan.Launcher (Titan COS.exe).
REM cmd.exe + Windows Script Host only. No PowerShell. No execution-policy change.
REM This replaces the old install-without-launch "no Desktop shortcuts" note for
REM this one Glenn-requested launcher; INSTALL.cmd still creates no shortcuts.

set APP=%LOCALAPPDATA%\TitanChief\app
set TARGET=%APP%\Titan COS.exe
if not exist "%TARGET%" set TARGET=%~dp0Titan COS.exe
if not exist "%TARGET%" (
  echo Titan COS app not found. Run INSTALL.cmd first so Titan COS.exe is in %APP%.
  pause
  exit /b 1
)

set DESKTOP=%USERPROFILE%\Desktop
set LNK=%DESKTOP%\Titan COS.lnk
set VBS=%TEMP%\titan-cos-shortcut.vbs

> "%VBS%" echo Set s = CreateObject("WScript.Shell")
>> "%VBS%" echo Set l = s.CreateShortcut("%LNK%")
>> "%VBS%" echo l.TargetPath = "%TARGET%"
>> "%VBS%" echo l.WorkingDirectory = "%APP%"
>> "%VBS%" echo l.Description = "Titan Autonomous COS - Valdosta live CRM loop (Bordine, Glenn)"
>> "%VBS%" echo l.IconLocation = "%TARGET%,0"
>> "%VBS%" echo l.Save
cscript //nologo "%VBS%"
del "%VBS%" 2>nul

if exist "%LNK%" (
  echo Home-screen app created: %LNK%
  echo Double-click "Titan COS" on the Desktop, then: 1 Check In / Verify, 2 Start Live CRM Loop.
) else (
  copy /Y "%~dp0Titan COS.cmd" "%DESKTOP%\Titan COS.cmd" >nul
  echo Shortcut failed, so a launchable "Titan COS" app was copied to the Desktop instead.
)
endlocal
pause
