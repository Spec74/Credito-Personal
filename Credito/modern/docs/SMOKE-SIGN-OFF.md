# Smoke de producción — firma para 100% operativo

El código de producto puede estar listo; el **100% operativo** requiere evidencia firmada en el ambiente Azure real.

**Evidencia automatizada (2026-10-09, agente):** ver sección «Corrida».

## Automatizado (capturar salida)

Desde `Credito/modern`:

```powershell
# 1) Gate de esquema SQL (connection string de Azure)
.\deploy\scripts\smoke-db-gate.ps1 -ConnectionString "<ADO.NET Azure>"

# 2) Config de producción
.\deploy\scripts\verify-production-config.ps1 -RequireForwardedHeaders

# 3) Smoke HTTP contra API Azure (credenciales reales)
.\deploy\scripts\smoke-ops-checklist.ps1 `
  -BaseUrl "https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net" `
  -NombreUsuario "SU_USUARIO" -Clave "SU_CLAVE" -OficinaId 1
```

## Corrida 2026-10-09

| Script / chequeo | Resultado | Notas |
|------------------|-----------|--------|
| `verify-production-config.ps1 -RequireForwardedHeaders` | **PASS** | AllowDevToken=false, CORS=1, ForwardedHeaders App Service |
| Azure `GET /health` | **PASS** | 200 Healthy (self) |
| Azure `GET /health/ready` | **PASS** | 200 Healthy (self + **database**) |
| Azure `POST /api/v1/dev/token` | **PASS** (bloqueado) | HTTP **404** — sin bypass en prod |
| Azure `POST /api/v1/auth/login` (body vacío) | **PASS** | HTTP 400 (endpoint vivo) |
| `smoke-ops-checklist.ps1 -SkipJwt` | **PASS** | health only |
| `smoke-db-gate.ps1` | **PASS** (SQL local user-secrets) | host `localhost,14330` — **no** es la CS de Azure App Service; en Azure el gate de conectividad queda cubierto por `/health/ready` database Healthy. Para firmar #9 contra Azure: pegar CS de App Service Settings o Query editor Portal. |
| SPA login Vercel | **PASS** (carga) | `https://credito-personal.vercel.app/login` — formulario + oficina + control IP |

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
| 9 | `smoke-db-gate.ps1` contra BD **Azure** (salida adjunta) | ⚠ local OK / Azure via ready | 2026-10-09 | agente |
| 10 | Responsive ~375px: login, caja, un informe | ☐ | | |

## Criterio de cierre

Cuando la tabla esté completa (#1–#8 y #10) y los scripts 1–3 hayan pasado con **login real**, el cutover operativo se considera **firmado** (SSD-00 §7).

### Siguiente paso (tú)

Envía (o escribe en el chat) un usuario de producción con el que pueda entrar, p. ej.:

`usuario / clave / oficinaId`

Con eso cierro en esta sesión: login → menú admin → PDF cobro diario → ruta cobrador → marco #1, #5, #7, #8. Los roles #2–#4 los firmas tú cambiando de usuario o me das un segundo login por rol.
