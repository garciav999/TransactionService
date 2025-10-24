# Script simple para ver solo el porcentaje de cobertura

$env:FLUENTASSERTIONS_DISABLECOMMUNITYWARNING = "true"

Write-Host ""
Write-Host "Ejecutando tests..." -ForegroundColor Cyan

dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/ --verbosity quiet --nologo

Write-Host ""
Write-Host "Cobertura de Codigo:" -ForegroundColor Green

$coverageFile = Get-ChildItem -Path "./TestResults" -Filter "coverage.cobertura.xml" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($coverageFile) {
    [xml]$coverage = Get-Content $coverageFile.FullName
    $lineRate = [math]::Round([decimal]$coverage.coverage.'line-rate' * 100, 2)
    $branchRate = [math]::Round([decimal]$coverage.coverage.'branch-rate' * 100, 2)
    
    Write-Host "  Lineas:  $lineRate%" -ForegroundColor Yellow
    Write-Host "  Ramas:   $branchRate%" -ForegroundColor Yellow
} else {
    Write-Host "  No se encontro archivo de cobertura" -ForegroundColor Red
}

Write-Host ""
