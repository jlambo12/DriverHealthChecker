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

set "CSPROJ_VERSION="
for /f "tokens=3 delims=<>" %%V in ('findstr /R /C:"^[ ]*<Version>.*</Version>" ".\%CSPROJ%"') do (
    set "CSPROJ_VERSION=%%V"
)

if "%CSPROJ_VERSION%"=="" (
    echo [ERROR] Не удалось прочитать ^<Version^> из %CSPROJ%.
    exit /b 1
)

echo ===============================
echo Driver Health Checker - Manual Publish Helper
echo Framework: net10.0-windows
echo Version  : %CSPROJ_VERSION%
echo ===============================

dotnet publish .\DriverHealthChecker.App\DriverHealthChecker.App.csproj -c Release -r win-x64 --self-contained false
if errorlevel 1 (
    echo [ERROR] Publish завершился с ошибкой.
    exit /b 1
)

echo [OK] Publish завершен успешно.
echo [OK] Папка публикации:
echo      .\DriverHealthChecker.App\bin\Release\net10.0-windows\win-x64\publish

echo.
echo Примечание: основной сценарий релиза в GitHub Releases — это .\release_gh.bat ^<version^>.

exit /b 0
