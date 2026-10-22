@echo off
setlocal
REM Build the Alienware win-x64 host from cmd.exe. No PowerShell.
cd /d "%~dp0\.."
set PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
dotnet test Titan.ChiefOfStaff.sln -c Release
if errorlevel 1 exit /b 1
dotnet publish src\Titan.ChiefOfStaff\Titan.ChiefOfStaff.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o dist\titan-chief-win-x64
if errorlevel 1 exit /b 1
dotnet publish src\Titan.Launcher\Titan.Launcher.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o dist\titan-chief-win-x64
if errorlevel 1 exit /b 1
del /Q dist\titan-chief-win-x64\*.ps1 2>nul
copy /Y deploy\alienware-local-program\INSTALL.cmd dist\titan-chief-win-x64\INSTALL.cmd
copy /Y deploy\alienware-local-program\CHECKIN.cmd dist\titan-chief-win-x64\CHECKIN.cmd
copy /Y deploy\alienware-local-program\TICK.cmd dist\titan-chief-win-x64\TICK.cmd
copy /Y deploy\alienware-local-program\LOOP.cmd dist\titan-chief-win-x64\LOOP.cmd
copy /Y "deploy\alienware-local-program\Titan COS.cmd" "dist\titan-chief-win-x64\Titan COS.cmd"
copy /Y deploy\alienware-local-program\CREATE-HOME-SCREEN-APP.cmd dist\titan-chief-win-x64\CREATE-HOME-SCREEN-APP.cmd
copy /Y deploy\alienware-local-program\Titan.cmd dist\titan-chief-win-x64\Titan.cmd
copy /Y deploy\alienware-local-program\START-HERE.txt dist\titan-chief-win-x64\START-HERE.txt
echo Published dist\titan-chief-win-x64
echo Hash the exe with certutil. Do not start it from a cloud Linux host.
certutil -hashfile dist\titan-chief-win-x64\Titan.ChiefOfStaff.exe SHA256
endlocal
