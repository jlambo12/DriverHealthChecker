param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "[1/3] dotnet restore..." -ForegroundColor Cyan
dotnet restore DriverHealthChecker.sln

Write-Host "[2/3] dotnet build..." -ForegroundColor Cyan
dotnet build DriverHealthChecker.sln --configuration $Configuration --no-restore

Write-Host "[3/3] dotnet test..." -ForegroundColor Cyan
dotnet test DriverHealthChecker.Tests/DriverHealthChecker.Tests.csproj --configuration $Configuration --no-build

Write-Host "Validation completed successfully." -ForegroundColor Green
