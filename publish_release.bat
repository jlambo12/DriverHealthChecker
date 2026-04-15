@echo off
setlocal

echo ===============================
echo Driver Health Checker - Publish
echo ===============================
echo.

dotnet publish .\DriverHealthChecker.App\DriverHealthChecker.App.csproj -c Release -r win-x64 --self-contained false
if errorlevel 1 (
    echo.
    echo Publish завершился с ошибкой.
    pause
    exit /b 1
)

echo.
echo Publish завершен успешно.
echo Готовая папка:
echo .\DriverHealthChecker.App\bin\Release\net8.0-windows\publish
echo.
echo Следующий шаг:
echo 1. Установить Inno Setup
echo 2. Открыть файл DriverHealthCheckerInstaller.iss
echo 3. Нажать Build ^> Compile
echo.
pause
