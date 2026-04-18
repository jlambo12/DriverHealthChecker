@echo off
setlocal EnableExtensions EnableDelayedExpansion

pushd "%~dp0" >nul 2>&1

set "ROOT_MARKER=DriverHealthChecker.sln"
set "CSPROJ=DriverHealthChecker.App\DriverHealthChecker.App.csproj"
set "REPO_URL=https://github.com/jlambo12/DriverHealthChecker"
set "VPK_VERSION=0.0.1298"
set "PUBLISH_FLAG=--publish"

if /I "%~2"=="--draft" set "PUBLISH_FLAG="
if /I "%~1"=="--draft" (
    echo [ERROR] Сначала укажи версию: .\upload_velopack_github.bat ^<version^> [--draft]
    popd >nul 2>&1
    exit /b 1
)

if not exist ".\%ROOT_MARKER%" (
    echo [ERROR] Запусти скрипт из корня репозитория ^(не найден %ROOT_MARKER%^).
    popd >nul 2>&1
    exit /b 1
)

if not exist ".\%CSPROJ%" (
    echo [ERROR] Не найден файл проекта: %CSPROJ%.
    popd >nul 2>&1
    exit /b 1
)

set "VERSION=%~1"
if "%VERSION%"=="" (
    echo [ERROR] Версия не передана.
    echo Использование: .\upload_velopack_github.bat ^<version^> [--draft]
    popd >nul 2>&1
    exit /b 1
)

set "CSPROJ_VERSION="
for /f "tokens=3 delims=<>" %%V in ('findstr /R /C:"^[ ]*<Version>.*</Version>" ".\%CSPROJ%"') do (
    set "CSPROJ_VERSION=%%V"
)

if "%CSPROJ_VERSION%"=="" (
    echo [ERROR] Не удалось прочитать ^<Version^> из %CSPROJ%.
    popd >nul 2>&1
    exit /b 1
)

if /I not "%VERSION%"=="%CSPROJ_VERSION%" (
    echo [ERROR] Версия не совпадает с csproj.
    echo         Аргумент : %VERSION%
    echo         csproj   : %CSPROJ_VERSION%
    popd >nul 2>&1
    exit /b 1
)

if "%GITHUB_TOKEN%"=="" (
    echo [ERROR] Переменная GITHUB_TOKEN не задана.
    echo         Пример: set GITHUB_TOKEN=ghp_xxx
    popd >nul 2>&1
    exit /b 1
)

if not exist ".\Releases" (
    echo [ERROR] Папка .\Releases не найдена. Сначала запусти build_velopack_release.bat.
    popd >nul 2>&1
    exit /b 1
)

dir /b .\Releases\*%VERSION%* >nul 2>&1
if errorlevel 1 (
    echo [ERROR] В .\Releases не найдено файлов для версии %VERSION%.
    echo         Сначала собери пакет: .\build_velopack_release.bat %VERSION%
    popd >nul 2>&1
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet CLI не найден в PATH.
    popd >nul 2>&1
    exit /b 1
)

where vpk >nul 2>&1
if errorlevel 1 (
    echo [INFO] vpk не найден в PATH. Пытаюсь установить глобально...
    dotnet tool install -g vpk --version %VPK_VERSION%
    if errorlevel 1 (
        echo [ERROR] Не удалось установить vpk версии %VPK_VERSION%.
        popd >nul 2>&1
        exit /b 1
    )
) else (
    echo [INFO] Обновляю vpk до версии %VPK_VERSION%...
    dotnet tool update -g vpk --version %VPK_VERSION%
    if errorlevel 1 (
        echo [ERROR] Не удалось обновить vpk до версии %VPK_VERSION%.
        popd >nul 2>&1
        exit /b 1
    )
)

echo ===============================
echo Driver Health Checker - GitHub Upload
echo Version : %VERSION%
echo Publish : %PUBLISH_FLAG%
echo Repo    : %REPO_URL%
echo ===============================

vpk upload github --outputDir .\Releases --repoUrl %REPO_URL% --token %GITHUB_TOKEN% %PUBLISH_FLAG% --releaseName "Driver Health Checker %VERSION%" --tag v%VERSION%
if errorlevel 1 (
    echo [ERROR] Ошибка на этапе vpk upload github.
    popd >nul 2>&1
    exit /b 1
)

if "%PUBLISH_FLAG%"=="" (
    echo [OK] GitHub Release создан/обновлён как draft: v%VERSION%
) else (
    echo [OK] GitHub Release опубликован/обновлен: v%VERSION%
)

echo [OK] Репозиторий: %REPO_URL%

popd >nul 2>&1
exit /b 0
