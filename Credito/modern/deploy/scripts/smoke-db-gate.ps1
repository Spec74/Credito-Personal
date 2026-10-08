<#
.SYNOPSIS
  Gate de release: comprueba que la BD Azure/local tiene lo mínimo del moderno.

.PARAMETER ConnectionString
  Cadena ADO.NET (o use $env:CreditoDatabase__ConnectionString).

.EXAMPLE
  .\smoke-db-gate.ps1 -ConnectionString "Server=...;Database=CREDITO;..."
#>
param(
    [string] $ConnectionString = $env:CreditoDatabase__ConnectionString
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Error 'Indique -ConnectionString o CreditoDatabase__ConnectionString.'
}

function Invoke-Scalar([string] $Sql) {
    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        return $cmd.ExecuteScalar()
    }
    finally {
        $conn.Close()
    }
}

$checks = @(
    @{ Name = 'EsPrendario'; Sql = "SELECT COL_LENGTH('CREDITO.Credito','EsPrendario')" },
    @{ Name = 'Prenda'; Sql = "SELECT OBJECT_ID('CREDITO.Prenda','U')" },
    @{ Name = 'usp_CreditoMora_Registrar'; Sql = "SELECT OBJECT_ID('CREDITO.usp_CreditoMora_Registrar','P')" },
    @{ Name = 'usp_MorosidadEmpresa'; Sql = "SELECT OBJECT_ID('CREDITO.usp_MorosidadEmpresa','P')" },
    @{ Name = 'CreditoInsQuotedOn'; Sql = "SELECT CAST(uses_quoted_identifier AS int) FROM sys.sql_modules WHERE object_id = OBJECT_ID('CREDITO.usp_Credito_Ins')" },
    @{ Name = 'ClaveUsuarioLen'; Sql = "SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='MAESTRO' AND TABLE_NAME='Usuario' AND COLUMN_NAME='ClaveUsuario'" }
)

$failed = 0
foreach ($c in $checks) {
    $v = Invoke-Scalar $c.Sql
    $ok = $null -ne $v -and [string]$v -ne '' -and [string]$v -ne '0'
    if ($c.Name -eq 'CreditoInsQuotedOn') { $ok = [int]$v -eq 1 }
    if ($c.Name -eq 'ClaveUsuarioLen') { $ok = [int]$v -ge 256 }
    $status = if ($ok) { 'OK' } else { 'FAIL'; $failed++ }
    Write-Host ("{0,-28} {1}  ({2})" -f $c.Name, $status, $v)
}

if ($failed -gt 0) {
    Write-Error "smoke-db-gate: $failed chequeo(s) fallaron."
    exit 1
}

Write-Host 'smoke-db-gate: todos los chequeos OK.'
exit 0
