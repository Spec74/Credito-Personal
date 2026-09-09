<#
.SYNOPSIS
    Restaura un backup .bak en una base nueva, sin tocar la base existente.

.DESCRIPTION
    Pensado para recibir cada entrega de base de datos del cliente y poder compararla contra
    la actual. Restaura siempre con un nombre de base distinto, reubicando los archivos a la
    ruta de datos por defecto de la instancia.

    Flujo habitual tras la restauracion:
        ./export-db-schema.ps1 -ConnectionString "...;Database=CreditoNueva;..." -OutputPath ../../db/schema
        git diff db/schema        # muestra que cambio respecto de la entrega anterior

    Requisito: la cuenta de servicio de SQL Server debe poder leer -BackupPath. Si el archivo
    esta en una carpeta de usuario, copielo antes a una ruta accesible (por ejemplo C:\Backups).

.PARAMETER BackupPath
    Ruta del archivo .bak, tal como la ve el servidor SQL.

.PARAMETER DatabaseName
    Nombre de la base destino. Debe ser distinto del de la base en uso.

.PARAMETER ServerInstance
    Instancia SQL Server. Por defecto localhost con autenticacion integrada.

.PARAMETER ConnectionString
    Cadena de conexion completa a master. Alternativa a -ServerInstance.

.PARAMETER Force
    Sobrescribe la base destino si ya existe.

.EXAMPLE
    ./restore-db-backup.ps1 -BackupPath C:\Backups\vendix_2026_09.bak -DatabaseName CreditoNueva
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BackupPath,
    [Parameter(Mandatory = $true)][string]$DatabaseName,
    [string]$ServerInstance = 'localhost',
    [string]$ConnectionString,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

if ($DatabaseName -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    throw "Nombre de base invalido: '$DatabaseName'. Use solo letras, numeros y guion bajo."
}

if (-not $ConnectionString) {
    $ConnectionString = "Server=$ServerInstance;Database=master;Integrated Security=true;TrustServerCertificate=true;"
}

Add-Type -AssemblyName System.Data

function Invoke-Master {
    param([string]$Sql, [hashtable]$Parameters = @{}, [int]$Timeout = 3600)

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Sql
        $command.CommandTimeout = $Timeout
        foreach ($key in $Parameters.Keys) {
            [void]$command.Parameters.AddWithValue($key, $Parameters[$key])
        }
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        $table = New-Object System.Data.DataTable
        [void]$adapter.Fill($table)
        # La coma evita que PowerShell desenrolle el DataTable en filas sueltas al devolverlo.
        return , $table
    }
    finally {
        $connection.Dispose()
    }
}

Write-Host "Leyendo cabecera del backup..."
$header = Invoke-Master 'RESTORE HEADERONLY FROM DISK = @ruta;' @{ ruta = $BackupPath }
if ($header.Rows.Count -eq 0) { throw "El backup no contiene conjuntos de respaldo." }
$info = $header.Rows[0]
Write-Host "  Base origen : $($info.DatabaseName)"
Write-Host "  Fecha       : $($info.BackupFinishDate)"
Write-Host "  Version SQL : $($info.SoftwareVersionMajor).$($info.SoftwareVersionMinor)"

$existe = Invoke-Master 'SELECT database_id FROM sys.databases WHERE name = @nombre;' @{ nombre = $DatabaseName }
if ($existe.Rows.Count -gt 0 -and -not $Force) {
    throw "La base '$DatabaseName' ya existe. Use -Force para sobrescribirla o elija otro nombre."
}

$rutas = Invoke-Master "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000)) AS DataPath, CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(4000)) AS LogPath;"
$dataPath = $rutas.Rows[0].DataPath
$logPath = $rutas.Rows[0].LogPath

Write-Host "Leyendo lista de archivos..."
$archivos = Invoke-Master 'RESTORE FILELISTONLY FROM DISK = @ruta;' @{ ruta = $BackupPath }

$moves = foreach ($archivo in $archivos) {
    $extension = if ($archivo.Type -eq 'L') { '.ldf' } else { '.mdf' }
    $carpeta = if ($archivo.Type -eq 'L') { $logPath } else { $dataPath }
    $destino = Join-Path $carpeta "$DatabaseName`_$($archivo.FileId)$extension"
    "  MOVE N'$($archivo.LogicalName.Replace("'", "''"))' TO N'$($destino.Replace("'", "''"))'"
}

$sqlRestore = @"
RESTORE DATABASE [$DatabaseName]
FROM DISK = @ruta
WITH
$($moves -join ",`n"),
  REPLACE,
  RECOVERY,
  STATS = 10;
"@

Write-Host "Restaurando en '$DatabaseName' (puede tardar varios minutos)..."
[void](Invoke-Master $sqlRestore @{ ruta = $BackupPath })

$verificacion = Invoke-Master @"
SELECT
    (SELECT COUNT(*) FROM [$DatabaseName].sys.tables WHERE is_ms_shipped = 0) AS Tablas,
    (SELECT COUNT(*) FROM [$DatabaseName].sys.procedures WHERE is_ms_shipped = 0) AS Procedimientos;
"@

Write-Host ""
Write-Host "Restauracion completa: $($verificacion.Rows[0].Tablas) tablas, $($verificacion.Rows[0].Procedimientos) procedimientos."
Write-Host ""
Write-Host "Siguiente paso, extraer el esquema para comparar contra la entrega anterior:"
Write-Host "  ./export-db-schema.ps1 -ConnectionString `"Server=$ServerInstance;Database=$DatabaseName;Integrated Security=true;TrustServerCertificate=true;`""
