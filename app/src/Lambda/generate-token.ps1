# Genera un JWT Token válido para el Transaction Service
param(
    [int]$ExpirationDays = 365
)

# Leer configuración
$appsettings = Get-Content "appsettings.json" | ConvertFrom-Json
$secretKey = $appsettings.Jwt.SecretKey
$issuer = $appsettings.Jwt.Issuer
$audience = $appsettings.Jwt.Audience

# Función para Base64Url encode
function ConvertTo-Base64Url {
    param([string]$text)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $base64 = [Convert]::ToBase64String($bytes)
    return $base64.Replace('+', '-').Replace('/', '_').TrimEnd('=')
}

# Crear header
$header = @{
    alg = "HS256"
    typ = "JWT"
} | ConvertTo-Json -Compress

# Crear payload
$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$expiration = [DateTimeOffset]::UtcNow.AddDays($ExpirationDays).ToUnixTimeSeconds()

$payload = @{
    sub = "demo-user"
    name = "Demo User"
    role = "admin"
    iss = $issuer
    aud = $audience
    iat = $now
    exp = $expiration
} | ConvertTo-Json -Compress

# Codificar header y payload
$headerEncoded = ConvertTo-Base64Url -text $header
$payloadEncoded = ConvertTo-Base64Url -text $payload

# Crear firma HMAC-SHA256
$message = "$headerEncoded.$payloadEncoded"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$keyBytes = [System.Text.Encoding]::UTF8.GetBytes($secretKey)

$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = $keyBytes
$signatureBytes = $hmac.ComputeHash($messageBytes)
$signatureBase64 = [Convert]::ToBase64String($signatureBytes)
$signatureEncoded = $signatureBase64.Replace('+', '-').Replace('/', '_').TrimEnd('=')

# JWT completo
$jwt = "$headerEncoded.$payloadEncoded.$signatureEncoded"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "JWT Token Generated Successfully!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Token Details:" -ForegroundColor Yellow
Write-Host "  User: Demo User (admin)" -ForegroundColor White
Write-Host "  Expires: $([DateTimeOffset]::FromUnixTimeSeconds($expiration).LocalDateTime)" -ForegroundColor Green
Write-Host "  Valid for: $ExpirationDays days" -ForegroundColor White
Write-Host ""
Write-Host "Your JWT Token:" -ForegroundColor Yellow
Write-Host $jwt -ForegroundColor White
Write-Host ""
Write-Host "Update appsettings.json with this token:" -ForegroundColor Cyan
Write-Host '"DemoToken": "' -NoNewline -ForegroundColor Gray
Write-Host $jwt -NoNewline -ForegroundColor Yellow  
Write-Host '"' -ForegroundColor Gray
Write-Host ""

# Copiar al portapapeles
Set-Clipboard -Value $jwt
Write-Host "Token copied to clipboard!" -ForegroundColor Green
