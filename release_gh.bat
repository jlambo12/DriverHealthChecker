@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT_MARKER=DriverHealthChecker.sln"
set "CSPROJ=DriverHealthChecker.App\DriverHealthChecker.App.csproj"

if not exist ".\%ROOT_MARKER%" (
    echo [ERROR] Запусти скрипт из корня репозитория ^(не найден %ROOT_MARKER%^).
    exit /b 1
)

if not exist ".\%CSPROJ%" (
    echo [ERROR] Не найден файл проекта: %CSPROJ%.
    exit /b 1
)

set "VERSION=%~1"
if "%VERSION%"=="" (
    echo [ERROR] Версия не передана.
    echo Использование: .\release_gh.bat ^<version^>
    exit /b 1
)

set "CSPROJ_VERSION="
for /f "tokens=3 delims=<>" %%V in ('findstr /R /C:"^[ ]*<Version>.*</Version>" ".\%CSPROJ%"') do (
    set "CSPROJ_VERSION=%%V"
)

if "%CSPROJ_VERSION%"=="" (
    echo [ERROR] Не удалось прочитать ^<Version^> из %CSPROJ%.
    exit /b 1
)

if /I not "%VERSION%"=="%CSPROJ_VERSION%" (
    echo [ERROR] Версия не совпадает с csproj.
    echo         Аргумент : %VERSION%
    echo         csproj   : %CSPROJ_VERSION%
    exit /b 1
)

if "%GITHUB_TOKEN%"=="" (
    echo [ERROR] Переменная GITHUB_TOKEN не задана.
    echo         Пример: set GITHUB_TOKEN=ghp_xxx
    exit /b 1
)

echo ===============================
echo Driver Health Checker - Full GitHub Release
echo Version: %VERSION%
echo ===============================

echo [STEP 1/2] Сборка Velopack пакета...
call .\build_velopack_release.bat %VERSION%
if errorlevel 1 (
    echo [ERROR] Build этап завершился с ошибкой.
    exit /b 1
)

echo [STEP 2/2] Публикация/обновление GitHub Release...
call .\upload_velopack_github.bat %VERSION%
if errorlevel 1 (
    echo [ERROR] Upload этап завершился с ошибкой.
    exit /b 1
)

echo [OK] Релиз полностью завершен: v%VERSION%

echo [OK] Основной сценарий: .\release_gh.bat ^<version^>

exit /b 0
