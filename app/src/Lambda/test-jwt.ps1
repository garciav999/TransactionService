# Script de prueba para validar JWT Token
# Asegúrate de que el servicio esté corriendo primero con: dotnet run

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Testing JWT Authentication" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Leer el token de appsettings.json
$appsettings = Get-Content "appsettings.json" | ConvertFrom-Json
$token = $appsettings.Jwt.DemoToken

Write-Host "Using token from appsettings.json:" -ForegroundColor Yellow
Write-Host $token.Substring(0, 50) + "..." -ForegroundColor Gray
Write-Host ""

# Decodificar el token para ver su contenido
$parts = $token.Split('.')
$payload = $parts[1]
# Agregar padding si es necesario
while ($payload.Length % 4 -ne 0) {
    $payload += "="
}
$payload = $payload.Replace('-', '+').Replace('_', '/')
$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload))
$payloadObj = $payloadJson | ConvertFrom-Json

Write-Host "Token Payload:" -ForegroundColor Yellow
Write-Host "  Subject: $($payloadObj.sub)"
Write-Host "  Name: $($payloadObj.name)"
Write-Host "  Role: $($payloadObj.role)"
Write-Host "  Issuer: $($payloadObj.iss)"
Write-Host "  Audience: $($payloadObj.aud)"
Write-Host "  Issued At: $([DateTimeOffset]::FromUnixTimeSeconds($payloadObj.iat).LocalDateTime)"
Write-Host "  Expires: $([DateTimeOffset]::FromUnixTimeSeconds($payloadObj.exp).LocalDateTime)" -ForegroundColor $(if ($payloadObj.exp -gt [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()) { "Green" } else { "Red" })
Write-Host ""

# Crear el cuerpo de la petición
$body = @{
    accountExternalIdDebit = "550e8400-e29b-41d4-a716-446655440000"
    accountExternalIdCredit = "650e8400-e29b-41d4-a716-446655440001"
    transferTypeId = 1
    value = 100.00
} | ConvertTo-Json

Write-Host "Request Body:" -ForegroundColor Yellow
Write-Host $body -ForegroundColor Gray
Write-Host ""

# Realizar la petición
Write-Host "Sending request to http://localhost:5050/..." -ForegroundColor Yellow
Write-Host ""

try {
    $headers = @{
        "Content-Type" = "application/json"
        "Authorization" = "Bearer $token"
    }
    
    $response = Invoke-WebRequest `
        -Uri "http://localhost:5050/" `
        -Method POST `
        -Headers $headers `
        -Body $body `
        -UseBasicParsing `
        -ErrorAction Stop
    
    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Yellow
    Write-Host $response.Content -ForegroundColor White
}
catch {
    Write-Host "FAILED!" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
        
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        
        Write-Host "Error Response:" -ForegroundColor Yellow
        Write-Host $responseBody -ForegroundColor Red
    }
    else {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Check if service is running on http://localhost:5050/" -ForegroundColor Gray
    Write-Host "2. Check if token signature matches SecretKey" -ForegroundColor Gray
    Write-Host "3. Check if Issuer matches: $($appsettings.Jwt.Issuer)" -ForegroundColor Gray
    Write-Host "4. Check if Audience matches: $($appsettings.Jwt.Audience)" -ForegroundColor Gray
    Write-Host "5. Check server logs for validation errors" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
