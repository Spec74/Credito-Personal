$body = @{ usuarioId = 3; oficinaId = 1 } | ConvertTo-Json
$tokenRes = Invoke-RestMethod -Uri 'http://localhost:5288/api/v1/dev/token' -Method POST -ContentType 'application/json' -Body $body
$token = $tokenRes.accessToken
if (-not $token) { $token = $tokenRes.token }
$h = @{ Authorization = "Bearer $token" }
$all = Invoke-RestMethod -Uri 'http://localhost:5288/api/v1/credito/rpt-cobranza-pagos?oficinaId=1' -Headers $h
$g16 = Invoke-RestMethod -Uri 'http://localhost:5288/api/v1/credito/rpt-cobranza-pagos?usuarioId=16&oficinaId=1' -Headers $h
$nullOnly = Invoke-RestMethod -Uri 'http://localhost:5288/api/v1/credito/rpt-cobranza-pagos' -Headers $h
Write-Host "oficinaId=1 only: $($all.data.Count) totalClientes=$($all.resumen.totalClientes)"
Write-Host "usuarioId=16 oficinaId=1: $($g16.data.Count) totalClientes=$($g16.resumen.totalClientes)"
Write-Host "sin params (dev token user3): $($nullOnly.data.Count) totalClientes=$($nullOnly.resumen.totalClientes)"
