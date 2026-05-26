# Lista controladores MVC sin [Autenticado] a nivel de clase (Home/Login excluido por diseño).
$root = Join-Path $PSScriptRoot "..\..\..\Web\Controllers"
$sinAuth = @()
Get-ChildItem -Path $root -Recurse -Filter "*Controller.cs" | ForEach-Object {
    $text = Get-Content -LiteralPath $_.FullName -Raw
    if ($text -notmatch '\[Autenticado\]') {
        $sinAuth += $_.FullName.Substring($root.Length).TrimStart('\')
    }
}
if ($sinAuth.Count -eq 0) {
    Write-Host "OK: todos los controladores incluyen [Autenticado] (o revisar Home/Login manualmente)." -ForegroundColor Green
    exit 0
}
Write-Host "FALTA [Autenticado] en:" -ForegroundColor Red
$sinAuth | ForEach-Object { Write-Host "  $_" }
exit 1
