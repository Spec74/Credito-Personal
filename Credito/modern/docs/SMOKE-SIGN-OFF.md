# Smoke de producción — firma para 100% operativo

**Corrida firmada:** 2026-10-10 · cajero `JVILLALOBOS` + cajas abiertas · API Azure + SPA Vercel.  
**Corrida admin previa:** 2026-10-09 · `admvendix` · oficina `1`.

> **Seguridad:** claves (`123.` / admin) circularon por chat. **Rotar** en producción cuando puedan. No versionar hashes ni secretos.

## Automatizado

```powershell
cd Credito/modern

.\deploy\scripts\verify-production-config.ps1 -RequireForwardedHeaders

.\deploy\scripts\smoke-ops-checklist.ps1 `
  -BaseUrl "https://crediconfiable-api-g3h7fja3dydsbbb7.centralus-01.azurewebsites.net" `
  -NombreUsuario "admvendix" -Clave "***" -OficinaId 1
```

### Evidencia 2026-10-09 (admin)

| Chequeo | Resultado |
|---------|-----------|
| `verify-production-config.ps1 -RequireForwardedHeaders` | **PASS** |
| Azure `/health` + `/health/ready` (database Healthy) | **PASS** |
| `/dev/token` en prod | **PASS** (HTTP 404) |
| `smoke-ops-checklist` login real `admvendix` | **PASS** (12/12) |
| Cobro diario PDF API | **PASS** (~96 KB, gestor 1031) |
| Generar ruta cobros API | **PASS** — gestor 1025, 5 paradas, 3 con GPS |
| `smoke-db-gate` | **PASS** SQL local + Azure `/health/ready` |
| SPA login Vercel → `/inicio` | **PASS** — ADMINISTRADOR, APROBADOR 1/2, CAJA |

### Evidencia 2026-10-10 (cajeros + cajas abiertas)

Clave de prueba: `123.` · oficina `1`.

| Usuario | Caja abierta | Login API | Roles (`/auth/me`) |
|---------|--------------|-----------|---------------------|
| `JVILLALOBOS` (1025) | 29573 CAJA CENTRAL 2 | **PASS** | ANALISTA, CAJA, CAJA CENTRAL, ENCARGADO, REPORTE, REPORTEPARCIAL |
| `YCERVANTES` (9) | 29574 CARMEN ALTO | **PASS** | ANALISTA, CAJA |
| `MVENTURA` (17) | 29575 CORAZON DE JESUS | **PASS** | (login OK) |
| `DSANCHEZ` (1033) | 29576 ESPERANZA | **PASS** | (login OK) |
| `JPERALES` (1028) | 29577 VIRGEN DE CHAPI | **PASS** | (login OK) |
| `RMANTILLA` (1021) | 29578 CAJA CENTRAL 1 | **PASS** | ADMINISTRADOR, ANALISTA, APROBADOR 1/2, CAJA |
| `APARIONA` (16) | 29579 SANTA ELENA | **PASS** | ANALISTA, CAJA |

| Chequeo | Resultado |
|---------|-----------|
| SPA `JVILLALOBOS` → `/caja/diario` sesión **ABIERTO** CAJA CENTRAL 2 | **PASS** — Entradas S/ 55.00 (neto = cobro smoke) |
| Cobro + ticket | **PASS** — crédito 87250 / planPago 1727658 → movimiento **2287634**, ticket PDF **~36 KB** (`%PDF`) |
| Cobros del día PDF API | **PASS** — `rpt-cobro-diario-pdf` **~145 KB** (`%PDF`) |
| Cobros del día PDF SPA | **PASS** — visor `/reportes/visor` 2 páginas |
| Ruta del cobrador SPA | **PASS** — 13 paradas, mapa Leaflet, **3 con GPS**, aviso 10 sin GPS, **Navegar en Google Maps** + WhatsApp |
| Menú ACL gestor/cajero (`YCERVANTES` / `APARIONA`) | **PASS** — CREDITO, CREDITOS, CLIENTE, SIMULADOR, CAJA DIARIO, TAREAS, PRENDARIO (sin SEGURIDAD/admin) |
| Menú ACL aprobador/admin (`RMANTILLA`) | **PASS** — APROBACION, BOVEDA, SEGURIDAD, DASHBOARD, CONDONACION, … |
| Responsive ~375px `/caja/diario` | **PASS** — cabecera compacta, KPIs apilados, menú hamburguesa |

## Manual — firma

| # | Prueba | OK | Fecha | Quién |
|---|--------|----|-------|-------|
| 1 | Login SPA en Vercel **sin** `/dev/token` | ✅ | 2026-10-09 | agente + admvendix |
| 2 | Menú ACL por rol: gestor | ✅ | 2026-10-10 | `YCERVANTES` / `APARIONA` — ANALISTA+CAJA, menú crédito/caja sin admin |
| 3 | Menú ACL por rol: cajero | ✅ | 2026-10-10 | `JVILLALOBOS` + clave `123.` / ofi 1 — CAJA DIARIO, sesión abierta |
| 4 | Menú ACL por rol: encargado / aprobador | ✅ | 2026-10-10 | `RMANTILLA` — APROBADOR 1/2 + ADMIN; ENCARGADO en `JVILLALOBOS` |
| 5 | Menú ACL por rol: admin | ✅ | 2026-10-09/10 | admvendix + RMANTILLA |
| 6 | Caja diario: sesión abierta → cobrar → ticket | ✅ | 2026-10-10 | `JVILLALOBOS` caja 29573; mov. 2287634; ticket ~36 KB |
| 7 | **1 PDF prod** cobro diario / morosidad | ✅ | 2026-10-10 | API ~145 KB + SPA visor |
| 8 | Ruta del cobrador: 2+ morosos → mapa + Navegar | ✅ | 2026-10-10 | SPA 13 paradas / 3 GPS + Google Maps |
| 9 | `smoke-db-gate` / SQL Azure | ✅ | 2026-10-09 | ready database Healthy + gate local |
| 10 | Responsive ~375px | ✅ | 2026-10-10 | `/caja/diario` Emulation 375×812 |

## Criterio de cierre

**Operativo 100% (SSD-00 §7 smoke)** con evidencia 2026-10-10:

- Login real cajeros (`123.`) + 7 cajas abiertas verificadas.
- Cobro + ticket PDF en producción.
- Ruta cobrador SPA (mapa + navegar).
- ACL por rol (gestor/cajero/aprobador/admin).
- PDF cobro diario + responsive 375px.

**Pendiente operativo (no bloquea smoke):** rotar claves expuestas en chat.
