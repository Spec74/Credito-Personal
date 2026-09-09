<#
.SYNOPSIS
    Aplica las migraciones propias del sistema moderno sobre una base entregada por el cliente.

.DESCRIPTION
    Las entregas de base del cliente no incluyen las adiciones del sistema moderno
    (geolocalizacion, credito prendario, mora postergada, compatibilidad de usp_Credito_Ins).
    Este script reaplica todos los .sql de deploy/sql en orden alfabetico.

    Todos los scripts deben ser idempotentes: se ejecutan tras cada restauracion.

    Flujo completo al recibir una base nueva:
        ./restore-db-backup.ps1 -BackupPath ... -DatabaseName CREDITO_aaaammdd
        ./apply-migrations.ps1  -ConnectionString "...Database=CREDITO_aaaammdd..."
        ./export-db-schema.ps1  -ConnectionString "...Database=CREDITO_aaaammdd..."
        dotnet test                                  # el contrato codigo<->esquema debe pasar

.PARAMETER ConnectionString
    Cadena de conexion a la base destino. Si se omite se lee de -EnvFile.

.PARAMETER EnvFile
    Archivo .env con CREDITO_DB_CONNECTION_STRING. Por defecto deploy/.env.

.PARAMETER SqlPath
    Carpeta con los scripts. Por defecto deploy/sql.

.PARAMETER WhatIf
    Muestra que se ejecutaria sin aplicar nada.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$ConnectionString,
    [string]$EnvFile,
    [string]$SqlPath
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$modernRoot = Resolve-Path (Join-Path $scriptDir '..\..')

if (-not $EnvFile) { $EnvFile = Join-Path $modernRoot 'deploy\.env' }
if (-not $SqlPath) { $SqlPath = Join-Path $modernRoot 'deploy\sql' }

if (-not $ConnectionString) {
    $line = Get-Content $EnvFile | Where-Object { $_ -match '^\s*CREDITO_DB_CONNECTION_STRING\s*=' } | Select-Object -First 1
    if (-not $line) { throw "CREDITO_DB_CONNECTION_STRING no esta definido en $EnvFile." }
    $ConnectionString = ($line -split '=', 2)[1].Trim().Trim('"')
}

Add-Type -AssemblyName System.Data

$archivos = @(Get-ChildItem -Path $SqlPath -Filter *.sql -File | Sort-Object Name)
if ($archivos.Count -eq 0) { throw "No hay scripts .sql en $SqlPath." }

Write-Host "Aplicando $($archivos.Count) migraciones desde $SqlPath"

$connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
try {
    $connection.Open()

    foreach ($archivo in $archivos) {
        Write-Host "  $($archivo.Name)"

        $contenido = Get-Content $archivo.FullName -Raw
        # Separa por lotes GO en linea propia, como haria sqlcmd.
        $lotes = [regex]::Split($contenido, '(?im)^\s*GO\s*$') |
            Where-Object { $_.Trim().Length -gt 0 }

        foreach ($lote in $lotes) {
            if (-not $PSCmdlet.ShouldProcess($archivo.Name, 'ejecutar lote')) { continue }

            $command = $connection.CreateCommand()
            $command.CommandText = $lote
            $command.CommandTimeout = 600
            try {
                [void]$command.ExecuteNonQuery()
            }
            catch {
                throw "Fallo en '$($archivo.Name)': $($_.Exception.Message)"
            }
        }
    }
}
finally {
    $connection.Dispose()
}

Write-Host ""
Write-Host "Migraciones aplicadas."
