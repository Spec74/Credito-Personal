# Levanta el stack strangler (API + nginx) en segundo plano.
# Uso: desde modern/  ->  .\deploy\scripts\start-strangler.ps1
# Smoke: .\deploy\scripts\smoke-strangler-proxy.ps1

param(
    [switch]$Foreground,
    [switch]$Build,
    [switch]$Fresh
)

$ErrorActionPreference = "Stop"

function Invoke-DockerComposeUp {
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

$modernRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$composeFile = Join-Path $modernRoot "deploy\docker-compose.strangler.yml"
$envFile = Join-Path $modernRoot "deploy\.env"
$envLocalFile = Join-Path $modernRoot "deploy\.env.local"
$envExample = Join-Path $modernRoot "deploy\.env.example"

if (Test-Path $envLocalFile) {
    $envFile = $envLocalFile
    Write-Host 'Usando deploy\.env.local (secretos locales).' -ForegroundColor Gray
}
elseif (-not (Test-Path $envFile)) {
    if (-not (Test-Path $envExample)) {
        throw 'No existe deploy\.env ni deploy\.env.example'
    }
    Copy-Item $envExample $envFile
    Write-Host 'Creado deploy\.env desde .env.example - edita SQL y JWT_SIGNING_KEY antes de continuar.' -ForegroundColor Yellow
}

Push-Location $modernRoot
try {
    $dockerArgs = @(
        "compose",
        "-f", $composeFile,
        "--env-file", $envFile,
        "up"
    )
    if ($Build -or $Fresh) { $dockerArgs += "--build" }
    if ($Fresh -and -not $Foreground) {
        Write-Host "Modo -Fresh: reconstruye imagen API (use restart-strangler-fresh.ps1 para down+up)." -ForegroundColor Yellow
    }
    if ($Foreground) {
        Invoke-DockerComposeUp -DockerArgs $dockerArgs
    }
    else {
        $dockerArgs += "-d"
        Invoke-DockerComposeUp -DockerArgs $dockerArgs
        Write-Host ""
        Write-Host "Stack en segundo plano. Fachada: http://localhost:9080  API directa: http://localhost:5080" -ForegroundColor Green
        Write-Host 'Verificar: .\deploy\scripts\verify-strangler-proxy.ps1' -ForegroundColor Gray
        Write-Host 'Smoke: .\deploy\scripts\smoke-strangler-proxy.ps1' -ForegroundColor Gray
        Write-Host 'Si rutas nuevas devuelven 404: vuelve a levantar con -Build' -ForegroundColor Gray
    }
}
finally {
    Pop-Location
}
