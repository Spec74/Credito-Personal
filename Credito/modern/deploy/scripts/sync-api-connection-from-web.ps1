# Alinea CreditoDatabase:ConnectionString de la API moderna con Web\Web.config (VENDIXEntities).
param(
    [string]$WebConfig = (Join-Path $PSScriptRoot "..\..\..\Web\Web.config"),
    [string]$ApiProject = (Join-Path $PSScriptRoot "..\..\Credito.Modern.Api\Credito.Modern.Api.csproj")
)

$ErrorActionPreference = "Stop"
$WebConfig = [System.IO.Path]::GetFullPath($WebConfig)
$ApiProject = [System.IO.Path]::GetFullPath($ApiProject)

if (-not (Test-Path $WebConfig)) {
    Write-Error "No se encontró Web.config en $WebConfig"
}

[xml]$xml = Get-Content -LiteralPath $WebConfig
$entityCs = [string](
    $xml.configuration.connectionStrings.add |
        Where-Object { $_.name -eq 'VENDIXEntities' } |
        Select-Object -First 1
).connectionString

if ([string]::IsNullOrWhiteSpace($entityCs)) {
    Write-Error 'connectionStrings/VENDIXEntities no encontrado en Web.config'
}

# Tras [xml], &quot; suele quedar como comilla normal.
if ($entityCs -match 'provider connection string="([^"]+)"') {
    $provider = $matches[1]
}
elseif ($entityCs -match 'provider connection string=&quot;([^&]+)&quot;') {
    $provider = $matches[1]
}
else {
    Write-Error 'No se pudo extraer provider connection string del Entity Framework'
}
$map = @{
    'data source'     = 'Server'
    'initial catalog' = 'Database'
    'user id'         = 'User Id'
    'password'        = 'Password'
}

$parts = @{}
foreach ($pair in ($provider -split ';')) {
    $pair = $pair.Trim()
    if (-not $pair) { continue }
    $kv = $pair -split '=', 2
    if ($kv.Length -lt 2) { continue }
    $key = $kv[0].Trim().ToLowerInvariant()
    $val = $kv[1].Trim()
    if ($map.ContainsKey($key)) {
        $parts[$map[$key]] = $val
    }
}

if (-not $parts['Server'] -or -not $parts['Database']) {
    Write-Error "Cadena incompleta tras parsear: $provider"
}

$modernCs = "Server=$($parts['Server']);Database=$($parts['Database']);User Id=$($parts['User Id']);Password=$($parts['Password']);TrustServerCertificate=True;Encrypt=False"

Write-Host "Web.config -> API:"
Write-Host $modernCs

dotnet user-secrets set "CreditoDatabase:ConnectionString" $modernCs --project $ApiProject
Write-Host "OK user-secrets actualizado. Reinicie dotnet run de Credito.Modern.Api."
