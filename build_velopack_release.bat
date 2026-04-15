@echo off
setlocal

set VERSION=%1
if "%VERSION%"=="" set VERSION=1.0.0

echo ===============================
echo Driver Health Checker - Velopack Build
echo Version: %VERSION%
echo ===============================
echo.

dotnet tool update -g vpk --version 0.0.1298
if errorlevel 1 (
    dotnet tool install -g vpk --version 0.0.1298
)

if errorlevel 1 (
    echo.
    echo Не удалось установить или обновить vpk.
    pause
    exit /b 1
)

dotnet publish .\DriverHealthChecker.App\DriverHealthChecker.App.csproj -c Release -r win-x64 --self-contained true -p:Version=%VERSION% -o .\publish
if errorlevel 1 (
    echo.
    echo Ошибка на этапе dotnet publish.
    pause
    exit /b 1
)

vpk pack --packId jlambo12.DriverHealthChecker --packVersion %VERSION% --packDir .\publish --mainExe DriverHealthChecker.App.exe --packAuthors jlambo12 --packTitle "Driver Health Checker" --icon .\app.ico
if errorlevel 1 (
    echo.
    echo Ошибка на этапе vpk pack.
    pause
    exit /b 1
)

echo.
echo Готово.
echo Папка с Velopack-релизом: .\Releases
echo.
pause
