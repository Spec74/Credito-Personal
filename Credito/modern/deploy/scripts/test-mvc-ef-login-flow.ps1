# Prueba EF en la misma secuencia que Home/Login y Home/Autenticar.
param(
    [string]$WebBin = (Join-Path $PSScriptRoot "..\..\..\Web\bin"),
    [string]$WebConfig = (Join-Path $PSScriptRoot "..\..\..\Web\Web.config"),
    [string]$NombreUsuario = "",
    [int]$OficinaId = 1
)

$ErrorActionPreference = "Stop"
$WebBin = [System.IO.Path]::GetFullPath($WebBin)
$WebConfig = [System.IO.Path]::GetFullPath($WebConfig)

function Get-VendixEntityConnectionString {
    [xml]$xml = Get-Content -LiteralPath $WebConfig
    $node = $xml.configuration.connectionStrings.add | Where-Object { $_.name -eq "VENDIXEntities" } | Select-Object -First 1
    return [string]$node.connectionString
}

if (-not (Test-Path (Join-Path $WebBin "ITB.VENDIX.DA.dll"))) {
    Write-Error "Compile Web (F6). Falta ITB.VENDIX.DA.dll en $WebBin"
}

$entityCs = Get-VendixEntityConnectionString -ConfigPath $WebConfig
$env:VENDIX_ENTITY_CS = $entityCs
$env:TEST_USER = $NombreUsuario
$env:TEST_OFICINA = $OficinaId

Add-Type -Path (Join-Path $WebBin "EntityFramework.dll")
Add-Type -Path (Join-Path $WebBin "EntityFramework.SqlServer.dll")
Add-Type -Path (Join-Path $WebBin "ITB.VENDIX.DA.dll")

$code = @'
using System;
using System.Data.Entity;
using System.Linq;
using ITB.VENDIX.DA;

public static class EfLoginFlowSmoke
{
    static VENDIXEntities Db()
    {
        var cs = Environment.GetEnvironmentVariable("VENDIX_ENTITY_CS");
        var db = new VENDIXEntities(cs);
        db.Configuration.ProxyCreationEnabled = false;
        db.Configuration.LazyLoadingEnabled = false;
        return db;
    }

    public static void RunAll()
    {
        Step1_OficinasActivas();
        Step2_AccesoCount();
        Step3_UsuarioOficinaInclude();
        Step4_BovedaAbierta();
    }

    static void Step1_OficinasActivas()
    {
        using (var db = Db())
        {
            var n = db.Set<Oficina>().Count(x => x.Estado);
            Console.WriteLine("OK Login Oficina: " + n + " activa(s)");
        }
    }

    static void Step2_AccesoCount()
    {
        using (var db = Db())
        {
            var tk = "127.0.0.1";
            var n = db.Set<Acceso>().Count(x => x.DireccionIp == tk);
            Console.WriteLine("OK Acceso (tk ejemplo): " + n + " fila(s) para " + tk);
        }
    }

    static void Step3_UsuarioOficinaInclude()
    {
        var user = Environment.GetEnvironmentVariable("TEST_USER");
        var oficina = int.Parse(Environment.GetEnvironmentVariable("TEST_OFICINA") ?? "1");
        using (var db = Db())
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                var any = db.Set<UsuarioOficina>()
                    .Include("Usuario")
                    .Include("Oficina")
                    .Where(x => x.Estado && x.Usuario.Estado)
                    .Take(1)
                    .ToList();
                Console.WriteLine("OK UsuarioOficina+nav: " + any.Count + " fila (muestra)");
                return;
            }
            var row = db.Set<UsuarioOficina>()
                .Include("Usuario")
                .Include("Oficina")
                .Where(x => x.Usuario.NombreUsuario == user
                    && x.OficinaId == oficina
                    && x.Estado
                    && x.Usuario.Estado)
                .FirstOrDefault();
            Console.WriteLine(row == null
                ? "OK UsuarioOficina: sin fila para usuario indicado (credenciales invalidas, no error EF)"
                : "OK UsuarioOficina: " + row.Usuario.NombreUsuario + " / " + row.Oficina.Denominacion);
        }
    }

    static void Step4_BovedaAbierta()
    {
        var oficina = int.Parse(Environment.GetEnvironmentVariable("TEST_OFICINA") ?? "1");
        using (var db = Db())
        {
            var b = db.Set<Boveda>()
                .Where(x => x.OficinaId == oficina && !x.IndCierre && !x.IndTemporal)
                .FirstOrDefault();
            Console.WriteLine(b == null
                ? "OK Boveda: ninguna abierta (Autenticar ya no rompe si falta)"
                : "OK Boveda: BovedaId=" + b.BovedaId);
        }
    }
}
'@

$refs = @(
    (Join-Path $WebBin "EntityFramework.dll"),
    (Join-Path $WebBin "EntityFramework.SqlServer.dll"),
    (Join-Path $WebBin "ITB.VENDIX.DA.dll"),
    "System.dll", "System.Core.dll", "System.Data.dll"
)
Add-Type -TypeDefinition $code -ReferencedAssemblies $refs -Language CSharp | Out-Null

try {
    [EfLoginFlowSmoke]::RunAll()
    Write-Host "`nFlujo EF login/autenticar: sin errores de mapeo SQL." -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "FALLO EF login flow:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    $inner = $_.Exception
    $d = 0
    while ($inner.InnerException -and $d -lt 12) {
        $inner = $inner.InnerException
        $d++
        Write-Host ("  Inner[{0}] {1}: {2}" -f $d, $inner.GetType().Name, $inner.Message) -ForegroundColor Yellow
    }
    exit 1
}
