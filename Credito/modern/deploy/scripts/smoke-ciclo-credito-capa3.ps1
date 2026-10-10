#Requires -Version 5.1
<#
.SYNOPSIS
  E2E capa 3: ciclo de negocio crédito personal en API (prod o local).

.DESCRIPTION
  Flujo controlado:
    simular → solicitud → crear (PEN) → aprobar (APROBADOR) → desembolso → cobro 1ª cuota + ticket
    → validar-cierre (NO cierra la caja en vivo).

  Por defecto usa montos pequeños (S/ 200, 2 cuotas). Limpia PEN huérfanos del mismo run vía rechazo.

.EXAMPLE
  cd Credito/modern
  .\deploy\scripts\smoke-ciclo-credito-capa3.ps1 `
    -BaseUrl "https://crediconfiable-api-....azurewebsites.net" `
    -OperadorUsuario RMANTILLA -OperadorClave "***" `
    -AprobadorUsuario BQUISPE -AprobadorClave "***" `
    -PersonaId 48 -OficinaId 1
#>
param(
    [Parameter(Mandatory = $true)][string]$BaseUrl,
    [string]$OperadorUsuario = "RMANTILLA",
    [string]$OperadorClave = "",
    [string]$AprobadorUsuario = "BQUISPE",
    [string]$AprobadorClave = "",
    [int]$OficinaId = 1,
    [int]$PersonaId = 48,
    [int]$ProductoId = 1,
    [decimal]$MontoCredito = 200,
    [int]$NumeroCuotas = 2,
    [decimal]$InteresMensual = 6,
    [switch]$SkipCobro,
    [switch]$AllowCerrarCaja
)

$ErrorActionPreference = "Stop"
if (-not $OperadorClave) { throw "Indique -OperadorClave (no se versiona en el repo)." }
if (-not $AprobadorClave) { $AprobadorClave = $OperadorClave }

function Get-ApiUrl([string]$path) { return ($BaseUrl.TrimEnd("/") + $path) }
function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg) { Write-Host "OK  $msg" -ForegroundColor Green }

function Invoke-Login([string]$user, [string]$clave) {
    $body = @{ nombreUsuario = $user; clave = $clave; oficinaId = $OficinaId } | ConvertTo-Json
    return Invoke-RestMethod -Uri (Get-ApiUrl "/api/v1/auth/login") -Method Post -Body $body -ContentType "application/json; charset=utf-8" -TimeoutSec 90
}

function AuthHeaders($token) { @{ Authorization = "Bearer $token" } }

function Post-Json($path, $headers, $obj) {
    $json = $obj | ConvertTo-Json -Depth 8 -Compress
    try {
        return Invoke-RestMethod -Uri (Get-ApiUrl $path) -Method Post -Headers $headers -Body $json -ContentType "application/json; charset=utf-8" -TimeoutSec 120
    }
    catch {
        $detail = $_.ErrorDetails.Message
        throw "POST $path failed: $($_.Exception.Message) :: $detail"
    }
}

$evidence = [ordered]@{
    startedAt = (Get-Date).ToString("o")
    baseUrl   = $BaseUrl
    oficinaId = $OficinaId
    personaId = $PersonaId
}

Write-Step "Login operador $OperadorUsuario"
$op = Invoke-Login $OperadorUsuario $OperadorClave
$hOp = AuthHeaders $op.accessToken
$evidence.operadorUsuarioId = $op.usuarioId

Write-Step "Sesión caja abierta"
$ses = Invoke-RestMethod -Uri (Get-ApiUrl "/api/v1/credito/caja-diario-sesion?oficinaId=$OficinaId") -Headers $hOp -TimeoutSec 60
if (-not $ses -or $ses.indCierre) { throw "El operador no tiene caja abierta." }
if ([decimal]$ses.saldoFinal -lt $MontoCredito) {
    throw "Saldo caja $($ses.saldoFinal) < monto $MontoCredito. Use otra caja o reduzca -MontoCredito."
}
$cajaDiarioId = [int]$ses.cajaDiarioId
Write-Ok "cajaDiarioId=$cajaDiarioId $($ses.cajaDenominacion) saldo=$($ses.saldoFinal)"
$evidence.cajaDiarioId = $cajaDiarioId
$evidence.cajaDenominacion = $ses.cajaDenominacion

$fechaPrimer = (Get-Date).AddDays(7).ToString("yyyy-MM-dd")

Write-Step "Simulador"
$sim = Post-Json "/api/v1/credito/simulador-credito" $hOp @{
    monto            = $MontoCredito
    formaPago        = "M"
    nroCuotas        = $NumeroCuotas
    interesMensual   = $InteresMensual
    fechaPrimerPago  = "$fechaPrimer`T00:00:00"
    gastosAdm        = 0
}
Write-Ok "plan simulado $(@($sim).Count) cuota(s)"

Write-Step "Crear solicitud + crédito (PEN)"
$sol = Post-Json "/api/v1/credito/crear-solicitud-credito" $hOp @{
    oficinaId = $OficinaId
    personaId = $PersonaId
}
$solicitudId = [int]$sol.solicitudCreditoId
$null = Post-Json "/api/v1/credito/crear-credito" $hOp @{
    oficinaId          = $OficinaId
    solicitudCreditoId = $solicitudId
    productoId         = $ProductoId
    tipoCuota          = "F"
    montoInicial       = 0
    montoGastosAdm     = 0
    indGastosAdm       = "ADE"
    montoCredito       = $MontoCredito
    modalidad          = "M"
    numeroCuotas       = $NumeroCuotas
    interesMensual     = $InteresMensual
    fechaPrimerPago    = "$fechaPrimer`T00:00:00"
    observacion        = "E2E capa3 smoke $($evidence.startedAt)"
    indCentralRiesgo   = $false
}
# En este dominio el id de crédito coincide con la solicitud.
$creditoId = $solicitudId
$evidence.creditoId = $creditoId
Write-Ok "crédito PEN creditoId=$creditoId"

Write-Step "Aprobar ($AprobadorUsuario)"
$ap = Invoke-Login $AprobadorUsuario $AprobadorClave
$hAp = AuthHeaders $ap.accessToken
$apr = Post-Json "/api/v1/credito/aprobar-credito" $hAp @{
    oficinaId = $OficinaId
    creditoId = $creditoId
    opcion    = 1
}
if (-not $apr.ok) { throw "Aprobación no OK: $($apr | ConvertTo-Json -Compress)" }
Write-Ok "aprobado creditoId=$creditoId"
$evidence.aprobado = $true

Write-Step "Desembolso"
$valDes = Invoke-RestMethod -Uri (Get-ApiUrl "/api/v1/credito/validar-desembolso?oficinaId=$OficinaId&cajaDiarioId=$cajaDiarioId&creditoId=$creditoId") -Headers $hOp -TimeoutSec 60
if (-not $valDes.puedeDesembolsar) { throw "No puede desembolsar: $($valDes.mensaje)" }
$des = Post-Json "/api/v1/credito/realizar-desembolso" $hOp @{
    oficinaId     = $OficinaId
    cajaDiarioId  = $cajaDiarioId
    creditoId     = $creditoId
}
$evidence.desembolsoMovimientoCajaId = $des.movimientoCajaId
Write-Ok "desembolso movimientoCajaId=$($des.movimientoCajaId)"

if (-not $SkipCobro) {
    Write-Step "Cobro 1ª cuota + ticket"
    $ses2 = Invoke-RestMethod -Uri (Get-ApiUrl "/api/v1/credito/caja-diario-sesion?oficinaId=$OficinaId") -Headers $hOp -TimeoutSec 60
    $cuotas = Invoke-RestMethod -Uri (Get-ApiUrl "/api/v1/credito/cuotas-pendientes?creditoId=$creditoId&indCancelacion=false") -Headers $hOp -TimeoutSec 60
    $first = @($cuotas) | Select-Object -First 1
    if (-not $first) { throw "Sin cuotas pendientes tras desembolso." }
    $planId = [int]$first.planPagoId
    $importe = [decimal]$first.pagoCuota
    if ($importe -le 0) { $importe = [decimal]$first.cuota }
    $pago = Post-Json "/api/v1/credito/pagar-cuotas" $hOp @{
        oficinaId        = $OficinaId
        cajaDiarioId     = [int]$ses2.cajaDiarioId
        creditoId        = $creditoId
        listaPlanPagoId  = "$planId"
        importeRecibido  = $importe
        tipoPagoId       = 1
    }
    $movPago = $pago.resultId
    $evidence.pagoMovimientoCajaId = $movPago
    $evidence.planPagoId = $planId
    $evidence.importeCobrado = $importe
    Write-Ok "pago resultId=$movPago importe=$importe"

    $ticketPath = Join-Path $env:TEMP "smoke-capa3-ticket-$movPago.pdf"
    Invoke-WebRequest -Uri (Get-ApiUrl "/api/v1/credito/movimiento-caja-ticket-pdf?oficinaId=$OficinaId&movimientoCajaId=$movPago") -Headers $hOp -OutFile $ticketPath -TimeoutSec 90
    $len = (Get-Item $ticketPath).Length
    if ($len -lt 1000) { throw "Ticket PDF demasiado pequeño ($len bytes)." }
    $evidence.ticketBytes = $len
    $evidence.ticketPath = $ticketPath
    Write-Ok "ticket PDF $len bytes → $ticketPath"
}

Write-Step "Validar cierre (sin cerrar por defecto)"
$ses3 = Invoke-RestMethod -Uri (Get-ApiUrl "/api/v1/credito/caja-diario-sesion?oficinaId=$OficinaId") -Headers $hOp -TimeoutSec 60
$vc = Invoke-RestMethod -Uri (Get-ApiUrl "/api/v1/credito/validar-cierre-caja-diario?oficinaId=$OficinaId&cajaDiarioId=$($ses3.cajaDiarioId)") -Headers $hOp -TimeoutSec 60
$evidence.validarCierre = $vc
Write-Ok "puedeCerrar=$($vc.puedeCerrar) blockers=$($vc.blockers -join '; ')"

if ($AllowCerrarCaja) {
    if (-not $vc.puedeCerrar) { throw "No se puede cerrar: $($vc.blockers -join '; ')" }
    Write-Step "Cerrar caja (explícito -AllowCerrarCaja)"
    $null = Post-Json "/api/v1/credito/cerrar-caja-diario" $hOp @{
        oficinaId    = $OficinaId
        cajaDiarioId = [int]$ses3.cajaDiarioId
    }
    Write-Ok "caja cerrada"
    $evidence.cajaCerrada = $true
}
else {
    $evidence.cajaCerrada = $false
    Write-Host "INFO cierre NO ejecutado (omita -AllowCerrarCaja solo en pruebas controladas)." -ForegroundColor Yellow
}

$evidence.finishedAt = (Get-Date).ToString("o")
$out = Join-Path $env:TEMP ("smoke-capa3-evidence-{0}.json" -f $creditoId)
$evidence | ConvertTo-Json -Depth 8 | Set-Content -Path $out -Encoding UTF8
Write-Host "`nEvidencia: $out" -ForegroundColor Green
Write-Host "PASS ciclo crédito capa 3 creditoId=$creditoId" -ForegroundColor Green
