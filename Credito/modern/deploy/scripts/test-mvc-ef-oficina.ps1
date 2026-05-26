# Prueba la misma consulta EF que Home/Login: OficinaBL.Listar(x => x.Estado)
# Requiere Web compilado (Visual Studio F6). Muestra InnerException si falla como en IIS Express.
param(
    [string]$WebBin = (Join-Path $PSScriptRoot "..\..\..\Web\bin"),
    [string]$WebConfig = (Join-Path $PSScriptRoot "..\..\..\Web\Web.config")
)

$ErrorActionPreference = "Stop"
$WebBin = [System.IO.Path]::GetFullPath($WebBin)
$WebConfig = [System.IO.Path]::GetFullPath($WebConfig)

function Get-VendixEntityConnectionString {
    param([string]$ConfigPath)
    [xml]$xml = Get-Content -LiteralPath $ConfigPath
    $node = $xml.configuration.connectionStrings.add | Where-Object { $_.name -eq "VENDIXEntities" } | Select-Object -First 1
    if (-not $node) { throw "No hay connectionString VENDIXEntities en $ConfigPath" }
    return [string]$node.connectionString
}

function Test-WebConfigEfBindings {
    [xml]$xml = Get-Content -LiteralPath $WebConfig
    $ef = $xml.configuration.runtime.assemblyBinding.dependentAssembly |
        Where-Object { $_.assemblyIdentity.name -eq "EntityFramework" }
    if (-not $ef -or $ef.bindingRedirect.newVersion -ne "6.0.0.0") {
        Write-Warning "Web.config: falta bindingRedirect EntityFramework -> 6.0.0.0 (proyecto usa EF 6.5.1)."
        return $false
    }
    $cs = $xml.configuration.connectionStrings.add | Where-Object { $_.name -eq "VENDIXEntities" }
    if ($cs.connectionString -notmatch "localhost,14330") {
        Write-Warning "Web.config: VENDIXEntities no apunta a localhost,14330."
        return $false
    }
    return $true
}

if (-not (Test-Path $WebConfig)) { Write-Error "No se encuentra Web.config: $WebConfig" }
if (-not (Test-Path (Join-Path $WebBin "ITB.VENDIX.DA.dll"))) {
    Write-Error "Compile Web en Visual Studio (F6). Falta: $WebBin\ITB.VENDIX.DA.dll"
}

[void](Test-WebConfigEfBindings)
$entityCs = Get-VendixEntityConnectionString -ConfigPath $WebConfig

Add-Type -Path (Join-Path $WebBin "EntityFramework.dll")
Add-Type -Path (Join-Path $WebBin "EntityFramework.SqlServer.dll")
Add-Type -Path (Join-Path $WebBin "ITB.VENDIX.DA.dll")

$env:VENDIX_ENTITY_CS = $entityCs

$code = @'
using System;
using System.Data.Entity;
using System.Linq;
using ITB.VENDIX.DA;

public static class EfOficinaSmoke
{
    public static string Run()
    {
        var entityCs = Environment.GetEnvironmentVariable("VENDIX_ENTITY_CS");
        if (string.IsNullOrEmpty(entityCs))
            throw new InvalidOperationException("Variable VENDIX_ENTITY_CS no definida.");
        using (var db = new VENDIXEntities(entityCs))
        {
            db.Configuration.ProxyCreationEnabled = false;
            db.Configuration.LazyLoadingEnabled = false;
            var list = db.Set<Oficina>().Where(x => x.Estado).ToList();
            return string.Format("OK: {0} oficina(s) activa(s). Ej: {1}",
                list.Count,
                list.Count > 0 ? list[0].Denominacion : "(ninguna)");
        }
    }
}
'@

$refs = @(
    (Join-Path $WebBin "EntityFramework.dll"),
    (Join-Path $WebBin "EntityFramework.SqlServer.dll"),
    (Join-Path $WebBin "ITB.VENDIX.DA.dll"),
    "System.dll",
    "System.Core.dll",
    "System.Data.dll"
)
Add-Type -TypeDefinition $code -ReferencedAssemblies $refs -Language CSharp | Out-Null

try {
    Write-Host ([EfOficinaSmoke]::Run()) -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "FALLO EF (misma consulta que /Home/Login):" -ForegroundColor Red
    Write-Host $_.Exception.Message
    $inner = $_.Exception
    $depth = 0
    while ($inner.InnerException -and $depth -lt 10) {
        $inner = $inner.InnerException
        $depth++
        Write-Host ("  Inner[{0}] {1}: {2}" -f $depth, $inner.GetType().Name, $inner.Message) -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "Revise Web.config (binding EF 6.0.0.0 + VENDIXEntities) y reconstruya Web (F6)." -ForegroundColor Cyan
    exit 1
}
