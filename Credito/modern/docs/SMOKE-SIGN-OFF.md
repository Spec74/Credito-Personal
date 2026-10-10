# Smoke de producción — firma para 100% operativo

**Corrida firmada:** 2026-10-09 · usuario `admvendix` · oficina `1` · API Azure + SPA Vercel.

> **Seguridad:** la clave usada en esta corrida circuló por chat. Rotar `admvendix` en producción cuando puedan.

## Automatizado

```powershell
cd Credito/modern

.\deploy\scripts\verify-production-config.ps1 -RequireForwardedHeaders

.\deploy\scripts\smoke-ops-checklist.ps1 `
  -BaseUrl "https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net" `
  -NombreUsuario "admvendix" -Clave "***" -OficinaId 1
```

### Evidencia 2026-10-09

| Chequeo | Resultado |
|---------|-----------|
| `verify-production-config.ps1 -RequireForwardedHeaders` | **PASS** |
| Azure `/health` + `/health/ready` (database Healthy) | **PASS** |
| `/dev/token` en prod | **PASS** (HTTP 404) |
| `smoke-ops-checklist` login real `admvendix` | **PASS** (12/12) |
| Cobro diario PDF API | **PASS** (~96 KB, `application/pdf`, gestor 1031) |
| Generar ruta cobros API | **PASS** — gestor 1025, 5 paradas, **3 con GPS**, `urlNavegacionGoogle` presente |
| `smoke-db-gate` | **PASS** SQL local user-secrets; Azure SQL vía `/health/ready` |
| SPA login Vercel → `/inicio` | **PASS** — roles visibles: ADMINISTRADOR, APROBADOR 1/2, CAJA |
| Menú ACL admin (Reportes, Crédito, Caja Diario, Morosos, …) | **PASS** |
| `/caja/diario` | **PASS** (pantalla OK; sin caja abierta para admvendix — mensaje claro) |
| `/informes/cobro-diario` | **PASS** (filtros + tabla; export habilitado tras consultar) |

## Manual — firma

| # | Prueba | OK | Fecha | Quién |
|---|--------|----|-------|-------|
| 1 | Login SPA en Vercel **sin** `/dev/token` | ✅ | 2026-10-09 | agente + admvendix |
| 2 | Menú ACL por rol: gestor | ⚠ | — | Mismo token trae CAJA; falta login de un **solo-gestor** |
| 3 | Menú ACL por rol: cajero | ❌→pendiente | 2026-10-10 | Intento `Jvillalobos`/`JVILLALOBOS` + oficina 3 (y 1): API **401** «Usuario, clave u oficina no válidos». Usuario canónico en maestros: **`JVILLALOBOS`** (id 1025). Confirmar clave/oficina y reintentar. |
| 4 | Menú ACL por rol: encargado / aprobador | ✅* | 2026-10-09 | *Roles APROBADOR 1/2 en sesión admvendix (no usuario dedicado) |
| 5 | Menú ACL por rol: admin | ✅ | 2026-10-09 | agente |
| 6 | Caja diario: abrir sesión → cobrar → ticket | ⚠ | 2026-10-09 | Pantalla OK; **admvendix sin caja abierta** — asignar caja y repetir cobro |
| 7 | **1 PDF prod** cobro diario / morosidad | ✅ | 2026-10-09 | API 96 KB PDF |
| 8 | Ruta del cobrador: 2+ morosos → mapa + Navegar | ✅ | 2026-10-09 | API: 3 GPS + URL Google Maps; SPA requiere caja abierta |
| 9 | `smoke-db-gate` / SQL Azure | ✅ | 2026-10-09 | ready database Healthy + gate local |
| 10 | Responsive ~375px | ⚠ | — | Foto login OK; falta pase formal 375px caja/informe |

## Criterio de cierre

**Operativo casi cerrado** con login real + PDF + ruta API.

Queda para 100% estricto SSD-00 §7:

1. Confirmar clave/oficina de **`JVILLALOBOS`** (cajero) y re-correr smoke (#3 / #6).
2. Asignar/abrir caja y hacer **1 cobro + ticket** (#6).
3. (Opcional) Captura responsive 375px (#10).
4. Rotar claves de `admvendix` y del cajero usadas en chat.
