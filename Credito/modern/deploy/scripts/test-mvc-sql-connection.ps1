# Prueba la misma SQL que Web\Web.config (VENDIXEntities -> localhost,14330 CREDITO).
# Uso: .\deploy\scripts\test-mvc-sql-connection.ps1
#      .\deploy\scripts\test-mvc-sql-connection.ps1 -Password "tu-clave-sa"

param(
    [string]$Server = "localhost,14330",
    [string]$Database = "CREDITO",
    [string]$UserId = "sa",
    [string]$Password = "123456"
)

$ErrorActionPreference = "Stop"

$connStr = "Server=$Server;Database=$Database;User Id=$UserId;Password=$Password;TrustServerCertificate=True;Encrypt=True;Connection Timeout=10;"

Write-Host "== Probar SQL (paridad Web.config MVC)" -ForegroundColor Cyan
Write-Host "  Server=$Server Database=$Database User=$UserId" -ForegroundColor Gray

try {
    Add-Type -AssemblyName "System.Data"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT DB_NAME() AS Db, @@VERSION AS Ver"
    $reader = $cmd.ExecuteReader()
    if ($reader.Read()) {
        Write-Host "  OK conectado: $($reader['Db'])" -ForegroundColor Green
    }
    $reader.Close()
    $conn.Close()
    Write-Host "`nSi MVC sigue fallando, reinicia IIS Express (Shift+F5) tras corregir Web.config EF binding." -ForegroundColor Gray
}
catch {
    Write-Host "  FALLO: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "  Inner: $($_.Exception.InnerException.Message)" -ForegroundColor Red
    }
    Write-Host "`nAjusta password en Web\Web.config y modern\deploy\.env (misma instancia SQL)." -ForegroundColor Yellow
    exit 1
}
