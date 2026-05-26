$cs = (Get-Content 'D:\GitHub\Credito\modern\Credito.Modern.Api\appsettings.Development.json' -Raw | ConvertFrom-Json).CreditoDatabase.ConnectionString
if (-not $cs) {
  $cs = $env:CreditoDatabase__ConnectionString
}
Write-Host "CS found: $($cs -ne $null)"
Add-Type -AssemblyName 'System.Data'
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID('CREDITO.usp_RptCobroDiarioDetalle'))"
$def = $cmd.ExecuteScalar()
$conn.Close()
if ($def) { $def.Substring(0, [Math]::Min(4000, $def.Length)) } else { 'SP not found' }
