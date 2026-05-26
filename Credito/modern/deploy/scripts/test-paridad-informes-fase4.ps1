# Verificación de paridad Fase 4 (lectura API + muestras SQL en CREDITO).
# Requiere: proxy 9080, dev/token, SQL localhost,14330 (misma BD que MVC).
#
# Uso:
#   .\deploy\scripts\test-paridad-informes-fase4.ps1
#   .\deploy\scripts\test-paridad-informes-fase4.ps1 -BaseUrl http://localhost:9080 -OficinaId 1

param(
    [string]$BaseUrl = "http://localhost:9080",
    [int]$UsuarioId = 1,
    [int]$OficinaId = 1,
    [string]$SqlServer = "localhost,14330",
    [string]$SqlDatabase = "CREDITO",
    [string]$SqlUserId = "sa",
    [string]$SqlPassword = "123456",
    [int]$MovimientoCajaCuotaId = 0,
    [int]$MovimientoCajaLibreId = 0,
    [int]$PersonaId = 0,
    [int]$CreditoId = 0,
    [int]$MovimientoAlmacenId = 0,
    [int]$ProductoId = 0,
    [switch]$SkipSqlDiscovery,
    [switch]$AllowSkipAll
)

$ErrorActionPreference = "Stop"
$scriptsDir = $PSScriptRoot
$modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")
$repoRoot = Resolve-Path (Join-Path $modernRoot "..")

$passed = 0
$skipped = 0
$failed = 0

function Write-ParidadOk([string]$msg) {
    Write-Host "OK  $msg" -ForegroundColor Green
    $script:passed++
}

function Write-ParidadSkip([string]$msg) {
    Write-Host "SKIP $msg" -ForegroundColor Yellow
    $script:skipped++
}

function Write-ParidadFail([string]$msg) {
    Write-Host "FAIL $msg" -ForegroundColor Red
    $script:failed++
}

function Get-ApiUrl([string]$path) {
    return ($BaseUrl.TrimEnd("/") + $path)
}

function Read-EnvConnectionString {
    $envFile = Join-Path $modernRoot "deploy\.env"
    if (-not (Test-Path $envFile)) { return $null }
    foreach ($line in Get-Content $envFile) {
        if ($line -match '^\s*CREDITO_DB_CONNECTION_STRING=(.+)$') {
            return $Matches[1].Trim()
        }
    }
    return $null
}

function Parse-SqlPasswordFromConnectionString([string]$cs) {
    if ($cs -match 'Password=([^;]+)') { return $Matches[1] }
    return $null
}

function Invoke-SqlScalar([string]$query) {
    Add-Type -AssemblyName "System.Data" -ErrorAction SilentlyContinue
    $cs = "Server=$SqlServer;Database=$SqlDatabase;User Id=$SqlUserId;Password=$SqlPassword;TrustServerCertificate=True;Encrypt=True;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($cs)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $query
        $cmd.CommandTimeout = 30
        $result = $cmd.ExecuteScalar()
        if ($null -eq $result -or $result -is [DBNull]) { return $null }
        return [int]$result
    }
    finally {
        $conn.Close()
    }
}

function Get-DevToken {
    $tokenUrl = Get-ApiUrl "/api/v1/dev/token"
    $body = @{ usuarioId = $UsuarioId; oficinaId = $OficinaId } | ConvertTo-Json
    $res = Invoke-WebRequest -Uri $tokenUrl -Method Post -Body $body -ContentType "application/json" -UseBasicParsing
    if ($res.StatusCode -ne 200) {
        throw "dev/token HTTP $($res.StatusCode)"
    }
    return ($res.Content | ConvertFrom-Json).accessToken
}

function Invoke-ApiJson([hashtable]$Headers, [string]$path) {
    $url = Get-ApiUrl $path
    $res = Invoke-WebRequest -Uri $url -Headers $Headers -UseBasicParsing
    if ($res.StatusCode -ne 200) {
        throw "HTTP $($res.StatusCode) $url"
    }
    return ($res.Content | ConvertFrom-Json)
}

function Invoke-ApiPdf([hashtable]$Headers, [string]$path) {
    $url = Get-ApiUrl $path
    $res = Invoke-WebRequest -Uri $url -Headers $Headers -UseBasicParsing
    if ($res.StatusCode -ne 200) {
        throw "HTTP $($res.StatusCode) $url"
    }
    return $res
}

function Test-SqlReachable {
    try {
        $null = Invoke-SqlScalar "SELECT 1"
        return $true
    }
    catch {
        return $false
    }
}

Write-Host ""
Write-Host "=== Paridad informes Fase 4 (API + SQL) ===" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl  OficinaId: $OficinaId"
Write-Host ""

$envCs = Read-EnvConnectionString
if ($envCs) {
    $pwd = Parse-SqlPasswordFromConnectionString $envCs
    if ($pwd) { $SqlPassword = $pwd }
    if ($envCs -match 'Server=([^;]+)') {
        $fromEnv = $Matches[1]
        # .env de Docker usa host.docker.internal; este script corre en el host Windows.
        if ($fromEnv -match 'host\.docker\.internal') {
            $SqlServer = "localhost,14330"
        }
        else {
            $SqlServer = $fromEnv
        }
    }
}

if (-not $SkipSqlDiscovery) {
    if (-not (Test-SqlReachable)) {
        Write-ParidadSkip "SQL no accesible ($SqlServer). Pasa IDs manualmente o levanta CREDITO."
        $SkipSqlDiscovery = $true
    }
    else {
        Write-Host ">> Descubrir IDs en CREDITO" -ForegroundColor Gray
        try {
        if ($MovimientoCajaCuotaId -lt 1) {
            $MovimientoCajaCuotaId = Invoke-SqlScalar @"
SELECT TOP 1 mc.MovimientoCajaId
FROM CREDITO.MovimientoCaja AS mc
WHERE mc.Operacion = 'CUO' AND mc.Estado = CAST(1 AS bit)
  AND EXISTS (
    SELECT 1 FROM CREDITO.PlanPago AS pp
    WHERE pp.MovimientoCajaId = mc.MovimientoCajaId AND pp.Estado = 'PAG')
ORDER BY mc.MovimientoCajaId DESC;
"@
        }
        if ($MovimientoCajaLibreId -lt 1) {
            $MovimientoCajaLibreId = Invoke-SqlScalar @"
SELECT TOP 1 mc.MovimientoCajaId
FROM CREDITO.MovimientoCaja AS mc
WHERE mc.Operacion = 'CUO' AND mc.Estado = CAST(1 AS bit)
  AND NOT EXISTS (
    SELECT 1 FROM CREDITO.PlanPago AS pp
    WHERE pp.MovimientoCajaId = mc.MovimientoCajaId AND pp.Estado = 'PAG')
ORDER BY mc.MovimientoCajaId DESC;
"@
        }
        if ($PersonaId -lt 1) {
            $PersonaId = Invoke-SqlScalar @"
SELECT TOP 1 c.PersonaId FROM MAESTRO.Cliente AS c ORDER BY c.PersonaId;
"@
        }
        if ($CreditoId -lt 1) {
            $CreditoId = Invoke-SqlScalar @"
SELECT TOP 1 c.CreditoId FROM CREDITO.Credito AS c WHERE c.Estado = 'DES' ORDER BY c.CreditoId DESC;
"@
        }
        if ($MovimientoAlmacenId -lt 1) {
            $MovimientoAlmacenId = Invoke-SqlScalar @"
SELECT TOP 1 m.MovimientoId FROM ALMACEN.Movimiento AS m ORDER BY m.MovimientoId DESC;
"@
        }
        if ($ProductoId -lt 1) {
            $ProductoId = Invoke-SqlScalar "SELECT TOP 1 ProductoId FROM CREDITO.Producto ORDER BY ProductoId;"
        }
        Write-Host "    MovCajaCuota=$MovimientoCajaCuotaId MovCajaLibre=$MovimientoCajaLibreId Persona=$PersonaId Credito=$CreditoId MovAlm=$MovimientoAlmacenId Producto=$ProductoId" -ForegroundColor Gray
        }
        catch {
            Write-ParidadSkip "descubrimiento SQL: $($_.Exception.Message)"
            $SkipSqlDiscovery = $true
        }
    }
}

Write-Host ">> JWT dev/token" -ForegroundColor Gray
$token = Get-DevToken
$headers = @{ Authorization = "Bearer $token" }

# --- Simulador ADE (regla de negocio corregida) ---
Write-Host ">> Simulador plan pagos (ga=ADE)" -ForegroundColor Gray
if ($ProductoId -lt 1) {
    Write-ParidadSkip "simulador: sin ProductoId"
}
else {
    try {
        $monto = 1000
        $gastos = 150
        $q = "productoId=$ProductoId&monto=$monto&nroCuotas=6&interesMensual=5&fechaPrimerPago=2026-06-01&formaPago=M&gastosAdm=$gastos&ga=ADE&cliente=Smoke"
        $sim = Invoke-ApiJson $headers "/api/v1/credito/rpt-simulador-plan-pagos?$q"
        if ($sim.cuotas.Count -lt 1) {
            Write-ParidadFail "simulador ADE: sin cuotas"
        }
        else {
            $maxGastos = ($sim.cuotas | ForEach-Object {
                $g = if ($null -eq $_.gastosAdm) { 0 } else { [decimal]$_.gastosAdm }
                $g
            } | Measure-Object -Maximum).Maximum
            if ($maxGastos -gt 0) {
                Write-ParidadFail "simulador ADE: cuotas con gastosAdm>0 (max=$maxGastos); esperado 0 como legacy cboGA=ADE"
            }
            else {
                Write-ParidadOk "simulador ADE: cuotas sin gastos en filas (max gastosAdm=0)"
            }
            $desembTxt = [string]$sim.cabecera.desembolso
            if ($desembTxt -notmatch '([\d]+(?:[.,]\d+)?)') {
                Write-ParidadFail "simulador ADE: desembolso no parseable ($desembTxt)"
            }
            else {
                $desembVal = [decimal]($Matches[1] -replace ',', '.')
                if ($desembVal -ne [decimal]$monto) {
                    Write-ParidadFail "simulador ADE: desembolso=$desembTxt ($desembVal) esperado monto $monto"
                }
                else {
                    Write-ParidadOk "simulador ADE: desembolso = monto ($monto)"
                }
            }
        }
    }
    catch {
        Write-ParidadFail "simulador ADE: $($_.Exception.Message)"
    }
}

# --- Cliente ---
if ($PersonaId -ge 1) {
    try {
        $cli = Invoke-ApiJson $headers "/api/v1/credito/rpt-cliente?personaId=$PersonaId"
        if (-not $cli.ficha.cliente) {
            Write-ParidadFail "rpt-cliente: ficha sin campo cliente"
        }
        else {
            Write-ParidadOk "rpt-cliente personaId=$PersonaId ($($cli.ficha.cliente))"
        }
    }
    catch {
        Write-ParidadFail "rpt-cliente: $($_.Exception.Message)"
    }
}
else {
    Write-ParidadSkip "rpt-cliente: sin PersonaId"
}

# --- Estado crédito + plan pagos ---
if ($CreditoId -ge 1) {
    try {
        $est = Invoke-ApiJson $headers "/api/v1/credito/rpt-estado-credito?creditoId=$CreditoId"
        $sumInteres = ($est.cuotas | ForEach-Object { [decimal]$_.interes } | Measure-Object -Sum).Sum
        $totalCalc = [decimal]$est.cabecera.montoCredito + $sumInteres
        $totalApi = [decimal]$est.cabecera.total
        if ([Math]::Abs($totalCalc - $totalApi) -gt [decimal]0.02) {
            Write-ParidadFail "rpt-estado-credito: total API=$totalApi calc=$totalCalc (MontoCredito+SumInteres)"
        }
        else {
            Write-ParidadOk "rpt-estado-credito creditoId=$CreditoId total=$totalApi"
        }
    }
    catch {
        Write-ParidadFail "rpt-estado-credito: $($_.Exception.Message)"
    }

    try {
        $plan = Invoke-ApiJson $headers "/api/v1/credito/rpt-plan-pagos?creditoId=$CreditoId"
        if ($plan.cuotas.Count -lt 1) {
            Write-ParidadSkip "rpt-plan-pagos creditoId=$CreditoId sin cuotas"
        }
        else {
            Write-ParidadOk "rpt-plan-pagos creditoId=$CreditoId ($($plan.cuotas.Count) filas)"
        }
    }
    catch {
        Write-ParidadFail "rpt-plan-pagos: $($_.Exception.Message)"
    }
}
else {
    Write-ParidadSkip "informes crédito: sin CreditoId"
}

# --- Tareas ---
try {
    $tareas = Invoke-ApiJson $headers "/api/v1/credito/rpt-credito-tarea?estado=PEN"
    Write-ParidadOk "rpt-credito-tarea PEN ($($tareas.Count) filas)"
}
catch {
    Write-ParidadFail "rpt-credito-tarea: $($_.Exception.Message)"
}

# --- Tickets caja PDF ---
foreach ($pair in @(
        @{ Id = $MovimientoCajaCuotaId; Label = "ticket CUO crédito" },
        @{ Id = $MovimientoCajaLibreId; Label = "ticket CUO libre" }
    )) {
    if ($pair.Id -lt 1) {
        Write-ParidadSkip "$($pair.Label): sin MovimientoCajaId en BD"
        continue
    }
    try {
        $pdf = Invoke-ApiPdf $headers "/api/v1/credito/movimiento-caja-ticket-pdf?oficinaId=$OficinaId&movimientoCajaId=$($pair.Id)"
        if ($pdf.RawContentLength -lt 400 -or $pdf.Content[0] -ne 37) {
            Write-ParidadFail "$($pair.Label): PDF vacío o inválido (id=$($pair.Id))"
        }
        else {
            Write-ParidadOk "$($pair.Label) PDF id=$($pair.Id) ($($pdf.RawContentLength) bytes)"
        }
    }
    catch {
        Write-ParidadFail "$($pair.Label): $($_.Exception.Message)"
    }
}

# --- Constancia almacén ---
if ($MovimientoAlmacenId -ge 1) {
    try {
        $alm = Invoke-ApiJson $headers "/api/v1/almacen/rpt-constancia-almacen?movimientoId=$MovimientoAlmacenId"
        if (-not $alm.cabecera.movimientoId) {
            Write-ParidadFail "constancia: cabecera incompleta"
        }
        elseif ($alm.detalle.Count -lt 1) {
            Write-ParidadSkip "constancia movimientoId=$MovimientoAlmacenId sin detalle"
        }
        else {
            Write-ParidadOk "constancia almacén movimientoId=$MovimientoAlmacenId ($($alm.detalle.Count) líneas)"
        }
    }
    catch {
        Write-ParidadFail "constancia almacén: $($_.Exception.Message)"
    }
}
else {
    Write-ParidadSkip "constancia almacén: sin MovimientoId"
}

# --- Comprobantes caja chica (últimos 90 días) ---
try {
    $fin = (Get-Date).ToString("yyyy-MM-dd")
    $ini = (Get-Date).AddDays(-90).ToString("yyyy-MM-dd")
    $comp = Invoke-ApiJson $headers "/api/v1/credito/rpt-comprobantes-caja-chica?fechaIni=$ini&fechaFin=$fin"
    Write-ParidadOk "rpt-comprobantes-caja-chica $ini..$fin ($($comp.Count) filas)"
}
catch {
    $code = 0
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    if ($code -eq 404) {
        Write-ParidadSkip "comprobantes caja chica: sin datos en rango"
    }
    else {
        Write-ParidadFail "comprobantes caja chica: $($_.Exception.Message)"
    }
}

Write-Host ""
Write-Host "Resumen: OK=$passed SKIP=$skipped FAIL=$failed" -ForegroundColor Cyan

$evidenceDir = Join-Path $repoRoot "docs\migration\evidence"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}
$stamp = Get-Date -Format "yyyy-MM-dd_HHmm"
$logPath = Join-Path $evidenceDir "paridad-fase4-$stamp.txt"
@(
    "Paridad Fase 4 $stamp",
    "BaseUrl=$BaseUrl OficinaId=$OficinaId",
    "IDs: MovCajaCuota=$MovimientoCajaCuotaId MovCajaLibre=$MovimientoCajaLibreId Persona=$PersonaId Credito=$CreditoId MovAlm=$MovimientoAlmacenId Producto=$ProductoId",
    "OK=$passed SKIP=$skipped FAIL=$failed"
) | Set-Content -Path $logPath -Encoding UTF8
Write-Host "Evidencia: $logPath" -ForegroundColor Gray

if ($failed -gt 0) {
    if ($AllowSkipAll) {
        Write-Host "AVISO: fallos presentes (-AllowSkipAll)." -ForegroundColor Yellow
        exit 0
    }
    exit 1
}

Write-Host "Paridad Fase 4 OK." -ForegroundColor Green
exit 0
