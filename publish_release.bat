@echo off
setlocal EnableExtensions EnableDelayedExpansion

pushd "%~dp0" >nul 2>&1

set "ROOT_MARKER=DriverHealthChecker.sln"
set "CSPROJ=DriverHealthChecker.App\DriverHealthChecker.App.csproj"

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

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet CLI не найден в PATH.
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

echo ===============================
echo Driver Health Checker - Manual Publish Helper
echo Framework: net10.0-windows
echo Version  : %CSPROJ_VERSION%
echo ===============================

dotnet publish .\DriverHealthChecker.App\DriverHealthChecker.App.csproj -c Release -r win-x64 --self-contained false
if errorlevel 1 (
    echo [ERROR] Publish завершился с ошибкой.
    popd >nul 2>&1
    exit /b 1
)

echo [OK] Publish завершен успешно.
echo [OK] Папка публикации:
echo      .\DriverHealthChecker.App\bin\Release\net10.0-windows\win-x64\publish

echo.
echo Примечание: этот скрипт НЕ публикует обновление для установленных пользователей.
echo Для автообновления через GitHub Releases используй: .\release_gh.bat ^<version^>

popd >nul 2>&1
exit /b 0
