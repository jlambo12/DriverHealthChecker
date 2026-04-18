param(
    [string]$ReportsDir = "$env:LOCALAPPDATA\DriverHealthChecker\scan-history",
    [string]$OutputFile = ".\DriverHealthChecker.Tests\Fixtures\driver-classification-fixtures.generated.json"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ReportsDir)) {
    throw "Папка scan-history не найдена: $ReportsDir"
}

$reportFiles = Get-ChildItem -Path $ReportsDir -Filter 'scan-*.json' -File | Sort-Object Name
if (-not $reportFiles) {
    throw "В папке нет файлов scan-*.json: $ReportsDir"
}

$allItems = @()
foreach ($file in $reportFiles) {
    $json = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json
    if ($json.Items) {
        $allItems += $json.Items
    }
}

if (-not $allItems) {
    throw "В scan-report файлах не найдено Items."
}

$deduped = $allItems |
    Where-Object { $_.Category -ne 'DeviceRecommendation' } |
    Sort-Object Name, Manufacturer -Unique

$result = foreach ($item in $deduped) {
    if ($item.Category -eq 'HiddenSystem') {
        [pscustomobject]@{
            name = $item.Name
            manufacturer = $item.Manufacturer
            expectedCategory = ""
            shouldClassify = $false
            note = "Скрыто системным/служебным фильтром"
        }
        continue
    }

    [pscustomobject]@{
        name = $item.Name
        manufacturer = $item.Manufacturer
        expectedCategory = $item.Category
        shouldClassify = $true
        note = "Автогенерация из scan-history, проверь категорию вручную"
    }
}

$targetDir = Split-Path -Path $OutputFile -Parent
if ($targetDir -and -not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir | Out-Null
}

$result | ConvertTo-Json -Depth 5 | Set-Content -Path $OutputFile -Encoding UTF8
Write-Host "Готово: $OutputFile"
Write-Host "Записано записей: $($result.Count)"
