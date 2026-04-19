# LOCAL_VALIDATION

Use this flow before manual UI verification and before opening/merging a PR.

## Prerequisites
- .NET SDK from `global.json`
- Windows machine for WPF/UI smoke checks

## Quick command
```powershell
pwsh ./scripts/validate-local.ps1
```

## What it does
1. `dotnet restore DriverHealthChecker.sln`
2. `dotnet build DriverHealthChecker.sln --configuration Release --no-restore`
3. `dotnet test DriverHealthChecker.Tests/DriverHealthChecker.Tests.csproj --configuration Release --no-build`

## Manual smoke checks after CLI validation
1. Launch app and confirm initial window size is usable.
2. Verify `Сбросить фильтры` is visible without manual resize.
3. Run scan and rescan; verify summary/report lines update.
4. Verify action buttons for NVIDIA/Intel/Realtek/OEM recommendations.
