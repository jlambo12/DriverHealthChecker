@echo off
setlocal

set VERSION=%1
if "%VERSION%"=="" set VERSION=1.0.0

if "%GITHUB_TOKEN%"=="" (
    echo Переменная GITHUB_TOKEN не задана.
    echo Сначала выполни:
    echo set GITHUB_TOKEN=твой_токен
    pause
    exit /b 1
)

vpk upload github --outputDir .\Releases --repoUrl https://github.com/jlambo12/Driver-checker- --token %GITHUB_TOKEN% --publish --releaseName "Driver Health Checker %VERSION%" --tag v%VERSION%
if errorlevel 1 (
    echo.
    echo Ошибка на этапе upload.
    pause
    exit /b 1
)

echo.
echo Релиз опубликован на GitHub.
echo.
pause
