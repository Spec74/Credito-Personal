# Smoke E2E detras del proxy strangler (sin escrituras destructivas).
# Requiere: docker compose en marcha (puerto 9080) y, para pasos JWT, API con SQL o dev/token.
#
# Ejemplo:
#   .\deploy\scripts\smoke-strangler-proxy.ps1
#   .\deploy\scripts\smoke-strangler-proxy.ps1 -BaseUrl "http://localhost:9080" -UsuarioId 1 -OficinaId 1

param(
    [string]$BaseUrl = "http://localhost:9080",
    [int]$UsuarioId = 1,
    [int]$OficinaId = 1,
    [string]$NombreUsuario = "",
    [string]$Clave = "",
    [int]$ClienteAccesoOficinaId = 0,
    [string]$CodigoArticulo = "SMOKE-NOEXISTE",
    [switch]$SkipJwt
)

$ErrorActionPreference = "Stop"

if ($BaseUrl -match 'tu-dominio|ejemplo\.|placeholder|\.{3}') {
    throw "BaseUrl parece plantilla ($BaseUrl). Usa http://localhost:9080 en local o la URL real de preprod."
}

try {
    $uri = [Uri]$BaseUrl
    if ($uri.Scheme -notin 'http', 'https') {
        throw "BaseUrl debe ser http o https."
    }
}
catch {
    throw "BaseUrl invalida: $BaseUrl"
}

function Get-ApiUrl([string]$path) {
    return ($BaseUrl.TrimEnd("/") + $path)
}

function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

function Write-SmokeOptionalHttp([string]$label, [int]$code) {
    if ($code -eq 404) {
        $msg = '{0} HTTP 404 - ruta registrada; sin fila en BD para este smoke (caja o codigo de prueba).' -f $label
        Write-Host $msg -ForegroundColor Gray
    }
    elseif ($code -eq 503) {
        $msg = '{0} HTTP 503 - revisar SQL en deploy\.env y que CREDITO este accesible.' -f $label
        Write-Host $msg -ForegroundColor Yellow
    }
    else {
        $msg = '{0} HTTP {1} (aceptable en smoke)' -f $label, $code
        Write-Host $msg -ForegroundColor Gray
    }
}

function Assert-Status($response, $allowed) {
    if ($response.StatusCode -notin $allowed) {
        $expected = $allowed -join " o "
        $uri = $response.RequestMessage.RequestUri
        throw "HTTP $($response.StatusCode) - se esperaba $expected. URI: $uri"
    }
}

$correlationId = [guid]::NewGuid().ToString("N")
$proxyHeaders = @{ "X-Correlation-ID" = $correlationId }

$healthUrl = Get-ApiUrl "/health"
Write-Step "Health via proxy ($healthUrl)"
$health = Invoke-WebRequest -Uri $healthUrl -Headers $proxyHeaders -UseBasicParsing
Assert-Status $health @(200)
if ($health.Headers["X-Correlation-ID"] -ne $correlationId) {
    throw "X-Correlation-ID no devuelto por /health (esperado $correlationId)."
}

Write-Step "Swagger via proxy GET /swagger (opcional; solo Development)"
try {
    $swagger = Invoke-WebRequest -Uri (Get-ApiUrl "/swagger/index.html") -Headers $proxyHeaders -UseBasicParsing
    if ($swagger.StatusCode -eq 200) {
        Write-Host "Swagger OK" -ForegroundColor Gray
    }
}
catch {
    $code = 0
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    if ($code -eq 404) {
        Write-Host "Swagger no expuesto (Staging/Production) - omitido" -ForegroundColor Gray
    }
    else {
        throw
    }
}

if ($SkipJwt) {
    Write-Host "SkipJwt: omitiendo login y rutas protegidas." -ForegroundColor Yellow
    Write-Host "`nSmoke basico OK." -ForegroundColor Green
    exit 0
}

$accessToken = $null

if ([string]::IsNullOrWhiteSpace($NombreUsuario)) {
    Write-Step "Dev token (Staging + AllowDevToken) POST /api/v1/dev/token"
    $tokenBody = @{ usuarioId = $UsuarioId; oficinaId = $OficinaId } | ConvertTo-Json
    try {
        $tokenUrl = Get-ApiUrl "/api/v1/dev/token"
        $tokenRes = Invoke-WebRequest -Uri $tokenUrl -Method Post -Body $tokenBody -ContentType "application/json" -UseBasicParsing
        Assert-Status $tokenRes @(200)
        $tokenJson = $tokenRes.Content | ConvertFrom-Json
        $accessToken = $tokenJson.accessToken
    }
    catch {
        Write-Host "dev/token no disponible ($($_.Exception.Message)). Prueba -NombreUsuario y -Clave para login real." -ForegroundColor Yellow
        exit 1
    }
}
else {
    Write-Step "Login real POST /api/v1/auth/login"
    $loginBody = @{
        nombreUsuario = $NombreUsuario
        clave = $Clave
        oficinaId = $OficinaId
    }
    if ($ClienteAccesoOficinaId -gt 0) {
        $loginBody.clienteAcceso = "$ClienteAccesoOficinaId"
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:CREDITO_LOGIN_CLIENTE_ACCESO)) {
        $loginBody.clienteAcceso = $env:CREDITO_LOGIN_CLIENTE_ACCESO
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:CREDITO_LOGIN_CLIENTE_ACCESO)) {
        $loginBody.clienteAcceso = $env:CREDITO_LOGIN_CLIENTE_ACCESO
    }
    $loginUrl = Get-ApiUrl "/api/v1/auth/login"
    $loginRes = Invoke-WebRequest -Uri $loginUrl -Method Post -Body ($loginBody | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing
    Assert-Status $loginRes @(200)
    $loginJson = $loginRes.Content | ConvertFrom-Json
    $accessToken = $loginJson.accessToken
}

$headers = @{ Authorization = "Bearer $accessToken" }

Write-Step "GET /api/v1/auth/me"
$me = Invoke-WebRequest -Uri (Get-ApiUrl "/api/v1/auth/me") -Headers $headers -UseBasicParsing
Assert-Status $me @(200)

Write-Step "GET /api/v1/menu"
$menu = Invoke-WebRequest -Uri (Get-ApiUrl "/api/v1/menu") -Headers $headers -UseBasicParsing
Assert-Status $menu @(200)

Write-Step "GET /api/v1/reportes/catalogo"
$catalogo = Invoke-WebRequest -Uri (Get-ApiUrl "/api/v1/reportes/catalogo") -Headers ($headers + $proxyHeaders) -UseBasicParsing
Assert-Status $catalogo @(200)

Write-Step "GET /api/v1/reportes/politica-exportacion"
$politica = Invoke-WebRequest -Uri (Get-ApiUrl "/api/v1/reportes/politica-exportacion") -Headers ($headers + $proxyHeaders) -UseBasicParsing
Assert-Status $politica @(200)
if ($politica.Content -notmatch "pdf-tabular-completo") {
    throw "politica-exportacion: fase pdf-tabular-completo no presente"
}
if ($politica.Content -notmatch '"informesPdfPilotos"') {
    throw "politica-exportacion: sin informesPdfPilotos"
}
$politicaJson = $politica.Content | ConvertFrom-Json
if ($politicaJson.informesPdfPilotos.Count -ne 40) {
    throw "politica-exportacion: informesPdfPilotos=$($politicaJson.informesPdfPilotos.Count) (esperado 40; rebuild API si imagen antigua)"
}

Write-Step "GET /api/v1/reportes/catalogo-cobertura"
$cobertura = Invoke-WebRequest -Uri (Get-ApiUrl "/api/v1/reportes/catalogo-cobertura") -Headers ($headers + $proxyHeaders) -UseBasicParsing
Assert-Status $cobertura @(200)
$coberturaJson = $cobertura.Content | ConvertFrom-Json
if ($coberturaJson.completoDatosJsonCsvPdf -ne 43) {
    throw "catalogo-cobertura: completoDatosJsonCsvPdf=$($coberturaJson.completoDatosJsonCsvPdf) (esperado 43)"
}

Write-Step "GET /api/v1/database-time"
$time = Invoke-WebRequest -Uri (Get-ApiUrl "/api/v1/database-time") -UseBasicParsing
Assert-Status $time @(200)

Write-Step "GET /api/v1/credito/validar-cierre-caja-diario (solo lectura)"
$validarUrl = Get-ApiUrl "/api/v1/credito/validar-cierre-caja-diario?oficinaId=$OficinaId&cajaDiarioId=1"
try {
    $validar = Invoke-WebRequest -Uri $validarUrl -Headers ($headers + $proxyHeaders) -UseBasicParsing
    Assert-Status $validar @(200, 404, 503)
}
catch {
    $code = [int]$_.Exception.Response.StatusCode
    if ($code -in 404, 503) {
        Write-SmokeOptionalHttp "validar-cierre-caja-diario" $code
    }
    else {
        throw
    }
}

Write-Step "GET /api/v1/ventas/caja-diario-venta-rapida (lectura)"
$cajaVentaUrl = Get-ApiUrl "/api/v1/ventas/caja-diario-venta-rapida?oficinaId=$OficinaId"
try {
    $cajaVenta = Invoke-WebRequest -Uri $cajaVentaUrl -Headers ($headers + $proxyHeaders) -UseBasicParsing
    Assert-Status $cajaVenta @(200, 404, 503)
}
catch {
    $code = [int]$_.Exception.Response.StatusCode
    if ($code -in 404, 503) {
        Write-SmokeOptionalHttp "caja-diario-venta-rapida" $code
    }
    else {
        throw
    }
}

Write-Step "GET /api/v1/ventas/caja-diario-venta-rapida (lectura)"
$cajaVentaUrl = Get-ApiUrl "/api/v1/ventas/caja-diario-venta-rapida?oficinaId=$OficinaId"
try {
    $cajaVenta = Invoke-WebRequest -Uri $cajaVentaUrl -Headers ($headers + $proxyHeaders) -UseBasicParsing
    Assert-Status $cajaVenta @(200, 404, 503)
}
catch {
    $code = [int]$_.Exception.Response.StatusCode
    if ($code -in 404, 503) {
        Write-Host "caja-diario-venta-rapida HTTP $code (aceptable en smoke)" -ForegroundColor Gray
    }
    else {
        throw
    }
}

Write-Step "GET /api/v1/ventas/articulo-venta-rapida (lectura, sin escrituras)"
$articuloUrl = Get-ApiUrl "/api/v1/ventas/articulo-venta-rapida?oficinaId=$OficinaId&codigo=$([uri]::EscapeDataString($CodigoArticulo))"
try {
    $articulo = Invoke-WebRequest -Uri $articuloUrl -Headers ($headers + $proxyHeaders) -UseBasicParsing
    Assert-Status $articulo @(200, 404, 503)
    if ($articulo.Headers["X-Correlation-ID"] -ne $correlationId) {
        Write-Host "AVISO: articulo-venta-rapida no devolvio X-Correlation-ID esperado" -ForegroundColor Yellow
    }
}
catch {
    $code = [int]$_.Exception.Response.StatusCode
    if ($code -in 404, 503) {
        Write-SmokeOptionalHttp "articulo-venta-rapida" $code
    }
    else {
        throw
    }
}

Write-Step "401 sin JWT en rutas protegidas (via proxy)"
$unauthUrls = @(
    $articuloUrl,
    (Get-ApiUrl "/api/v1/reportes/catalogo"),
    (Get-ApiUrl "/api/v1/reportes/politica-exportacion"),
    (Get-ApiUrl "/api/v1/credito/rpt-credito-csv?oficinaId=$OficinaId&fechaIni=2020-01-01&fechaFin=2020-01-31&estadoCredito=ACT"),
    (Get-ApiUrl "/api/v1/credito/rpt-credito-observado-pdf?oficinaId=$OficinaId"),
    (Get-ApiUrl "/api/v1/credito/rpt-credito-condonado-pdf?oficinaId=$OficinaId&fechaIni=2020-01-01&fechaFin=2020-01-31"),
    (Get-ApiUrl "/api/v1/almacen/generar-kardex-csv?oficinaId=$OficinaId&articuloId=1&almacenId=1"),
    (Get-ApiUrl "/api/v1/almacen/generar-kardex-pdf?oficinaId=$OficinaId&articuloId=1&almacenId=1"),
    (Get-ApiUrl "/api/v1/almacen/generar-kardex-csv?oficinaId=$OficinaId&articuloId=1&almacenId=1"),
    (Get-ApiUrl "/api/v1/almacen/generar-kardex-pdf?oficinaId=$OficinaId&articuloId=1&almacenId=1"),
    (Get-ApiUrl "/api/v1/almacen/reporte-stock-csv?oficinaId=$OficinaId"),
    (Get-ApiUrl "/api/v1/almacen/reporte-stock-pdf?oficinaId=$OficinaId")
)
foreach ($url in $unauthUrls) {
    try {
        Invoke-WebRequest -Uri $url -UseBasicParsing | Out-Null
        throw "Se esperaba 401 sin Bearer: $url"
    }
    catch {
        $code = [int]$_.Exception.Response.StatusCode
        if ($code -eq 404) {
            throw "Sin JWT ($url): HTTP 404 - rebuild API: docker compose -f deploy/docker-compose.strangler.yml --env-file deploy/.env up -d --build credito-modern-api"
        }
        if ($code -eq 404) {
            throw "Sin JWT ($url): HTTP 404 - rebuild API: docker compose -f deploy/docker-compose.strangler.yml --env-file deploy/.env up -d --build credito-modern-api"
        }
        if ($code -ne 401) {
            throw "Sin JWT ($url): HTTP $code (esperado 401)"
        }
    }
}

Write-Step "Ruta MVC legado (debe responder el upstream, no 502)"
try {
    $mvc = Invoke-WebRequest -Uri (Get-ApiUrl "/") -UseBasicParsing -MaximumRedirection 0 -ErrorAction SilentlyContinue
    Write-Host "MVC upstream HTTP $($mvc.StatusCode)" -ForegroundColor Gray
}
catch {
    if ($_.Exception.Response.StatusCode -eq 502) {
        Write-Host "AVISO: nginx no alcanza el MVC en host.docker.internal - ajusta deploy/nginx/default.conf" -ForegroundColor Yellow
    }
    else {
        Write-Host "MVC probe: $($_.Exception.Message)" -ForegroundColor Gray
    }
}

Write-Host "`nSmoke strangler proxy completado." -ForegroundColor Green
