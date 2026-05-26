# Comprueba que el proxy strangler esta operativo (archivos + contenedor + nginx + health).
# Uso: desde Credito\modern -> .\deploy\scripts\verify-strangler-proxy.ps1

param(
    [string]$BaseUrl = "http://localhost:9080",
    [switch]$TryStartProxy
)

$ErrorActionPreference = "Stop"

function Invoke-DockerCompose {
    param(
        [Parameter(Mandatory = $true)][string[]]$ComposeArgs,
        [string]$ComposeFile,
        [string]$EnvFile
    )
    # docker compose escribe progreso en stderr; con Stop eso no debe abortar el script.
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $lines = & docker compose -f $ComposeFile --env-file $EnvFile @ComposeArgs 2>&1
        foreach ($line in $lines) {
            if ($null -eq $line) { continue }
            if ($line -is [System.Management.Automation.ErrorRecord]) {
                $t = $line.Exception.Message.Trim()
                if ($t) { Write-Host $t -ForegroundColor Gray }
            }
            else {
                Write-Host ($line.ToString().Trim()) -ForegroundColor Gray
            }
        }
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose $($ComposeArgs -join ' ') salio con codigo $LASTEXITCODE"
        }
    }
    finally {
        $ErrorActionPreference = $prevEap
    }
}

$modernRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$composeFile = Join-Path $modernRoot "deploy\docker-compose.strangler.yml"
$envFile = Join-Path $modernRoot "deploy\.env"
$defaultConf = Join-Path $modernRoot "deploy\nginx\default.conf"
$stranglerInclude = Join-Path $modernRoot "deploy\nginx\proxy_params_strangler.conf"

Write-Host "== Archivos nginx (rutas absolutas)" -ForegroundColor Cyan
foreach ($p in @($defaultConf, $stranglerInclude)) {
    $full = (Resolve-Path $p).Path
    if (-not (Test-Path $full)) {
        throw "Falta $full"
    }
    Write-Host "  OK $full" -ForegroundColor Gray
}

Write-Host "`n== Validar sintaxis compose" -ForegroundColor Cyan
Push-Location $modernRoot
try {
    if (-not (Test-Path $envFile)) {
        Write-Host "AVISO: falta deploy\.env - copia desde deploy\.env.example" -ForegroundColor Yellow
    }
    else {
        & docker compose -f $composeFile --env-file $envFile config --quiet 2>&1 | Out-Host
        Write-Host "OK docker compose config" -ForegroundColor Green

        $dockerEngineOk = $true
        try {
            & docker info 2>&1 | Out-Null
        }
        catch {
            $dockerEngineOk = $false
        }

        if (-not $dockerEngineOk) {
            Write-Host "`nAVISO: Docker Desktop / engine no disponible (pipe dockerDesktopLinuxEngine)." -ForegroundColor Yellow
            Write-Host "  - Archivos nginx y compose config OK; omitiendo ps/nginx hasta que Docker este en marcha." -ForegroundColor Gray
            Write-Host "  - API directa sin proxy: http://localhost:5080/health" -ForegroundColor Gray
        }
        else {
        Write-Host "`n== Estado contenedores" -ForegroundColor Cyan
        Invoke-DockerCompose -ComposeFile $composeFile -EnvFile $envFile -ComposeArgs @(
            'ps', 'strangler-proxy', 'credito-modern-api'
        )

        function Test-StranglerProxyRunning {
            $id = (& docker compose -f $composeFile --env-file $envFile ps -q strangler-proxy 2>$null | Select-Object -First 1)
            if (-not $id) { return $false }
            return (& docker inspect -f '{{.State.Running}}' $id 2>$null) -eq 'true'
        }

        $running = Test-StranglerProxyRunning

        if (-not $running) {
            Write-Host "`nstrangler-proxy NO esta corriendo." -ForegroundColor Yellow
            Write-Host "Ultimas lineas del servicio:" -ForegroundColor Gray
            Invoke-DockerCompose -ComposeFile $composeFile -EnvFile $envFile -ComposeArgs @(
                'logs', 'strangler-proxy', '--tail', '40'
            )

            if ($TryStartProxy) {
                Write-Host "`nIntentando: docker compose up -d strangler-proxy --force-recreate ..." -ForegroundColor Cyan
                Invoke-DockerCompose -ComposeFile $composeFile -EnvFile $envFile -ComposeArgs @(
                    'up', '-d', 'strangler-proxy', '--force-recreate'
                )
                Start-Sleep -Seconds 5
                Invoke-DockerCompose -ComposeFile $composeFile -EnvFile $envFile -ComposeArgs @(
                    'ps', 'strangler-proxy', 'credito-modern-api'
                )
                $running = Test-StranglerProxyRunning
            }
            else {
                Write-Host "`nSiguiente paso (desde modern\):" -ForegroundColor Cyan
                Write-Host '  docker compose -f deploy/docker-compose.strangler.yml --env-file deploy/.env up -d strangler-proxy --force-recreate' -ForegroundColor White
                Write-Host "Si Docker Desktop muestra error al montar volumenes:" -ForegroundColor Cyan
                Write-Host "  - Settings -> Resources -> File sharing: incluir la unidad D:\" -ForegroundColor White
                Write-Host "  - Usar este compose: deploy/docker-compose.strangler.yml desde carpeta modern\" -ForegroundColor White
                Write-Host "  - Opcional: .\deploy\scripts\verify-strangler-proxy.ps1 -TryStartProxy" -ForegroundColor White
            }
        }

        if ($running) {
            Write-Host "`n== nginx -t en el contenedor" -ForegroundColor Cyan
            # nginx escribe por stderr; en PS 5.1 eso llega como ErrorRecord al usar 2>&1.
            $prevEap = $ErrorActionPreference
            $ErrorActionPreference = 'SilentlyContinue'
            try {
                $ngxLines = & docker compose -f $composeFile --env-file $envFile exec -T strangler-proxy nginx -t 2>&1
                foreach ($line in $ngxLines) {
                    if ($null -eq $line) { continue }
                    if ($line -is [System.Management.Automation.ErrorRecord]) {
                        $t = $line.Exception.Message.Trim()
                        if ($t) { Write-Host $t -ForegroundColor Gray }
                    }
                    else {
                        Write-Host ($line.ToString().Trim()) -ForegroundColor Gray
                    }
                }
                if ($LASTEXITCODE -ne 0) {
                    throw "nginx -t salida $($LASTEXITCODE)"
                }
                Write-Host "OK nginx -t" -ForegroundColor Green
            }
            finally {
                $ErrorActionPreference = $prevEap
            }
        }
        }
    }
}
finally {
    Pop-Location
}

Write-Host "`n== GET $BaseUrl/health (via nginx)" -ForegroundColor Cyan
try {
    $r = Invoke-WebRequest -Uri ($BaseUrl.TrimEnd('/') + "/health") -UseBasicParsing -TimeoutSec 10
    if ($r.StatusCode -ne 200) {
        throw "HTTP $($r.StatusCode)"
    }
    Write-Host "OK HTTP 200" -ForegroundColor Green
}
catch {
    Write-Host "FALLO: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Si la API esta bien pero falta nginx: http://localhost:5080/health" -ForegroundColor Yellow
    Write-Host "Si Docker no corre: inicia Docker Desktop y ejecuta compose (ver mensajes arriba)." -ForegroundColor Yellow
    throw "Health via proxy fallo. Repara strangler-proxy y vuelve a ejecutar este script."
}

Write-Host "`n== Rutas API modernas (401 sin JWT, via proxy)" -ForegroundColor Cyan
$unauthPaths = @(
    "/api/v1/reportes/catalogo",
    "/api/v1/reportes/politica-exportacion",
    "/api/v1/credito/rpt-credito-csv?oficinaId=1&fechaIni=2020-01-01&fechaFin=2020-01-31&estadoCredito=ACT",
    "/api/v1/credito/rpt-credito-observado-pdf?oficinaId=1",
    "/api/v1/credito/rpt-credito-condonado-pdf?oficinaId=1&fechaIni=2020-01-01&fechaFin=2020-01-31",
    "/api/v1/almacen/generar-kardex-csv?oficinaId=1&articuloId=1&almacenId=1",
    "/api/v1/almacen/generar-kardex-pdf?oficinaId=1&articuloId=1&almacenId=1",
    "/api/v1/almacen/generar-kardex-csv?oficinaId=1&articuloId=1&almacenId=1",
    "/api/v1/almacen/generar-kardex-pdf?oficinaId=1&articuloId=1&almacenId=1",
    "/api/v1/almacen/reporte-stock-csv?oficinaId=1",
    "/api/v1/almacen/reporte-stock-pdf?oficinaId=1"
)
foreach ($path in $unauthPaths) {
    $url = $BaseUrl.TrimEnd("/") + $path
    try {
        Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10 | Out-Null
        throw "Se esperaba 401 sin JWT: $url"
    }
    catch {
        $code = 0
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        if ($code -eq 404) {
            throw "Sin JWT ($url): HTTP 404 - la imagen API puede estar desactualizada. Ejecuta: docker compose -f deploy/docker-compose.strangler.yml --env-file deploy/.env up -d --build credito-modern-api"
        }
        if ($code -eq 404) {
            throw "Sin JWT ($url): HTTP 404 - la imagen API puede estar desactualizada. Ejecuta: docker compose -f deploy/docker-compose.strangler.yml --env-file deploy/.env up -d --build credito-modern-api"
        }
        if ($code -ne 401) {
            throw "Sin JWT ($url): HTTP $code (esperado 401)"
        }
        Write-Host "  OK 401 $path" -ForegroundColor Gray
    }
}

Write-Host "`n== Rutas API modernas (401 sin JWT, via proxy)" -ForegroundColor Cyan
$unauthPaths = @(
    "/api/v1/reportes/catalogo",
    "/api/v1/reportes/politica-exportacion",
    "/api/v1/credito/rpt-credito-csv?oficinaId=1&fechaIni=2020-01-01&fechaFin=2020-01-31&estadoCredito=ACT",
    "/api/v1/credito/rpt-credito-observado-pdf?oficinaId=1",
    "/api/v1/credito/rpt-credito-condonado-pdf?oficinaId=1&fechaIni=2020-01-01&fechaFin=2020-01-31",
    "/api/v1/almacen/reporte-stock-csv?oficinaId=1",
    "/api/v1/almacen/reporte-stock-pdf?oficinaId=1"
)
foreach ($path in $unauthPaths) {
    $url = $BaseUrl.TrimEnd("/") + $path
    try {
        Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10 | Out-Null
        throw "Se esperaba 401 sin JWT: $url"
    }
    catch {
        $code = 0
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        if ($code -ne 401) {
            throw "Sin JWT ($url): HTTP $code (esperado 401)"
        }
        Write-Host "  OK 401 $path" -ForegroundColor Gray
    }
}

Write-Host "`nVerificacion strangler OK." -ForegroundColor Green
