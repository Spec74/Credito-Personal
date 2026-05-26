# Arranca Credito.Modern.Api desde bin/ (sin .csproj).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$apiDir = Join-Path $root 'Credito.Modern.Api\bin\Debug\net10.0'
$exe = Join-Path $apiDir 'Credito.Modern.Api.exe'
$envFile = Join-Path $root 'deploy\.env'

if (-not (Test-Path $exe)) {
  Write-Error "No se encontró $exe. Compile en otro equipo o restaure el source."
}

function Set-EnvFromDotEnv($path) {
  if (-not (Test-Path $path)) { return }
  Get-Content $path | ForEach-Object {
    $line = $_.Trim()
    if ($line -eq '' -or $line.StartsWith('#')) { return }
    $eq = $line.IndexOf('=')
    if ($eq -lt 1) { return }
    $key = $line.Substring(0, $eq).Trim()
    $val = $line.Substring($eq + 1).Trim()
    if ($key -eq 'CREDITO_DB_CONNECTION_STRING') {
      $env:CreditoDatabase__ConnectionString = $val
    }
    elseif ($key -eq 'JWT_SIGNING_KEY') {
      $env:Jwt__SigningKey = $val
    }
    elseif ($key -eq 'JWT_ISSUER') { $env:Jwt__Issuer = $val }
    elseif ($key -eq 'JWT_AUDIENCE') { $env:Jwt__Audience = $val }
    elseif ($key -eq 'JWT_REFRESH_AUDIENCE') { $env:Jwt__RefreshAudience = $val }
  }
}

Set-EnvFromDotEnv $envFile
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5288;https://localhost:7288'

if ([string]::IsNullOrWhiteSpace($env:CreditoDatabase__ConnectionString)) {
  Write-Warning 'CreditoDatabase__ConnectionString vacía. Revise deploy\.env'
}

Write-Host "Iniciando API en $apiDir ..."
Write-Host '  http://localhost:5288  |  https://localhost:7288  |  /swagger'
Set-Location $apiDir
& $exe
