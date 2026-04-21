@echo off
setlocal

set "PROJECT_PATH=%~dp0"
if "%PROJECT_PATH:~-1%"=="\" set "PROJECT_PATH=%PROJECT_PATH:~0,-1%"

if defined UNITY_EXE (
    set "UNITY_PATH=%UNITY_EXE%"
) else (
    set "UNITY_PATH=D:\Unity 2022.3.0f1c1\Editor\2022.3.62f2c1\Editor\Unity.exe"
)

if not exist "%UNITY_PATH%" (
    echo [POPHero] Unity.exe not found:
    echo %UNITY_PATH%
    echo.
    echo You can either:
    echo 1. Edit this bat and update UNITY_PATH
    echo 2. Set a UNITY_EXE environment variable
    exit /b 1
)

if exist "%PROJECT_PATH%\\Temp\\UnityLockfile" (
    echo [POPHero] This project looks open in Unity already.
    echo [POPHero] If the Editor is open, use menu:
    echo [POPHero]   POPHero ^> Config ^> Rebuild Tables
    echo [POPHero] Or close Unity first, then run this bat again.
    exit /b 1
)

echo [POPHero] Rebuilding config tables...
echo [POPHero] Project: %PROJECT_PATH%
echo [POPHero] Unity:   %UNITY_PATH%
echo.

"%UNITY_PATH%" ^
  -batchmode ^
  -quit ^
  -projectPath "%PROJECT_PATH%" ^
  -executeMethod POPHero.ConfigTableImporter.RebuildTablesCli ^
  -logFile -

set "EXIT_CODE=%ERRORLEVEL%"
echo.

if "%EXIT_CODE%"=="0" (
    echo [POPHero] Config tables rebuilt successfully.
) else (
    echo [POPHero] Config table rebuild failed with exit code %EXIT_CODE%.
)

exit /b %EXIT_CODE%
