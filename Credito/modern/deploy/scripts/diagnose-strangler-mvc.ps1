# Diagnostico: 502 en :9080/, MVC colgado, EntityFramework.
# Uso: .\deploy\scripts\diagnose-strangler-mvc.ps1

$ErrorActionPreference = "Continue"

function Test-Tcp([string]$HostName, [int]$Port) {
    try {
        $c = New-Object System.Net.Sockets.TcpClient
        $c.Connect($HostName, $Port)
        $c.Close()
        return $true
    }
    catch { return $false }
}

function Test-Http([string]$Url) {
    try {
        $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        return "HTTP $($r.StatusCode)"
    }
    catch {
        $code = 0
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        if ($code -gt 0) { return "HTTP $code" }
        return $_.Exception.Message
    }
}

$scriptsDir = $PSScriptRoot
$devPort = 0
$iisPort = 0
try {
    $devPort = & (Join-Path $scriptsDir "resolve-mvc-dev-port.ps1") -Profile DevServer -AsInt
    $iisPort = & (Join-Path $scriptsDir "resolve-mvc-dev-port.ps1") -Profile IISExpress -AsInt
}
catch {
    Write-Host "AVISO: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "== Puertos MVC (Web.csproj)" -ForegroundColor Cyan
Write-Host "  Servidor desarrollo VS: $devPort" -ForegroundColor Gray
Write-Host "  IIS Express:            $iisPort" -ForegroundColor Gray

Write-Host "`n== Escucha en 127.0.0.1" -ForegroundColor Cyan
foreach ($p in @($devPort, $iisPort) | Where-Object { $_ -gt 0 } | Select-Object -Unique) {
    $ok = Test-Tcp "127.0.0.1" $p
    $color = if ($ok) { "Green" } else { "Yellow" }
    Write-Host "  Puerto $p : $(if ($ok) { 'ABIERTO' } else { 'cerrado' })" -ForegroundColor $color
}

Write-Host "`n== HTTP directo al MVC (sin proxy)" -ForegroundColor Cyan
foreach ($p in @($devPort, $iisPort) | Where-Object { $_ -gt 0 } | Select-Object -Unique) {
    $result = Test-Http "http://localhost:$p/"
    Write-Host "  http://localhost:$p/ -> $result" -ForegroundColor Gray
    if ((Test-Tcp "127.0.0.1" $p) -and $result -match "tiempo de espera|timeout|Timeout") {
        Write-Host "    AVISO: puerto abierto pero HTTP no responde (MVC colgado o EF en primer request)." -ForegroundColor Yellow
    }
}
if (Get-Process iisexpress -ErrorAction SilentlyContinue) {
    Write-Host "`n  Proceso iisexpress.exe en ejecucion -> usar puerto IIS Express ($iisPort) en nginx." -ForegroundColor Yellow
}

Write-Host "`n== SQL Server (Web.config: localhost,14330)" -ForegroundColor Cyan
$sqlOk = Test-Tcp "127.0.0.1" 14330
if ($sqlOk) {
    Write-Host "  Puerto 14330: ABIERTO" -ForegroundColor Green
}
else {
    Write-Host "  Puerto 14330: CERRADO - inicia SQL Server (Docker o instancia local)." -ForegroundColor Red
    Write-Host "  Sin SQL, MVC suele fallar con EntityCommandCompilationException al arrancar." -ForegroundColor Yellow
}
if ($sqlOk) {
    Write-Host "  Cadena SQL: .\deploy\scripts\test-mvc-sql-connection.ps1" -ForegroundColor Gray
}
if ($sqlOk) {
    Write-Host "  Cadena SQL: .\deploy\scripts\test-mvc-sql-connection.ps1" -ForegroundColor Gray
}

Write-Host "`n== Proxy strangler :9080" -ForegroundColor Cyan
Write-Host "  /health -> $(Test-Http 'http://localhost:9080/health')" -ForegroundColor Gray
Write-Host "  /       -> $(Test-Http 'http://localhost:9080/')" -ForegroundColor Gray

Write-Host "`n== Entity Framework (Login + Autenticar)" -ForegroundColor Cyan
foreach ($efScript in @("test-mvc-ef-oficina.ps1", "test-mvc-ef-login-flow.ps1", "audit-edmx-msl.ps1")) {
    $path = Join-Path $scriptsDir $efScript
    if (Test-Path $path) {
        Write-Host "  --- $efScript ---" -ForegroundColor Gray
        & $path
    }
}

Write-Host "`n== Entity Framework (Login + Autenticar)" -ForegroundColor Cyan
foreach ($efScript in @("test-mvc-ef-oficina.ps1", "test-mvc-ef-login-flow.ps1", "audit-edmx-msl.ps1")) {
    $path = Join-Path $scriptsDir $efScript
    if (Test-Path $path) {
        Write-Host "  --- $efScript ---" -ForegroundColor Gray
        & $path
    }
}

Write-Host "`n== Acciones sugeridas" -ForegroundColor Cyan
if (-not (Test-Tcp "127.0.0.1" 14330)) {
    Write-Host "  1. Levantar SQL CREDITO en localhost,14330 (misma cadena que Web\Web.config)." -ForegroundColor White
}
$active = @($devPort, $iisPort) | Where-Object { $_ -gt 0 -and (Test-Tcp "127.0.0.1" $_) }
if ($active.Count -eq 0) {
    Write-Host "  2. F5 en Visual Studio (proyecto Web). Revisa la barra: IIS Express usa puerto $iisPort." -ForegroundColor White
}
if (Get-Process iisexpress -ErrorAction SilentlyContinue) {
    $use = $iisPort
    Write-Host "  3. IIS Express detectado -> .\deploy\scripts\set-mvc-upstream-port.ps1 -Port $use" -ForegroundColor White
}
elseif ($active.Count -gt 0) {
    $use = $active[0]
    Write-Host "  3. Alinear nginx: .\deploy\scripts\set-mvc-upstream-port.ps1 -Port $use" -ForegroundColor White
    Write-Host "  4. Probar: http://localhost:9080/ (debe ser MVC, no 502)." -ForegroundColor White
}
Write-Host "  Perfil IIS Express: .\deploy\scripts\run-local-strangler-e2e.ps1 -Profile IISExpress" -ForegroundColor White
