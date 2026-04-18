@echo off
setlocal EnableExtensions EnableDelayedExpansion

pushd "%~dp0" >nul 2>&1

set "ROOT_MARKER=DriverHealthChecker.sln"
set "CSPROJ=DriverHealthChecker.App\DriverHealthChecker.App.csproj"
set "FORCE_RELEASE=0"

if /I "%~2"=="--force" set "FORCE_RELEASE=1"
if /I "%~1"=="--force" (
    echo [ERROR] Сначала укажи версию: .\release_gh.bat ^<version^> [--force]
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
    echo Использование: .\release_gh.bat ^<version^> [--force]
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

where git >nul 2>&1
if errorlevel 1 (
    echo [ERROR] git не найден в PATH.
    popd >nul 2>&1
    exit /b 1
)

if "%FORCE_RELEASE%"=="0" (
    for /f "delims=" %%B in ('git rev-parse --abbrev-ref HEAD') do set "CURRENT_BRANCH=%%B"
    if /I not "%CURRENT_BRANCH%"=="main" if /I not "%CURRENT_BRANCH%"=="master" (
        echo [ERROR] Публикация разрешена только из main/master. Текущая ветка: %CURRENT_BRANCH%
        echo        Если уверен, запусти с флагом --force
        popd >nul 2>&1
        exit /b 1
    )

    git diff --quiet
    if errorlevel 1 (
        echo [ERROR] Есть незакоммиченные изменения. Сначала закоммить их или используй --force.
        popd >nul 2>&1
        exit /b 1
    )

    git diff --cached --quiet
    if errorlevel 1 (
        echo [ERROR] Есть изменения в staging. Сначала закоммить их или используй --force.
        popd >nul 2>&1
        exit /b 1
    )
)

echo ===============================
echo Driver Health Checker - Full GitHub Release
echo Version: %VERSION%
echo Force  : %FORCE_RELEASE%
echo ===============================

echo [STEP 1/2] Сборка Velopack пакета...
call .\build_velopack_release.bat %VERSION%
if errorlevel 1 (
    echo [ERROR] Build этап завершился с ошибкой.
    popd >nul 2>&1
    exit /b 1
)

echo [STEP 2/2] Публикация/обновление GitHub Release...
call .\upload_velopack_github.bat %VERSION%
if errorlevel 1 (
    echo [ERROR] Upload этап завершился с ошибкой.
    popd >nul 2>&1
    exit /b 1
)

echo [OK] Релиз полностью завершен: v%VERSION%
echo [OK] Основной сценарий: .\release_gh.bat ^<version^>

popd >nul 2>&1
exit /b 0
