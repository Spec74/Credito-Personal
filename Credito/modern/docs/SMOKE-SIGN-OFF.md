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

## GPS cliente — cobro-bloque + ficha (SSD-03 / SSD-05 §7)

Evidencia **automatizada** (código, 2026-10-10): `clienteMapaNavegacion.test.ts`, `creditoGestorPendiente.test.ts`, `cobroBloqueOffline.test.ts`, `deviceGeolocation.test.ts`, `clienteendpointstests` (ubicacion 401/400), `ClienteGpsRules`.

Evidencia **operativa HTTPS** (marcar al ejecutar en Dev/preprod; no inventar PASS):

| # | Paso | Entorno | Resultado | Fecha | Notas |
|---|------|----------|-----------|-------|-------|
| G1 | PC `/caja/cobro-bloque`: columna **Ubicación** (dirección → maps); **sin** «Registrar mi ubicación» | | ☐ | | Viewport ≥ md |
| G2 | Móvil (&lt; md): cliente **sin** GPS → botón + confirm → guarda; pill GPS; enlace usa coords | | ☐ | | HTTPS + permiso ubicación |
| G3 | Móvil offline: tipado OK; GPS muestra aviso de red | | ☐ | | |
| G4 | Ficha `/clientes/editar/:id` → Ubicación: «Actualizar GPS del dispositivo» corrige pin | | ☐ | | |
| G5 | Geocode distrito = «Vista aproximada»; no oculta botón de campo hasta Ubicar/GPS/arrastre + Guardar | | ☐ | | |

Al firmar G1–G5, marcar las casillas GPS en [SSD-03 §7](ssd/SSD-03-caja.md) y [SSD-05 §7](ssd/SSD-05-clientes.md).

## Criterio de cierre

**Operativo 100% (SSD-00 §7 smoke)** con evidencia 2026-10-10:

- Login real cajeros (`123.`) + 7 cajas abiertas verificadas.
- Cobro + ticket PDF en producción.
- Ruta cobrador SPA (mapa + navegar).
- ACL por rol (gestor/cajero/aprobador/admin).
- PDF cobro diario + responsive 375px.

**Pendiente operativo (no bloquea smoke):** rotar claves expuestas en chat.

## E2E plataforma (rol × módulo)

Corrida completa por perfiles ACL: ver **[E2E-ROLE-MODULE-MATRIX.md](./E2E-ROLE-MODULE-MATRIX.md)**.

| Suite Playwright | Resultado 2026-10-10 |
|------------------|----------------------|
| `e2e/role-module-matrix.spec.ts` | **4/4 PASS** (ADMIN, GESTOR_CAJA, APROBADOR_REPORTE, CAJERO_ENCARGADO) |
| `e2e/role-flows.spec.ts` | **4/4 PASS** (seguridad/bóveda/aprobación, cartera, saldos/reportes, caja ABIERTO) |

### Capa 3 — ciclo crédito (API prod)

| Paso | Evidencia |
|------|-----------|
| Crear → aprobar → desembolso → cobro | crédito **87691** · desembolso mov. **2287635** · pago **2287636** · ticket ~35 KB |
| Validar cierre | `puedeCerrar=true` (caja **no** cerrada) |
| Script | `deploy/scripts/smoke-ciclo-credito-capa3.ps1` |

### Capa 4 — bóveda / prendario / condonación / alta

| Flujo | Evidencia |
|-------|-----------|
| Bóveda → caja S/ 50 | movBoveda **77331** · movCaja **2287637** · ticket ~36 KB · rpt PDF ~109 KB |
| Prendario nuevo | crédito **87693** · desembolso **2287638** · contrato/acta PDF |
| Condonación | crédito **87691** solicitud **323** → ejecutada |
| Alta persona | personaId **8878** |
| SPA | `e2e/ciclo-capa4-extras.spec.ts` **2/2 PASS** |
| Script | `deploy/scripts/smoke-ciclo-capa4-extras.ps1` |

Detalle: [E2E-ROLE-MODULE-MATRIX.md](./E2E-ROLE-MODULE-MATRIX.md).
