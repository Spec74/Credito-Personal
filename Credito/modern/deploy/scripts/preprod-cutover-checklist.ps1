# Checklist corte preprod/prod (A + B + C)
# Uso:
#   .\preprod-cutover-checklist.ps1 -SkipInteractive
# Runbook: docs/migration/PHASE-5-OPERATIONS-CUTOVER.md
# Spec:    docs/ssd/SSD-00-cutover.md

param(
    [switch]$SkipInteractive
)

$ErrorActionPreference = "Stop"
$modernRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

Write-Host ""
Write-Host "=== Credito - Checklist corte preprod/prod ===" -ForegroundColor Cyan
Write-Host "Repo: $modernRoot"
Write-Host "Runbook: docs/migration/PHASE-5-OPERATIONS-CUTOVER.md"
Write-Host ""

$sections = @(
    @{
        Title = "Corte A - API y proxy"
        Items = @(
            "SQL Server accesible desde el host API"
            "deploy/.env.production desde .env.production.example (secretos fuera de Git)"
            "Jwt:SigningKey de al menos 32 caracteres; AllowDevToken=false"
            "nginx o IIS ARR segun production.conf.example / iis-arr-web.config.example"
            "Headers X-Forwarded-* y ForwardedHeaders en API (KnownProxies = IP del proxy)"
            "verify-production-config.ps1 -RequireForwardedHeaders OK (en el servidor, no contra la plantilla del repo)"
            "smoke-strangler-proxy.ps1 contra URL preprod"
            "test-auth-login.ps1 contra preprod (usuario real, no dev/token)"
            "Scripts deploy/sql pendientes aplicados (ClaveUsuario 256, condonacion, TRF bancos)"
        )
    },
    @{
        Title = "Corte B - SPA /app/"
        Items = @(
            "npm run build en Credito.Modern.Web (VITE_BASE_URL=/app/)"
            "Estaticos en location /app/ del proxy"
            "BrowserRouter basename = import.meta.env.BASE_URL"
            "CORS: origen real en BrowserCors:AllowedOrigins"
            "Login SPA + menu usp_MenuLst"
            "Piloto: 1 oficina (caja, aprobar credito, consulta). Ventas/almacen solo si el menu las tiene"
        )
    },
    @{
        Title = "Corte C - retiro gradual MVC"
        Items = @(
            "Observacion 14 dias (404, tickets) antes de apagar vistas MVC"
            "Incluir deploy/nginx/spa-legacy-redirects.conf cuando el piloto este firmado"
            "Mantener ReporteController mientras negocio exija RDLC identico"
            "test-local-cutover.ps1 OK en Docker local antes de prod"
            "Ui:UseSpaForModule / DefaultLoginToSpa alineados al entorno (PreProduction arranca en false)"
        )
    },
    @{
        Title = "Verificacion local (Development)"
        Items = @(
            "verify-production-config.ps1 (sin -RequireForwardedHeaders)"
            "smoke-local-api.ps1 contra la API ya en marcha (no segundo dotnet run)"
            "dotnet test filtro Strangler (OutputPath aislado si la API esta ocupando la DLL)"
        )
    }
)

foreach ($sec in $sections) {
    Write-Host "--- $($sec.Title) ---" -ForegroundColor Yellow
    $i = 1
    foreach ($item in $sec.Items) {
        Write-Host ("  [{0}] {1}" -f $i, $item)
        $i++
    }
    Write-Host ""
}

if (-not $SkipInteractive) {
    Write-Host "Ejecutar verificaciones locales ahora? (s/N): " -NoNewline
    $r = Read-Host
    if ($r -eq "s" -or $r -eq "S") {
        Write-Host "`n>> verify-production-config.ps1" -ForegroundColor Green
        & (Join-Path $PSScriptRoot "verify-production-config.ps1")
        $smoke = Join-Path $PSScriptRoot "smoke-local-api.ps1"
        Write-Host "`n>> smoke-local-api.ps1 (http://localhost:5288)" -ForegroundColor Green
        & $smoke -BaseUrl "http://localhost:5288"
    }
}

Write-Host ""
Write-Host "Spec: docs/ssd/SSD-00-cutover.md" -ForegroundColor Cyan
Write-Host "Marque los items en el runbook institucional antes del go-live." -ForegroundColor Gray
Write-Host ""
