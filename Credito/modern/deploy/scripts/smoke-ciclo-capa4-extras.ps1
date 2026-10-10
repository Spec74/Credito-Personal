#Requires -Version 5.1
<#
.SYNOPSIS
  E2E capa 4: bóveda, prendario (originación), condonación (solicitar+ejecutar), alta persona.

.DESCRIPTION
  Escrituras controladas en API. No cierra caja ni bóveda.
  Requiere operador ADMIN con bóveda+caja, aprobador, y gestor con caja (prendario).

.EXAMPLE
  .\deploy\scripts\smoke-ciclo-capa4-extras.ps1 `
    -BaseUrl "https://....azurewebsites.net" `
    -AdminUsuario RMANTILLA -AdminClave "***" `
    -AprobadorUsuario BQUISPE -AprobadorClave "***" `
    -GestorUsuario YCERVANTES -GestorClave "***"
#>
param(
    [Parameter(Mandatory = $true)][string]$BaseUrl,
    [string]$AdminUsuario = "RMANTILLA",
    [string]$AdminClave = "",
    [string]$AprobadorUsuario = "BQUISPE",
    [string]$AprobadorClave = "",
    [string]$GestorUsuario = "YCERVANTES",
    [string]$GestorClave = "",
    [int]$OficinaId = 1,
    [int]$CajaIdDestinoBoveda = 1012,
    [decimal]$ImporteBoveda = 50,
    [decimal]$MontoPrendario = 500,
    [int]$PersonaPrendarioId = 0,
    [int]$CreditoCondonarId = 0,
    [switch]$SkipCondonacion,
    [switch]$SkipAltaPersona
)

$ErrorActionPreference = "Stop"
if (-not $AdminClave) { throw "Indique -AdminClave" }
if (-not $AprobadorClave) { $AprobadorClave = $AdminClave }
if (-not $GestorClave) { $GestorClave = $AdminClave }

function Get-ApiUrl([string]$p) { $BaseUrl.TrimEnd("/") + $p }
function Write-Step($m) { Write-Host "`n==> $m" -ForegroundColor Cyan }
function Write-Ok($m) { Write-Host "OK  $m" -ForegroundColor Green }

function Invoke-Login($u, $c) {
    $body = @{ nombreUsuario = $u; clave = $c; oficinaId = $OficinaId } | ConvertTo-Json
    Invoke-RestMethod -Uri (Get-ApiUrl "/api/v1/auth/login") -Method Post -Body $body -ContentType "application/json; charset=utf-8" -TimeoutSec 180
}
function Get-Auth($t) { @{ Authorization = "Bearer $t" } }
function Invoke-ApiGet($path, $h) {
    Invoke-RestMethod -Uri (Get-ApiUrl $path) -Headers $h -TimeoutSec 90
}
function Invoke-ApiPost($path, $h, $obj) {
    try {
        Invoke-RestMethod -Uri (Get-ApiUrl $path) -Method Post -Headers $h -Body ($obj | ConvertTo-Json -Depth 8 -Compress) -ContentType "application/json; charset=utf-8" -TimeoutSec 120
    }
    catch {
        throw "POST $path :: $($_.Exception.Message) :: $($_.ErrorDetails.Message)"
    }
}

$ev = [ordered]@{ startedAt = (Get-Date).ToString("o"); baseUrl = $BaseUrl }

Write-Step "Logins"
$admin = Invoke-Login $AdminUsuario $AdminClave
$hAdmin = Get-Auth $admin.accessToken
$aprob = Invoke-Login $AprobadorUsuario $AprobadorClave
$hAprob = Get-Auth $aprob.accessToken
$gestor = Invoke-Login $GestorUsuario $GestorClave
$hGestor = Get-Auth $gestor.accessToken
Write-Ok "admin/aprobador/gestor"

Write-Step "Bóveda → caja (S/ $ImporteBoveda)"
$est0 = Invoke-ApiGet "/api/v1/credito/boveda-estado-dinero?oficinaId=$OficinaId" $hAdmin
$trf = Invoke-ApiPost "/api/v1/credito/transferir-boveda-caja" $hAdmin @{
    oficinaId          = $OficinaId
    cajaId             = $CajaIdDestinoBoveda
    importe            = $ImporteBoveda
    descripcion        = "E2E capa4 boveda->caja"
    tipoPagoOrigenId   = 1
    tipoPagoDestinoId  = 1
}
$ev.boveda = @{
    saldoAntes            = $est0.saldoBoveda
    movimientoBovedaId    = $trf.movimientoBovedaId
    movimientoCajaId      = $trf.movimientoCajaId
}
Write-Ok "movBoveda=$($trf.movimientoBovedaId) movCaja=$($trf.movimientoCajaId)"

Write-Step "Prendario: solicitud + bienes + PEN + aprobar + desembolso"
if ($PersonaPrendarioId -lt 1) {
    $cli = Invoke-ApiGet "/api/v1/clientes/buscar?term=VILLALBA" $hAdmin
    $PersonaPrendarioId = [int](@($cli)[0].personaId)
}
$sol = Invoke-ApiPost "/api/v1/prendario/crear-solicitud" $hGestor @{
    oficinaId = $OficinaId
    personaId = $PersonaPrendarioId
}
$sid = [int]$sol.solicitudCreditoId
$null = Invoke-ApiPost "/api/v1/credito/guardar-prendas" $hGestor @{
    oficinaId = $OficinaId
    creditoId = $sid
    prendas   = @(
        @{
            descripcion   = "E2E BIEN PRENDARIO"
            marca         = "GEN"
            modelo        = "TEST"
            serie         = "E2E"
            color         = "N/A"
            valorTasacion = [decimal]($MontoPrendario * 1.5)
        }
    )
    fechaRemate = ((Get-Date).AddDays(60).ToString("yyyy-MM-dd") + "T00:00:00")
}
$fp = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
$null = Invoke-ApiPost "/api/v1/credito/crear-credito" $hGestor @{
    oficinaId          = $OficinaId
    solicitudCreditoId = $sid
    productoId         = 2
    tipoCuota          = "F"
    montoInicial       = 0
    montoGastosAdm     = 0
    indGastosAdm       = "ADE"
    montoCredito       = $MontoPrendario
    modalidad          = "M"
    numeroCuotas       = 1
    interesMensual     = 8
    fechaPrimerPago    = "$fp`T00:00:00"
    observacion        = "E2E prendario capa4"
    indCentralRiesgo   = $false
}
$apr = Invoke-ApiPost "/api/v1/credito/aprobar-credito" $hAprob @{
    oficinaId = $OficinaId
    creditoId = $sid
    opcion    = 1
}
if (-not $apr.ok) { throw "Aprobación prendario falló" }
$sesG = Invoke-ApiGet "/api/v1/credito/caja-diario-sesion?oficinaId=$OficinaId" $hGestor
if ([decimal]$sesG.saldoFinal -lt $MontoPrendario) {
    throw "Saldo caja gestor $($sesG.saldoFinal) < $MontoPrendario"
}
$des = Invoke-ApiPost "/api/v1/credito/realizar-desembolso" $hGestor @{
    oficinaId    = $OficinaId
    cajaDiarioId = [int]$sesG.cajaDiarioId
    creditoId    = $sid
}
$ev.prendario = @{
    personaId         = $PersonaPrendarioId
    creditoId         = $sid
    desembolsoMovId   = $des.movimientoCajaId
}
Write-Ok "prendario creditoId=$sid desembolso=$($des.movimientoCajaId)"

if (-not $SkipCondonacion -and $CreditoCondonarId -gt 0) {
    Write-Step "Condonación creditoId=$CreditoCondonarId"
    $sesA = Invoke-ApiGet "/api/v1/credito/caja-diario-sesion?oficinaId=$OficinaId" $hAdmin
    $cuotas = Invoke-ApiGet "/api/v1/credito/cuotas-pendientes?creditoId=$CreditoCondonarId&indCancelacion=false" $hAdmin
    $rest = 0
    foreach ($q in @($cuotas)) { $rest += [decimal]$q.pagoCuota }
    $null = Invoke-ApiPost "/api/v1/credito/solicitar-condonacion" $hAdmin @{
        oficinaId        = $OficinaId
        cajaDiarioId     = [int]$sesA.cajaDiarioId
        creditoId        = $CreditoCondonarId
        moraCondonacion  = 0
    }
    $cond = Invoke-ApiPost "/api/v1/credito/condonar-credito" $hAdmin @{
        oficinaId         = $OficinaId
        creditoId         = $CreditoCondonarId
        montoCxc          = $rest
        montoCondonacion  = $rest
        observacion       = "E2E capa4 condonacion"
    }
    if (-not $cond.success) { throw "Condonar falló: $($cond | ConvertTo-Json -Compress)" }
    $ev.condonacion = @{ creditoId = $CreditoCondonarId; restante = $rest }
    Write-Ok "condonado restante=$rest"
}

if (-not $SkipAltaPersona) {
    Write-Step "Alta persona rápida"
    $dni = "9999" + (Get-Random -Minimum 1000 -Maximum 9999)
    $per = Invoke-ApiPost "/api/v1/clientes/crear-persona-rapida" $hAdmin @{
        dni        = $dni
        nombre     = "E2E"
        apePaterno = "PRUEBA"
        apeMaterno = "CAPA4"
        celular    = "999000111"
    }
    $ev.personaRapida = $per
    Write-Ok "personaId=$($per.personaId) $($per.label)"
}

Write-Step "Validar cierre admin (sin cerrar)"
$sesF = Invoke-ApiGet "/api/v1/credito/caja-diario-sesion?oficinaId=$OficinaId" $hAdmin
$vc = Invoke-ApiGet "/api/v1/credito/validar-cierre-caja-diario?oficinaId=$OficinaId&cajaDiarioId=$($sesF.cajaDiarioId)" $hAdmin
$ev.validarCierre = $vc
Write-Ok "puedeCerrar=$($vc.puedeCerrar)"

$ev.finishedAt = (Get-Date).ToString("o")
$out = Join-Path $env:TEMP ("smoke-capa4-{0}.json" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
$ev | ConvertTo-Json -Depth 8 | Set-Content $out -Encoding UTF8
Write-Host "`nEvidencia: $out" -ForegroundColor Green
Write-Host "PASS capa 4 extras" -ForegroundColor Green
