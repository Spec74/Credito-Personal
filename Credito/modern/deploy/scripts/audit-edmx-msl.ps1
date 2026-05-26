# Detecta columnas en SSDL sin ScalarProperty en MSL (causa tipica EntityCommandExecutionException).
param(
    [string]$EdmxPath = (Join-Path $PSScriptRoot "..\..\..\ITB.VENDIX.DA\VENDIXModel.edmx")
)

$ErrorActionPreference = "Stop"
$EdmxPath = [System.IO.Path]::GetFullPath($EdmxPath)
if (-not (Test-Path $EdmxPath)) { throw "No existe $EdmxPath" }

[xml]$xml = Get-Content -LiteralPath $EdmxPath
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("edmx", "http://schemas.microsoft.com/ado/2009/11/edmx")
$ns.AddNamespace("ssdl", "http://schemas.microsoft.com/ado/2009/11/edm/ssdl")
$ns.AddNamespace("csdl", "http://schemas.microsoft.com/ado/2009/11/edm")
$ns.AddNamespace("m", "http://schemas.microsoft.com/ado/2009/11/mapping/cs")

$storage = $xml.SelectSingleNode("//edmx:Runtime/edmx:StorageModels", $ns)
$mapping = $xml.SelectSingleNode("//edmx:Runtime/edmx:Mappings", $ns)

$storeEntities = @{}
foreach ($et in $storage.SelectNodes("//ssdl:EntityType", $ns)) {
    $name = $et.GetAttribute("Name")
    $cols = @($et.SelectNodes("ssdl:Property", $ns) | ForEach-Object { $_.GetAttribute("Name") })
    $storeEntities[$name] = $cols
}

$issues = [System.Collections.Generic.List[string]]::new()
foreach ($esm in $mapping.SelectNodes("//m:EntitySetMapping", $ns)) {
    $setName = $esm.GetAttribute("Name")
    foreach ($etm in $esm.SelectNodes("m:EntityTypeMapping", $ns)) {
        $typeName = ($etm.GetAttribute("TypeName") -split '\.')[-1]
        $frag = $etm.SelectSingleNode("m:MappingFragment", $ns)
        if (-not $frag) { continue }
        $table = $frag.GetAttribute("StoreEntitySet")
        $mapped = @($frag.SelectNodes("m:ScalarProperty", $ns) | ForEach-Object { $_.GetAttribute("Name") })
        if (-not $storeEntities.ContainsKey($table)) { continue }
        foreach ($col in $storeEntities[$table]) {
            if ($mapped -notcontains $col) {
                $issues.Add("$setName / tabla $table -> falta MSL para columna '$col'")
            }
        }
    }
}

if ($issues.Count -eq 0) {
    Write-Host "OK: no hay columnas SSDL sin mapeo MSL." -ForegroundColor Green
    exit 0
}

Write-Host "Columnas sin mapeo MSL ($($issues.Count)):" -ForegroundColor Red
$issues | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
exit 1
