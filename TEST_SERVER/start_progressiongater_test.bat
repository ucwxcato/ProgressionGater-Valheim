@echo off
setlocal

REM Progression Gater isolated local dedicated-server launcher.
REM This launches the existing Steam server install but deploys only this
REM repository's ProgressionGater.dll. It never starts automatically.

set "REPO_DIR=%~dp0.."
for %%I in ("%REPO_DIR%") do set "REPO_DIR=%%~fI"
if not defined PG_SERVER_INSTALL set "PG_SERVER_INSTALL=C:\PROGRA~2\Steam\steamapps\common\Valheim dedicated server"

if not exist "%PG_SERVER_INSTALL%\valheim_server.exe" goto :no_server
if not exist "%PG_SERVER_INSTALL%\BepInEx\core\BepInEx.dll" goto :no_bepinex
if not exist "%PG_SERVER_INSTALL%\winhttp.dll" goto :no_doorstop
if not exist "%REPO_DIR%\src\ProgressionGater\bin\Release\net48\net48\ProgressionGater.dll" goto :no_mod

if not exist "%PG_SERVER_INSTALL%\BepInEx\plugins" mkdir "%PG_SERVER_INSTALL%\BepInEx\plugins"
copy /Y "%REPO_DIR%\src\ProgressionGater\bin\Release\net48\net48\ProgressionGater.dll" "%PG_SERVER_INSTALL%\BepInEx\plugins\ProgressionGater.dll" >nul
if errorlevel 1 goto :copy_failed

set "SteamAppId=892970"

echo.
echo ============================================================
echo  Progression Gater isolated test server
echo ============================================================
echo  World:   progressiongater_test
echo  Connect: 127.0.0.1:2462
echo  Config:  %PG_SERVER_INSTALL%\BepInEx\config\com.catosaur.progressiongater.cfg
echo  Log:     %PG_SERVER_INSTALL%\BepInEx\LogOutput.log
echo.
echo  Press CTRL+C to stop the server.
echo ============================================================
echo.

cd /d "%PG_SERVER_INSTALL%"
"%PG_SERVER_INSTALL%\valheim_server.exe" ^
    -name "ProgressionGater Test" ^
    -port 2462 ^
    -world "progressiongater_test" ^
    -password "696969" ^
    -public 0
goto :eof

:no_server
echo ERROR: valheim_server.exe was not found at %PG_SERVER_INSTALL%
pause
exit /b 1

:no_bepinex
echo ERROR: BepInEx is not installed in the dedicated-server folder.
pause
exit /b 1

:no_doorstop
echo ERROR: winhttp.dll is missing from the dedicated-server folder.
pause
exit /b 1

:no_mod
echo ERROR: ProgressionGater.dll has not been built. Run scripts\build.ps1 first.
pause
exit /b 1

:copy_failed
echo ERROR: Could not deploy ProgressionGater.dll.
pause
exit /b 1
