#!/bin/bash

# Script simple para ver cobertura en bash

export FLUENTASSERTIONS_DISABLECOMMUNITYWARNING=true

echo ""
echo "Ejecutando tests..."

dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/ --verbosity quiet --nologo 2>&1 | grep -v "MSBUILD"

echo ""
echo "Cobertura de Codigo:"

# Buscar archivo de cobertura
COVERAGE_FILE=$(find ./TestResults -name "coverage.cobertura.xml" -type f | head -1)

if [ -f "$COVERAGE_FILE" ]; then
    # Extraer line-rate y branch-rate del XML
    LINE_RATE=$(grep -oP 'line-rate="\K[^"]+' "$COVERAGE_FILE" | head -1)
    BRANCH_RATE=$(grep -oP 'branch-rate="\K[^"]+' "$COVERAGE_FILE" | head -1)
    
    # Convertir a porcentaje usando awk (bc no esta disponible en Git Bash)
    LINE_PCT=$(echo "$LINE_RATE" | awk '{printf "%.2f", $1 * 100}')
    BRANCH_PCT=$(echo "$BRANCH_RATE" | awk '{printf "%.2f", $1 * 100}')
    
    echo "  Lineas:  ${LINE_PCT}%"
    echo "  Ramas:   ${BRANCH_PCT}%"
else
    echo "  No se encontro archivo de cobertura"
fi

echo ""
