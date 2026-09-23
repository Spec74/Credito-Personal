<#
.SYNOPSIS
  Gate de calidad local Credito Modern (metricas minimas de release).

.DESCRIPTION
  Ejecuta en secuencia los controles que deben pasar antes de cutover/release:
  - Backend: dotnet test (0 fallos) u opcionalmente cobertura line %
  - Frontend: vitest (+ coverage), eslint (0 errores), tsc -b, vite build
  - Strangler (opcional): health + verify-strangler-proxy

  Codigos de salida: 0 = OK, 1 = fallo en algun gate.

.EXAMPLE
  .\deploy\scripts\quality-gate.ps1
  .\deploy\scripts\quality-gate.ps1 -WithCoverage -SkipDocker
#>
[CmdletBinding()]
param(
    [switch]$SkipDocker,
    [switch]$SkipBuild,
    [switch]$SkipBackend,
    [switch]$WithCoverage,
    # Baseline local ~46% lineas (Api+App+Domain+Infra). Umbral anti-regresion.
    [ValidateRange(0, 100)]
    [int]$BackendCoverageThreshold = 40
)

$ErrorActionPreference = 'Stop'
$modernRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$webRoot = Join-Path $modernRoot 'Credito.Modern.Web'
$failed = 0

function Write-Gate([string]$name, [bool]$ok, [string]$detail = '') {
    $msg = if ($detail) { "$name - $detail" } else { $name }
    if ($ok) {
        Write-Host "OK  $msg" -ForegroundColor Green
    }
    else {
        Write-Host "FAIL $msg" -ForegroundColor Red
        $script:failed++
    }
}

Write-Host ""
Write-Host "=== Credito Modern quality gate ===" -ForegroundColor Cyan
Write-Host "Root: $modernRoot"
Write-Host ""

Push-Location $modernRoot
try {
    if ($SkipBackend) {
        Write-Host ">> Backend: skipped (-SkipBackend)" -ForegroundColor DarkGray
    }
    elseif ($WithCoverage) {
        Write-Host ">> Backend: coverage (>= $BackendCoverageThreshold% lineas)" -ForegroundColor Yellow
        $covOk = $false
        try {
            & (Join-Path $PSScriptRoot 'test-coverage-backend.ps1') -Threshold $BackendCoverageThreshold
            $covOk = (($null -eq $LASTEXITCODE) -or ($LASTEXITCODE -eq 0))
        }
        catch {
            Write-Host $_.Exception.Message -ForegroundColor Red
            $covOk = $false
        }
        Write-Gate 'dotnet coverage' $covOk "umbral=$BackendCoverageThreshold%"
    }
    else {
        Write-Host ">> Backend: dotnet test" -ForegroundColor Yellow
        $testOut = & dotnet test Credito.Modern.Tests\Credito.Modern.Tests.csproj --nologo -v q 2>&1 | Out-String
        $testOk = $LASTEXITCODE -eq 0
        $summary = if ($testOut -match 'Superado:\s+(\d+).*Total:\s+(\d+)') {
            "pasados=$($Matches[1]) total=$($Matches[2])"
        } elseif ($testOut -match 'Passed!\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Total:\s+(\d+)') {
            "passed=$($Matches[2]) total=$($Matches[3])"
        } else { '' }
        Write-Gate 'dotnet test' $testOk $summary
    }
}
finally {
    Pop-Location
}

Push-Location $webRoot
try {
    # npm escribe warnings a stderr; en PS eso + pipeline rompe LASTEXITCODE.
    function Invoke-NpmGate([string]$ArgsLine, [string]$LogName) {
        $log = Join-Path $webRoot $LogName
        cmd /c "npm $ArgsLine > `"$log`" 2>&1"
        $code = $LASTEXITCODE
        $text = if (Test-Path $log) { Get-Content $log -Raw -ErrorAction SilentlyContinue } else { '' }
        return @{ Ok = ($code -eq 0); ExitCode = $code; Text = $text }
    }

    Write-Host ">> Frontend: vitest$(if ($WithCoverage) { ' + coverage' })" -ForegroundColor Yellow
    $v = if ($WithCoverage) {
        Invoke-NpmGate 'run test:coverage' 'qg-vitest.log'
    } else {
        Invoke-NpmGate 'test' 'qg-vitest.log'
    }
    $vSum = if ($v.Text -match 'Tests\s+(\d+)\s+passed') { "tests=$($Matches[1])" } else { "exit=$($v.ExitCode)" }
    if ($WithCoverage -and $v.Text -match 'Lines\s*:\s*([\d.]+)\s*%') {
        $vSum = ($vSum + " lines=$($Matches[1])%").Trim()
    }
    Write-Gate $(if ($WithCoverage) { 'vitest coverage' } else { 'vitest' }) $v.Ok $vSum

    Write-Host ">> Frontend: eslint" -ForegroundColor Yellow
    $lint = Invoke-NpmGate 'run lint' 'qg-lint.log'
    $lintOk = $lint.Ok -and ($lint.Text -notmatch '✖\s+[1-9]\d*\s+problems?\s+\([1-9]')
    Write-Gate 'eslint (0 errors)' $lintOk "exit=$($lint.ExitCode)"

    Write-Host ">> Frontend: tsc -b" -ForegroundColor Yellow
    $tscLog = Join-Path $webRoot 'qg-tsc.log'
    cmd /c "npx tsc -b --pretty false > `"$tscLog`" 2>&1"
    Write-Gate 'typescript (tsc -b)' ($LASTEXITCODE -eq 0) "exit=$LASTEXITCODE"

    if (-not $SkipBuild) {
        Write-Host ">> Frontend: vite build" -ForegroundColor Yellow
        $b = Invoke-NpmGate 'run build' 'qg-build.log'
        $chunkWarn = $b.Text -match 'chunks are larger than'
        Write-Gate 'vite production build' $b.Ok $(if ($chunkWarn) { 'con avisos de chunk' } else { 'sin avisos >limite' })
    }
}
finally {
    Pop-Location
}

if (-not $SkipDocker) {
    Write-Host ">> Strangler: health" -ForegroundColor Yellow
    $healthOk = $false
    try {
        $r = Invoke-WebRequest 'http://127.0.0.1:9080/health' -UseBasicParsing -TimeoutSec 8
        $healthOk = $r.StatusCode -eq 200
    }
    catch { }
    Write-Gate 'GET /health via proxy :9080' $healthOk

    if ($healthOk) {
        Write-Host ">> Strangler: verify-strangler-proxy.ps1" -ForegroundColor Yellow
        & (Join-Path $modernRoot 'deploy\scripts\verify-strangler-proxy.ps1') 2>&1 | Out-Null
        Write-Gate 'verify-strangler-proxy' (($null -eq $LASTEXITCODE) -or ($LASTEXITCODE -eq 0))
    }
}

Write-Host ""
if ($failed -eq 0) {
    Write-Host "Quality gate PASSED (release-ready local)." -ForegroundColor Green
    exit 0
}

Write-Host "Quality gate FAILED ($failed control(es))." -ForegroundColor Red
exit 1
