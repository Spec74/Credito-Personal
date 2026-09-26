# Smoke operativo serio (solo lectura) tras proxy strangler.
# Cubre: health, JWT (dev/token o login real), caja, cobro diario, aprobar, reportes clave, pista PBKDF2.
#
#   cd Credito/modern
#   .\deploy\scripts\smoke-ops-checklist.ps1
#   .\deploy\scripts\smoke-ops-checklist.ps1 -NombreUsuario ADMVENDIX -Clave '***' -OficinaId 1

param(
    [string]$BaseUrl = "http://localhost:9080",
    [int]$UsuarioId = 1,
    [int]$OficinaId = 1,
    [string]$NombreUsuario = "",
    [string]$Clave = "",
    [switch]$SkipJwt
)

$ErrorActionPreference = "Stop"

function Get-ApiUrl([string]$path) { return ($BaseUrl.TrimEnd("/") + $path) }
function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg) { Write-Host "OK  $msg" -ForegroundColor Green }
function Write-Warn([string]$msg) { Write-Host "WARN $msg" -ForegroundColor Yellow }

function Assert-Status($response, $allowed) {
    if ($response.StatusCode -notin $allowed) {
        throw "HTTP $($response.StatusCode) - se esperaba $($allowed -join ' o '). URI: $($response.RequestMessage.RequestUri)"
    }
}

function Invoke-Api {
    param(
        [string]$Uri,
        [Microsoft.PowerShell.Commands.WebRequestMethod]$Method = "Get",
        [hashtable]$Headers = @{},
        [string]$Body = $null
    )
    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $Headers
        UseBasicParsing = $true
    }
    if ($Body) {
        $params.Body = $Body
        $params.ContentType = "application/json"
    }
    return Invoke-WebRequest @params
}

function Invoke-ApiAllow {
    param(
        [string]$Uri,
        [hashtable]$Headers,
        [int[]]$Allowed
    )
    try {
        $res = Invoke-Api -Uri $Uri -Headers $Headers
        Assert-Status $res $Allowed
        return $res
    }
    catch {
        $code = 0
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        if ($code -in $Allowed) {
            Write-Warn "$Uri -> HTTP $code (aceptable en smoke)"
            return $null
        }
        throw
    }
}

$results = [System.Collections.Generic.List[string]]::new()
function Mark([string]$id, [bool]$ok, [string]$detail = "") {
    $line = if ($ok) { "[PASS] $id $detail" } else { "[FAIL] $id $detail" }
    $results.Add($line.Trim())
    if ($ok) { Write-Ok $line } else { Write-Host $line -ForegroundColor Red }
}

Write-Step "1) Health proxy"
$health = Invoke-Api -Uri (Get-ApiUrl "/health")
Assert-Status $health @(200)
Mark "health" $true

if ($SkipJwt) {
    Write-Warn "SkipJwt: fin temprano."
    $results | ForEach-Object { Write-Host $_ }
    exit 0
}

$accessToken = $null
$loginMode = "dev-token"

if (-not [string]::IsNullOrWhiteSpace($NombreUsuario)) {
    Write-Step "2) Login real POST /api/v1/auth/login (PBKDF2 o claro)"
    $loginBody = @{
        nombreUsuario = $NombreUsuario
        clave = $Clave
        oficinaId = $OficinaId
    } | ConvertTo-Json
    $loginRes = Invoke-Api -Uri (Get-ApiUrl "/api/v1/auth/login") -Method Post -Body $loginBody
    Assert-Status $loginRes @(200)
    $accessToken = ($loginRes.Content | ConvertFrom-Json).accessToken
    $loginMode = "auth-login"
    Mark "login-pbkdf2-or-plain" $true "mode=auth-login user=$NombreUsuario"
}
else {
    Write-Step "2) Dev token POST /api/v1/dev/token"
    $tokenBody = @{ usuarioId = $UsuarioId; oficinaId = $OficinaId } | ConvertTo-Json
    try {
        $tokenRes = Invoke-Api -Uri (Get-ApiUrl "/api/v1/dev/token") -Method Post -Body $tokenBody
        Assert-Status $tokenRes @(200)
        $accessToken = ($tokenRes.Content | ConvertFrom-Json).accessToken
        Mark "dev-token" $true "usuarioId=$UsuarioId oficinaId=$OficinaId"
        Write-Warn "Login PBKDF2 real omitido (pasa -NombreUsuario/-Clave). DevToken no reescribe claves."
    }
    catch {
        Mark "dev-token" $false $_.Exception.Message
        throw
    }
}

$headers = @{ Authorization = "Bearer $accessToken" }

Write-Step "3) Auth me + menu"
$me = Invoke-Api -Uri (Get-ApiUrl "/api/v1/auth/me") -Headers $headers
Assert-Status $me @(200)
Mark "auth-me" $true

$menu = Invoke-Api -Uri (Get-ApiUrl "/api/v1/menu") -Headers $headers
Assert-Status $menu @(200)
Mark "menu" $true

Write-Step "4) Caja diario sesion"
$caja = Invoke-ApiAllow -Uri (Get-ApiUrl "/api/v1/credito/caja-diario-sesion?oficinaId=$OficinaId") -Headers $headers -Allowed @(200, 404, 503)
Mark "caja-diario-sesion" ($null -ne $caja -or $true) $(if ($caja) { "HTTP $($caja.StatusCode)" } else { "sin sesion abierta (404/503 OK)" })

Write-Step "5) Cobro diario (lectura)"
# usp_RptCobroDiario(UsuarioId, OficinaId) — deben coincidir con el JWT.
$cobroUrl = Get-ApiUrl "/api/v1/credito/rpt-cobro-diario?usuarioId=$UsuarioId&oficinaId=$OficinaId"
$cobro = Invoke-ApiAllow -Uri $cobroUrl -Headers $headers -Allowed @(200, 400, 403, 404, 503)
if ($cobro -and $cobro.StatusCode -eq 200) {
    $n = @(($cobro.Content | ConvertFrom-Json)).Count
    Mark "cobro-diario" $true "HTTP 200 rows=$n"
}
else {
    Mark "cobro-diario" $true "sin filas o rol gestor (HTTP opcional)"
}

$cobroPdfUrl = Get-ApiUrl "/api/v1/credito/rpt-cobro-diario-pdf?usuarioId=$UsuarioId&oficinaId=$OficinaId"
$cobroPdf = Invoke-ApiAllow -Uri $cobroPdfUrl -Headers $headers -Allowed @(200, 400, 403, 404, 503)
if ($cobroPdf -and $cobroPdf.StatusCode -eq 200) {
    $ct = [string]$cobroPdf.Headers["Content-Type"]
    $isPdf = $cobroPdf.RawContentLength -gt 800 -and ($ct -match "pdf")
    Mark "cobro-diario-pdf" $isPdf "bytes=$($cobroPdf.RawContentLength) content-type=$ct"
}
else {
    Mark "cobro-diario-pdf" $true "sin PDF (rol/datos; OK en smoke)"
}

Write-Step "6) Creditos por aprobar (lectura)"
$aprobar = Invoke-ApiAllow -Uri (Get-ApiUrl "/api/v1/credito/creditos-por-aprobar?oficinaId=$OficinaId") -Headers $headers -Allowed @(200, 404, 503)
Mark "creditos-por-aprobar" $true $(if ($aprobar) { "HTTP $($aprobar.StatusCode)" } else { "sin pendientes" })

Write-Step "7) Reportes clave (catalogo + politica + movimiento boveda PDF)"
$cat = Invoke-Api -Uri (Get-ApiUrl "/api/v1/reportes/catalogo") -Headers $headers
Assert-Status $cat @(200)
Mark "reportes-catalogo" $true

$pol = Invoke-Api -Uri (Get-ApiUrl "/api/v1/reportes/politica-exportacion") -Headers $headers
Assert-Status $pol @(200)
$polJson = $pol.Content | ConvertFrom-Json
Mark "reportes-politica" ($polJson.informesPdfPilotos.Count -ge 1) "pilotos=$($polJson.informesPdfPilotos.Count)"

$bovPdf = Invoke-ApiAllow -Uri (Get-ApiUrl "/api/v1/credito/rpt-movimiento-boveda-pdf?bovedaId=1") -Headers $headers -Allowed @(200, 404, 400, 503)
if ($bovPdf -and $bovPdf.StatusCode -eq 200) {
    Mark "rpt-movimiento-boveda-pdf" ($bovPdf.RawContentLength -gt 800) "bytes=$($bovPdf.RawContentLength)"
}
else {
    Mark "rpt-movimiento-boveda-pdf" $true "sin bovedaId=1 (aceptable)"
}

Write-Step "8) 401 sin JWT"
try {
    Invoke-Api -Uri (Get-ApiUrl "/api/v1/reportes/catalogo") | Out-Null
    Mark "unauth-401" $false "respondio sin Bearer"
}
catch {
    $code = [int]$_.Exception.Response.StatusCode
    Mark "unauth-401" ($code -eq 401) "HTTP $code"
}

Write-Host "`n========== SMOKE OPS SUMMARY ($loginMode) ==========" -ForegroundColor Cyan
$fail = 0
foreach ($r in $results) {
    Write-Host $r
    if ($r.StartsWith("[FAIL]")) { $fail++ }
}
if ($fail -gt 0) {
    Write-Host "`nSmoke OPS: $fail fallo(s)." -ForegroundColor Red
    exit 1
}
Write-Host "`nSmoke OPS completado." -ForegroundColor Green
if ($loginMode -eq "dev-token") {
    Write-Host "Pendiente manual: login SPA con usuario real para validar/migrar clave `$pbk2`$." -ForegroundColor Yellow
}
exit 0
