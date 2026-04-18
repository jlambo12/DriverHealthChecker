@echo off
setlocal EnableExtensions EnableDelayedExpansion

pushd "%~dp0" >nul 2>&1

set "ROOT_MARKER=DriverHealthChecker.sln"
set "CSPROJ=DriverHealthChecker.App\DriverHealthChecker.App.csproj"
set "VPK_VERSION=0.0.1298"
set "PUBLISH_DIR=.\publish"
set "RELEASES_DIR=.\Releases"

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
    echo Использование: .\build_velopack_release.bat ^<version^>
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

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet CLI не найден в PATH.
    popd >nul 2>&1
    exit /b 1
)

echo ===============================
echo Driver Health Checker - Velopack Build
echo Version   : %VERSION%
echo VPK       : %VPK_VERSION%
echo PublishDir: %PUBLISH_DIR%
echo Releases  : %RELEASES_DIR%
echo ===============================

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

if exist "%PUBLISH_DIR%" (
    echo [INFO] Очищаю %PUBLISH_DIR%...
    rmdir /s /q "%PUBLISH_DIR%"
)
if exist "%RELEASES_DIR%" (
    echo [INFO] Очищаю %RELEASES_DIR%...
    rmdir /s /q "%RELEASES_DIR%"
)

dotnet publish .\DriverHealthChecker.App\DriverHealthChecker.App.csproj -c Release -r win-x64 --self-contained true -p:Version=%VERSION% -o %PUBLISH_DIR%
if errorlevel 1 (
    echo [ERROR] Ошибка на этапе dotnet publish.
    popd >nul 2>&1
    exit /b 1
)

vpk pack --packId jlambo12.DriverHealthChecker --packVersion %VERSION% --packDir %PUBLISH_DIR% --mainExe DriverHealthChecker.App.exe --packAuthors jlambo12 --packTitle "Driver Health Checker" --icon .\app.ico
if errorlevel 1 (
    echo [ERROR] Ошибка на этапе vpk pack.
    popd >nul 2>&1
    exit /b 1
)

echo [OK] Velopack build завершен успешно.
echo [OK] Папка релиза: %RELEASES_DIR%

popd >nul 2>&1
exit /b 0
