<#
.SYNOPSIS
  Gate de release: comprueba que la BD Azure/local tiene lo mínimo del moderno.

.PARAMETER ConnectionString
  Cadena ADO.NET (o use $env:CreditoDatabase__ConnectionString).

.EXAMPLE
  .\smoke-db-gate.ps1 -ConnectionString "Server=tcp:...;Database=CREDITO;User ID=...;Password=...;Encrypt=True;"
#>
param(
    [string] $ConnectionString = $env:CreditoDatabase__ConnectionString
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Error @'
Indique -ConnectionString o la variable CreditoDatabase__ConnectionString.

Sin secretos, puede verificar en Azure Portal → SQL → Query editor:

SELECT
  COL_LENGTH(N''CREDITO.Credito'', N''EsPrendario'') AS EsPrendario,
  OBJECT_ID(N''CREDITO.Prenda'', N''U'') AS Prenda,
  OBJECT_ID(N''CREDITO.usp_CreditoMora_Registrar'', N''P'') AS MoraRegistrar,
  OBJECT_ID(N''CREDITO.usp_MorosidadEmpresa'', N''P'') AS MorosidadEmpresa,
  (SELECT uses_quoted_identifier FROM sys.sql_modules
   WHERE object_id = OBJECT_ID(N''CREDITO.usp_Credito_Ins'')) AS CreditoInsQuotedOn,
  (SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_SCHEMA=N''MAESTRO'' AND TABLE_NAME=N''Usuario'' AND COLUMN_NAME=N''ClaveUsuario'') AS ClaveUsuarioLen;
'@
}

Add-Type -AssemblyName System.Data

function Invoke-Scalar([string] $Sql) {
    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        $cmd.CommandTimeout = 60
        return $cmd.ExecuteScalar()
    }
    finally {
        $conn.Close()
    }
}

$checks = @(
    @{ Name = 'EsPrendario'; Sql = "SELECT COL_LENGTH(N'CREDITO.Credito', N'EsPrendario')" },
    @{ Name = 'Prenda'; Sql = "SELECT OBJECT_ID(N'CREDITO.Prenda', N'U')" },
    @{ Name = 'usp_CreditoMora_Registrar'; Sql = "SELECT OBJECT_ID(N'CREDITO.usp_CreditoMora_Registrar', N'P')" },
    @{ Name = 'usp_MorosidadEmpresa'; Sql = "SELECT OBJECT_ID(N'CREDITO.usp_MorosidadEmpresa', N'P')" },
    @{ Name = 'CreditoInsQuotedOn'; Sql = "SELECT CAST(uses_quoted_identifier AS int) FROM sys.sql_modules WHERE object_id = OBJECT_ID(N'CREDITO.usp_Credito_Ins')" },
    @{ Name = 'ClaveUsuarioLen'; Sql = "SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=N'MAESTRO' AND TABLE_NAME=N'Usuario' AND COLUMN_NAME=N'ClaveUsuario'" }
)

$failed = 0
foreach ($c in $checks) {
    $v = Invoke-Scalar $c.Sql
    $ok = $null -ne $v -and [string]$v -ne '' -and [string]$v -ne '0'
    if ($c.Name -eq 'CreditoInsQuotedOn') { $ok = [int]$v -eq 1 }
    if ($c.Name -eq 'ClaveUsuarioLen') { $ok = [int]$v -ge 256 }
    $status = if ($ok) { 'OK' } else { 'FAIL'; $script:failed++ }
    Write-Host ("{0,-28} {1}  ({2})" -f $c.Name, $status, $v)
}

if ($failed -gt 0) {
    Write-Error "smoke-db-gate: $failed chequeo(s) fallaron."
    exit 1
}

Write-Host 'smoke-db-gate: todos los chequeos OK.'
exit 0
