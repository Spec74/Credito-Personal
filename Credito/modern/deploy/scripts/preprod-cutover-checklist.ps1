# Checklist interactivo — Corte preprod/prod (Corte A + B)
# Uso: .\preprod-cutover-checklist.ps1 [-SkipInteractive]

param(
    [switch]$SkipInteractive
)

$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$repoRoot = Split-Path $root -Parent

Write-Host ""
Write-Host "=== Credito — Checklist corte preprod/prod ===" -ForegroundColor Cyan
Write-Host "Repo: $repoRoot"
Write-Host "Docs: docs/migration/PHASE-5-OPERATIONS-CUTOVER.md"
Write-Host ""

$sections = @(
    @{
        Title = "Corte A — API y proxy"
        Items = @(
            "SQL Server accesible desde host API"
            "deploy/.env.production desde .env.production.example (secretos fuera de git)"
            "Jwt:SigningKey >= 32 caracteres; AllowDevToken=false"
            "nginx o IIS ARR segun production.conf.example / iis-arr-web.config.example"
            "Headers X-Forwarded-* y ForwardedHeaders en API"
            "verify-production-config.ps1 -RequireForwardedHeaders OK"
            "smoke-strangler-proxy.ps1 contra URL preprod"
            "test-auth-login.ps1 contra preprod"
        )
    },
    @{
        Title = "Corte B — SPA (/app/)"
        Items = @(
            "npm run build en Credito.Modern.Web (VITE_BASE_URL=/app/)"
            "Static files en location /app/ del proxy"
            "BrowserRouter basename = import.meta.env.BASE_URL"
            "CORS: origen prod en BrowserCors:AllowedOrigins"
            "Login SPA probado contra API preprod"
            "Piloto: 1 oficina o rol (caja, aprobar credito, consulta)"
            "Comunicar brechas MVC: docs/migration/PHASE-5-BRECHAS-NEGOCIO.md"
        )
    },
    @{
        Title = "Corte C — 5C-7 Retiro MVC (ops)"
        Items = @(
            "Ui:UseSpaForModule y DefaultLoginToSpa en appsettings.Production (piloto)"
            "GET /api/v1/hosting/ui-config responde fase cutover"
            "Incluir deploy/nginx/spa-legacy-redirects.conf en nginx preprod"
            "Opcional: location = / return 302 /app/login"
            "Observacion 14 dias (404, tickets) antes de apagar vistas MVC"
            "Mantener ReporteController para layout RDLC opcional (Fase 4 §6B)"
            "test-local-cutover.ps1 OK en entorno de piloto antes de prod"
            "docs/migration/PHASE-5C-7-CUTOVER.md"
        )
    },
    @{
        Title = "Corte C — 5C-7 Retiro MVC (ops)"
        Items = @(
            "Ui:UseSpaForModule y DefaultLoginToSpa en appsettings.Production (piloto)"
            "GET /api/v1/hosting/ui-config responde fase cutover"
            "Incluir deploy/nginx/spa-legacy-redirects.conf en nginx preprod"
            "Opcional: location = / return 302 /app/login"
            "Observacion 14 dias (404, tickets) antes de apagar vistas MVC"
            "Mantener ReporteController para layout RDLC opcional (Fase 4 §6B)"
            "test-local-cutover.ps1 OK en entorno de piloto antes de prod"
            "docs/migration/PHASE-5C-7-CUTOVER.md"
        )
    },
    @{
        Title = "Verificacion local (opcional)"
        Items = @(
            "dotnet test en modern/"
            "npm run build en Credito.Modern.Web"
            "migration-status.ps1"
            "finalize-migration.ps1 -RunDockerChecks (con Docker + MVC)"
            "test-local-cutover.ps1 (cutover local Docker)"
            "verify-preprod-cutover.ps1 -BaseUrl <URL preprod>"
            "test-local-cutover.ps1 (cutover local Docker)"
            "verify-preprod-cutover.ps1 -BaseUrl <URL preprod>"
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
        Push-Location (Join-Path $repoRoot "modern")
        try {
            Write-Host "`n>> dotnet test" -ForegroundColor Green
            dotnet test --no-restore 2>$null
            if ($LASTEXITCODE -ne 0) { dotnet test }
            Write-Host "`n>> migration-status.ps1" -ForegroundColor Green
            & (Join-Path $PSScriptRoot "migration-status.ps1")
        }
        finally {
            Pop-Location
        }
        $web = Join-Path $repoRoot "modern\Credito.Modern.Web"
        if (Test-Path $web) {
            Push-Location $web
            try {
                Write-Host "`n>> npm run build" -ForegroundColor Green
                npm run build
            }
            finally {
                Pop-Location
            }
        }
    }
}

Write-Host ""
Write-Host "Referencia piloto UI: docs/migration/PHASE-5-PILOT-PACK.md" -ForegroundColor Cyan
Write-Host "Listo. Marque items en su runbook institucional antes del go-live." -ForegroundColor Gray
Write-Host ""
