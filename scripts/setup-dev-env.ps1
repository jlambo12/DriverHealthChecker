[CmdletBinding()]
param(
    [switch]$SkipRestore
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Ensure-DotNet {
    Write-Step "Проверка и установка .NET SDK 10"

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($dotnet) {
        $hasSdk10 = (& dotnet --list-sdks) -match '^10\.'
        if ($hasSdk10) {
            Write-Host ".NET SDK 10 уже установлен." -ForegroundColor Green
            return
        }
    }

    $installScript = Join-Path $env:TEMP 'dotnet-install.ps1'
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installScript

    $installDir = Join-Path $env:USERPROFILE '.dotnet'
    & powershell -ExecutionPolicy Bypass -File $installScript -Channel '10.0' -InstallDir $installDir

    if (-not (Test-Path $installDir)) {
        throw "Не удалось установить .NET SDK в $installDir"
    }

    $env:PATH = "$installDir;$installDir\tools;$env:PATH"

    $hasSdk10AfterInstall = (& dotnet --list-sdks) -match '^10\.'
    if (-not $hasSdk10AfterInstall) {
        throw 'Установка завершилась без .NET SDK 10. Проверьте интернет-соединение и повторите запуск.'
    }

    Write-Host ".NET SDK 10 успешно установлен." -ForegroundColor Green
}

function Restore-Solution {
    Write-Step 'Восстановление зависимостей решения'
    dotnet restore .\DriverHealthChecker.sln
}

function Validate-Project {
    Write-Step 'Минимальная проверка структуры проекта'
    dotnet --info | Out-Host
    dotnet --list-sdks | Out-Host

    Write-Host "`nПроверка target framework:" -ForegroundColor Yellow
    $frameworkLine = Select-String -Path '.\DriverHealthChecker.App\DriverHealthChecker.App.csproj' -Pattern '<TargetFramework>(.+)</TargetFramework>'
    if (-not $frameworkLine) {
        throw 'Не найден TargetFramework в DriverHealthChecker.App.csproj'
    }

    Write-Host $frameworkLine.Line -ForegroundColor Green
}

Write-Step 'Подготовка окружения DriverHealthChecker'
Ensure-DotNet
Validate-Project

if (-not $SkipRestore) {
    Restore-Solution
}

Write-Host "`nГотово. Для запуска: dotnet run --project .\DriverHealthChecker.App\" -ForegroundColor Green
