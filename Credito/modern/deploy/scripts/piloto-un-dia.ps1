# Piloto de un día (oficina 1). Imprime el guion; no escribe en la BD.
# Uso:
#   .\piloto-un-dia.ps1
#   .\piloto-un-dia.ps1 -RunApiSmoke -NombreUsuario RMANTILLA -Clave '***'
# Spec: docs/ssd/SSD-00-cutover.md · docs/E2E-ROLE-MODULE-MATRIX.md

param(
    [string] $SpaUrl = "https://credito-personal.vercel.app",
    [string] $ApiUrl = "https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net",
    [int] $OficinaId = 1,
    [switch] $RunApiSmoke,
    [string] $NombreUsuario = "",
    [string] $Clave = ""
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Credix — Piloto un día (oficina $OficinaId) ===" -ForegroundColor Cyan
Write-Host "SPA: $SpaUrl"
Write-Host "API: $ApiUrl"
Write-Host "MVC no se apaga. Rollback = reenrutar la rebanada."
Write-Host ""

function Show-Section([string] $title, [string[]] $items) {
    Write-Host "--- $title ---" -ForegroundColor Yellow
    $i = 1
    foreach ($item in $items) {
        Write-Host ("  [{0}] {1}" -f $i, $item)
        $i++
    }
    Write-Host ""
}

Show-Section "Antes (Informática, 15 min)" @(
    "2026-10-23-smoke-cierre.sql ya verde (11 índices, PlaInusual, FK) — hecho 23-oct",
    "Pegar 2026-10-23-azure-backup-pitr.sql y confirmar PITR ≥ 7 días en el portal",
    "Jwt signing key ≥ 32; AllowDevToken=false; secretos fuera de Git"
)

Show-Section "Aprobador (BQUISPE o equivalente)" @(
    "Login SPA oficina $OficinaId — menú usp_MenuLst",
    "Bandeja /credito/aprobar: abrir un PEN, ficha completa",
    "Rechazar un PEN de prueba (sin caja/CxC/mora). Toast + sale de la bandeja",
    "No anular DES. usp_Credito_Del no se usa aquí"
)

Show-Section "Cajero / gestor (YCERVANTES o JVILLALOBOS)" @(
    "Login, /inicio (Hoy), barra de sede",
    "Caja diario: sesión de la oficina del JWT",
    "Consulta o cobro real del día (no inventar movimiento de prueba en prod)",
    "Un PDF Credix (cobro diario o ticket). RDLC solo si negocio lo pide"
)

Show-Section "Admin / oficial (RMANTILLA)" @(
    "/informes/pla-uif abre (ya no 503). Si TcSbs=0, cargar promedio venta SBS del mes anterior",
    "No subir el piso legal US`$ 10 000 / 50 000",
    "Avisos + agenda: leer un comunicado, ver el día",
    "375 px (DevTools): login, hoy, aprobar, caja diario, consulta, avisos"
)

Show-Section "Firma Corte A (mismo día)" @(
    "API /api/v1/* no cae al MVC; una URL no migrada sigue en legado",
    "Negocio: un día sin ticket bloqueante en crédito/caja",
    "MVC IIS sigue vivo. Corte B/C no se firman hoy"
)

Write-Host "Scripts de apoyo (no son el piloto de oficina):" -ForegroundColor Gray
Write-Host "  smoke-ops-checklist.ps1 -BaseUrl $ApiUrl -NombreUsuario ... -OficinaId $OficinaId"
Write-Host "  smoke-ciclo-credito-capa3.ps1  (solo si Informática pide ciclo S/ 200)"
Write-Host "  docs/responsive-audit.md       (casillas 375 px)"
Write-Host ""

if ($RunApiSmoke) {
    if ([string]::IsNullOrWhiteSpace($NombreUsuario)) {
        throw "RunApiSmoke requiere -NombreUsuario y -Clave (usuario real, no dev/token)."
    }
    $smoke = Join-Path $PSScriptRoot "smoke-ops-checklist.ps1"
    Write-Host ">> smoke-ops-checklist.ps1 contra API" -ForegroundColor Green
    & $smoke -BaseUrl $ApiUrl -NombreUsuario $NombreUsuario -Clave $Clave -OficinaId $OficinaId
}
