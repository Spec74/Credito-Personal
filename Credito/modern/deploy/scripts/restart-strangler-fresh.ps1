# Reconstruye imagen API y reinicia proxy (corrige 404 por Docker desactualizado).
# Uso: .\deploy\scripts\restart-strangler-fresh.ps1

$ErrorActionPreference = "Stop"
$modernRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$composeFile = Join-Path $modernRoot "deploy\docker-compose.strangler.yml"
$envFile = Join-Path $modernRoot "deploy\.env"

function Invoke-DockerCompose {
    param([string[]]$DockerArgs)
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $lines = & docker @DockerArgs 2>&1
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
            throw "docker $($DockerArgs -join ' ') salio con codigo $LASTEXITCODE"
        }
    }
    finally {
        $ErrorActionPreference = $prevEap
    }
}

Push-Location $modernRoot
try {
    Write-Host "Deteniendo stack strangler..." -ForegroundColor Cyan
    Invoke-DockerCompose -DockerArgs @(
        "compose", "-f", $composeFile, "--env-file", $envFile, "down"
    )
    Write-Host "Limpiando cache de build (evita snapshot parent not found)..." -ForegroundColor Cyan
    Invoke-DockerCompose -DockerArgs @("builder", "prune", "-f")
    Write-Host "Levantando con --build..." -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot "start-strangler.ps1") -Build
    Write-Host ""
    & (Join-Path $PSScriptRoot "verify-strangler-proxy.ps1")
}
finally {
    Pop-Location
}
