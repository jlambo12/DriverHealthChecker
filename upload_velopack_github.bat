@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT_MARKER=DriverHealthChecker.sln"
set "CSPROJ=DriverHealthChecker.App\DriverHealthChecker.App.csproj"
set "REPO_URL=https://github.com/jlambo12/DriverHealthChecker"

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
    echo Использование: .\upload_velopack_github.bat ^<version^>
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

if not exist ".\Releases" (
    echo [ERROR] Папка .\Releases не найдена. Сначала запусти build_velopack_release.bat.
    exit /b 1
)

dotnet tool update -g vpk --version 0.0.1298
if errorlevel 1 (
    dotnet tool install -g vpk --version 0.0.1298
)

if errorlevel 1 (
    echo [ERROR] Не удалось установить или обновить vpk.
    exit /b 1
)

vpk upload github --outputDir .\Releases --repoUrl %REPO_URL% --token %GITHUB_TOKEN% --publish --releaseName "Driver Health Checker %VERSION%" --tag v%VERSION%
if errorlevel 1 (
    echo [ERROR] Ошибка на этапе vpk upload github.
    exit /b 1
)

echo [OK] GitHub Release опубликован/обновлен: v%VERSION%

echo [OK] Репозиторий: %REPO_URL%

exit /b 0
