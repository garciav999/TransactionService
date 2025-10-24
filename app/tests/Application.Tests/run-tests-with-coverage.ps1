# Script para ejecutar pruebas con reporte de cobertura en consola

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Ejecutando Pruebas - Application Layer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Ejecutar pruebas con cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Resumen de Cobertura" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Buscar el archivo de cobertura más reciente
$coverageFile = Get-ChildItem -Path "./TestResults" -Filter "coverage.cobertura.xml" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($coverageFile) {
    Write-Host "Archivo de cobertura generado:" -ForegroundColor Yellow
    Write-Host $coverageFile.FullName -ForegroundColor White
    Write-Host ""
    
    # Leer el XML y mostrar estadísticas
    [xml]$coverage = Get-Content $coverageFile.FullName
    
    $lineRate = [math]::Round([decimal]$coverage.coverage.'line-rate' * 100, 2)
    $branchRate = [math]::Round([decimal]$coverage.coverage.'branch-rate' * 100, 2)
    
    Write-Host "Cobertura de Líneas:  " -NoNewline -ForegroundColor Cyan
    Write-Host "$lineRate%" -ForegroundColor Green
    
    Write-Host "Cobertura de Ramas:   " -NoNewline -ForegroundColor Cyan
    Write-Host "$branchRate%" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Detalles por Clase:" -ForegroundColor Yellow
    Write-Host "-------------------" -ForegroundColor Yellow
    
    foreach ($package in $coverage.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
            $className = $class.name
            $classLineRate = [math]::Round([decimal]$class.'line-rate' * 100, 2)
            $classBranchRate = [math]::Round([decimal]$class.'branch-rate' * 100, 2)
            
            Write-Host "  $className" -ForegroundColor White
            Write-Host "    Líneas: $classLineRate% | Ramas: $classBranchRate%" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "No se encontró archivo de cobertura." -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
