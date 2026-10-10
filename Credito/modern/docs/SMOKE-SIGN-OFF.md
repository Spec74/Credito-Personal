# Smoke de producción — firma para 100% operativo

El código de producto puede estar listo; el **100% operativo** requiere evidencia firmada en el ambiente Azure real.

## Automatizado (capturar salida)

Desde `Credito/modern`:

```powershell
# 1) Gate de esquema SQL (connection string de Azure)
.\deploy\scripts\smoke-db-gate.ps1 -ConnectionString "<ADO.NET Azure>"

# 2) Config de producción
.\deploy\scripts\verify-production-config.ps1

# 3) Smoke HTTP contra API Azure (con credenciales reales, no /dev/token)
.\deploy\scripts\smoke-ops-checklist.ps1 -BaseUrl "https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net"
```

## Manual — marcar con fecha / responsable

| # | Prueba | OK | Fecha | Quién |
|---|--------|----|-------|-------|
| 1 | Login SPA en Vercel **sin** `/dev/token` | ☐ | | |
| 2 | Menú ACL por rol: gestor | ☐ | | |
| 3 | Menú ACL por rol: cajero | ☐ | | |
| 4 | Menú ACL por rol: encargado / aprobador | ☐ | | |
| 5 | Menú ACL por rol: admin | ☐ | | |
| 6 | Caja diario: abrir sesión → cobrar → ticket | ☐ | | |
| 7 | **1 PDF prod**: informe cobro diario o morosidad (descarga/visor OK) | ☐ | | |
| 8 | Ruta del cobrador: 2+ morosos → mapa + Navegar | ☐ | | |
| 9 | `smoke-db-gate.ps1` contra BD Azure (salida adjunta) | ☐ | | |
| 10 | Responsive ~375px: login, caja, un informe | ☐ | | |

## Criterio de cierre

Cuando la tabla esté completa y los scripts 1–3 hayan pasado, el cutover operativo se considera **firmado** (SSD-00 §7). Hasta entonces el producto puede estar al 100% de código y UX, pero no de operaciones firmadas.
