<#
.SYNOPSIS
    Exporta el esquema de la base de datos a archivos versionables (un archivo por objeto).

.DESCRIPTION
    Genera un snapshot solo-esquema (sin datos) bajo db/schema para que cada entrega de base
    de datos se pueda revisar con `git diff`: tablas nuevas, columnas alteradas y cambios en
    el cuerpo de procedimientos, vistas, funciones y triggers.

    No imprime ni guarda la cadena de conexion.

.PARAMETER ConnectionString
    Cadena de conexion. Si se omite, se lee CREDITO_DB_CONNECTION_STRING del archivo -EnvFile.

.PARAMETER EnvFile
    Archivo .env desde donde leer la cadena de conexion. Por defecto deploy/.env.

.PARAMETER OutputPath
    Carpeta destino del snapshot. Por defecto db/schema en la raiz de Credito/modern.

.EXAMPLE
    ./export-db-schema.ps1
    ./export-db-schema.ps1 -EnvFile ../.env.nueva -OutputPath ../../db/schema-nueva
#>
[CmdletBinding()]
param(
    [string]$ConnectionString,
    [string]$EnvFile,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$modernRoot = Resolve-Path (Join-Path $scriptDir '..\..')

if (-not $EnvFile) { $EnvFile = Join-Path $modernRoot 'deploy\.env' }
if (-not $OutputPath) { $OutputPath = Join-Path $modernRoot 'db\schema' }

if (-not $ConnectionString) {
    if (-not (Test-Path $EnvFile)) {
        throw "No se encontro $EnvFile. Pase -ConnectionString o indique -EnvFile."
    }
    $line = Get-Content $EnvFile | Where-Object { $_ -match '^\s*CREDITO_DB_CONNECTION_STRING\s*=' } | Select-Object -First 1
    if (-not $line) { throw "CREDITO_DB_CONNECTION_STRING no esta definido en $EnvFile." }
    $ConnectionString = ($line -split '=', 2)[1].Trim().Trim('"')
}

Add-Type -AssemblyName System.Data

function Invoke-SchemaQuery {
    param([string]$Sql)

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Sql
        $command.CommandTimeout = 180
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        $table = New-Object System.Data.DataTable
        [void]$adapter.Fill($table)
        return $table
    }
    finally {
        $connection.Dispose()
    }
}

function Write-ObjectFile {
    param([string]$Path, [string]$Content)

    $dir = Split-Path -Parent $Path
    if (-not (Test-Path $dir)) { [void](New-Item -ItemType Directory -Path $dir -Force) }
    # Sin BOM y con LF para que los diffs sean estables entre maquinas.
    $normalized = $Content -replace "`r`n", "`n"
    [IO.File]::WriteAllText($Path, $normalized, (New-Object Text.UTF8Encoding $false))
}

function Format-ColumnType {
    param($Row)

    $type = $Row.DataType
    switch -Regex ($type) {
        '^(nvarchar|nchar)$' {
            $len = if ($Row.MaxLength -eq -1) { 'max' } else { [int]$Row.MaxLength / 2 }
            return "$type($len)"
        }
        '^(varchar|char|varbinary|binary)$' {
            $len = if ($Row.MaxLength -eq -1) { 'max' } else { [int]$Row.MaxLength }
            return "$type($len)"
        }
        '^(decimal|numeric)$' { return "$type($($Row.Precision),$($Row.Scale))" }
        '^(datetime2|time|datetimeoffset)$' { return "$type($($Row.Scale))" }
        default { return $type }
    }
}

Write-Host "Exportando esquema hacia $OutputPath"

# --- Limpieza del snapshot anterior para que los objetos borrados desaparezcan del diff ---
foreach ($sub in 'tables', 'routines') {
    $target = Join-Path $OutputPath $sub
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
}

# --- Tablas y columnas ---
$columns = Invoke-SchemaQuery @'
SELECT s.name AS SchemaName, t.name AS TableName, c.column_id AS ColumnId, c.name AS ColumnName,
       ty.name AS DataType, c.max_length AS MaxLength, c.precision AS Precision, c.scale AS Scale,
       c.is_nullable AS IsNullable, c.is_identity AS IsIdentity, dc.definition AS DefaultDefinition
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;
'@

$indexes = Invoke-SchemaQuery @'
SELECT s.name AS SchemaName, t.name AS TableName, i.name AS IndexName, i.type_desc AS TypeDesc,
       i.is_unique AS IsUnique, i.is_primary_key AS IsPrimaryKey, c.name AS ColumnName,
       ic.key_ordinal AS KeyOrdinal, ic.is_included_column AS IsIncluded, ic.is_descending_key AS IsDescending
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.type > 0 AND t.is_ms_shipped = 0
ORDER BY s.name, t.name, i.name, ic.is_included_column, ic.key_ordinal;
'@

$foreignKeys = Invoke-SchemaQuery @'
SELECT s.name AS SchemaName, t.name AS TableName, fk.name AS FkName,
       rs.name AS RefSchema, rt.name AS RefTable,
       pc.name AS ColumnName, rc.name AS RefColumn, fkc.constraint_column_id AS Ordinal
FROM sys.foreign_keys fk
JOIN sys.tables t ON t.object_id = fk.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
ORDER BY s.name, t.name, fk.name, fkc.constraint_column_id;
'@

$tableKeys = $columns | ForEach-Object { "$($_.SchemaName).$($_.TableName)" } | Sort-Object -Unique
foreach ($key in $tableKeys) {
    $parts = $key -split '\.', 2
    $schemaName = $parts[0]
    $tableName = $parts[1]

    $lines = New-Object Collections.Generic.List[string]
    $lines.Add("-- Tabla: [$schemaName].[$tableName]")
    $lines.Add('')
    $lines.Add("CREATE TABLE [$schemaName].[$tableName] (")

    $tableColumns = $columns | Where-Object { $_.SchemaName -eq $schemaName -and $_.TableName -eq $tableName }
    $columnLines = foreach ($col in $tableColumns) {
        $definition = "    [$($col.ColumnName)] $(Format-ColumnType $col)"
        if ($col.IsIdentity) { $definition += ' IDENTITY' }
        $definition += if ($col.IsNullable) { ' NULL' } else { ' NOT NULL' }
        if ($col.DefaultDefinition -isnot [DBNull]) { $definition += " DEFAULT $($col.DefaultDefinition)" }
        $definition
    }
    $lines.Add(($columnLines -join ",`n"))
    $lines.Add(');')

    $tableIndexes = $indexes | Where-Object { $_.SchemaName -eq $schemaName -and $_.TableName -eq $tableName }
    if ($tableIndexes) {
        $lines.Add('')
        $lines.Add('-- Indices')
        foreach ($indexName in ($tableIndexes | ForEach-Object { $_.IndexName } | Sort-Object -Unique)) {
            # @() es obligatorio: indexar un DataRow suelto devuelve una columna, no la fila.
            $group = @($tableIndexes | Where-Object { $_.IndexName -eq $indexName })
            $keyCols = ($group | Where-Object { -not $_.IsIncluded } |
                ForEach-Object { "[$($_.ColumnName)]" + $(if ($_.IsDescending) { ' DESC' } else { '' }) }) -join ', '
            $included = ($group | Where-Object { $_.IsIncluded } | ForEach-Object { "[$($_.ColumnName)]" }) -join ', '
            $kind = if ($group[0].IsPrimaryKey) { 'PRIMARY KEY' } elseif ($group[0].IsUnique) { 'UNIQUE' } else { 'INDEX' }
            $line = "--   $kind $indexName ($($group[0].TypeDesc)): $keyCols"
            if ($included) { $line += " INCLUDE ($included)" }
            $lines.Add($line)
        }
    }

    $tableFks = $foreignKeys | Where-Object { $_.SchemaName -eq $schemaName -and $_.TableName -eq $tableName }
    if ($tableFks) {
        $lines.Add('')
        $lines.Add('-- Claves foraneas')
        foreach ($fkName in ($tableFks | ForEach-Object { $_.FkName } | Sort-Object -Unique)) {
            $group = @($tableFks | Where-Object { $_.FkName -eq $fkName })
            $local = ($group | ForEach-Object { "[$($_.ColumnName)]" }) -join ', '
            $remote = ($group | ForEach-Object { "[$($_.RefColumn)]" }) -join ', '
            $lines.Add("--   $fkName : ($local) -> [$($group[0].RefSchema)].[$($group[0].RefTable)] ($remote)")
        }
    }

    Write-ObjectFile -Path (Join-Path $OutputPath "tables\$schemaName.$tableName.sql") -Content (($lines -join "`n") + "`n")
}

# --- Procedimientos, vistas, funciones y triggers ---
$modules = Invoke-SchemaQuery @'
SELECT s.name AS SchemaName, o.name AS ObjectName, o.type_desc AS TypeDesc, m.definition AS Definition
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.is_ms_shipped = 0
ORDER BY s.name, o.name;
'@

foreach ($module in $modules) {
    $folder = switch -Wildcard ($module.TypeDesc) {
        'SQL_STORED_PROCEDURE' { 'procedures' }
        '*FUNCTION*' { 'functions' }
        'VIEW' { 'views' }
        'SQL_TRIGGER' { 'triggers' }
        default { 'otros' }
    }
    $path = Join-Path $OutputPath "routines\$folder\$($module.SchemaName).$($module.ObjectName).sql"
    Write-ObjectFile -Path $path -Content ($module.Definition.TrimEnd() + "`n")
}

# --- Manifiesto ---
$manifest = New-Object Collections.Generic.List[string]
$manifest.Add('# Snapshot de esquema')
$manifest.Add('')
$manifest.Add('Generado por `deploy/scripts/export-db-schema.ps1`. Solo esquema, sin datos.')
$manifest.Add('Regenerar tras cada entrega de base de datos y revisar el `git diff`.')
$manifest.Add('')
$manifest.Add('| Tipo | Cantidad |')
$manifest.Add('| --- | --- |')
$manifest.Add("| Tablas | $($tableKeys.Count) |")
foreach ($group in ($modules | Group-Object TypeDesc | Sort-Object Name)) {
    $manifest.Add("| $($group.Name) | $($group.Count) |")
}
$manifest.Add('')
$manifest.Add('## Esquemas')
$manifest.Add('')
foreach ($group in ($columns | Group-Object SchemaName | Sort-Object Name)) {
    $count = ($group.Group | ForEach-Object { $_.TableName } | Sort-Object -Unique).Count
    $manifest.Add("- ``$($group.Name)``: $count tablas")
}

Write-ObjectFile -Path (Join-Path $OutputPath 'README.md') -Content (($manifest -join "`n") + "`n")

Write-Host "Listo. Tablas: $($tableKeys.Count). Modulos: $($modules.Count)."
