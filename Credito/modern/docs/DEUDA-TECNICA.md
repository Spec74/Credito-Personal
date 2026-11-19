# Deuda técnica — Credito.Modern

**Fecha:** 2026-11-18 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md)

---

## Veredicto (honesto)

| Dimensión | Estado |
|-----------|--------|
| Docs as-built (prosa / SSD / índices) | **100%** escrito y navegable |
| Organización código (fachadas / capas) | **~90%** — splits hechos; quedan 4 archivos >1500 líneas |
| Ops / SSD §7 / cutover | **Parcial** — smokes Docker + firmas Azure/SMOKE reconciliadas; SSD-00 preprod y varios §7 módulo siguen abiertos |

No declarar «producto 100% cerrado» hasta cutover SSD-00 + humos humanos restantes.

---

## 1. Evidencia operativa (SSD §7)

Las casillas `[ ]` / `[x]` en SSD §7 registran **smoke ejecutado**, no huecos de prosa.

| Evidencia | Estado |
|-----------|--------|
| API Docker `:5080` — health, `dev/token`, Red 200/403, analista 200 | **Hecho** — `smoke-local-api.ps1` 2026-11-18 |
| Strangler `:9080` — proxy + JWT + Red/analista + reportes/ventas/MVC | **Hecho** — `smoke-strangler-proxy.ps1` 2026-11-18 |
| Login SPA real + cobro/ticket + GPS G1–G5 + ACL (Azure/Vercel) | **Hecho** — [SMOKE-SIGN-OFF.md](SMOKE-SIGN-OFF.md) 2026-10-09/12; casillas SSD-01/03/05 reconciliadas |
| Cutover preprod SSD-00 §7 (login `:9080` local, piloto un día, rollback, ALTER preprod) | **Pendiente ops humano** |
| SSD-01 refresh token SPA; SSD-03 impagos/mora/verificar/saldos PDF/hub | **Pendiente humo oficina** |
| Misma clave `$pbk2$` MVC↔API en preprod | **Pendiente humo cruzado** |

Pista: [GUIA-PRUEBAS-GO-LIVE.md](GUIA-PRUEBAS-GO-LIVE.md) · [SMOKE-SIGN-OFF.md](SMOKE-SIGN-OFF.md).

---

## 2. Archivos grandes — estado

| Archivo | Líneas ~ | Estado |
|---------|----------|--------|
| `creditobovedaoperacionendpoints*.cs` | facade + partials ≤1394 | **Hecho** |
| `creditoreportecreditocarteraendpoints*.cs` | partials ≤911 | **Hecho** |
| `creditocajadiarioendpoints*.cs` | partials ≤866 | **Hecho** |
| `ventas/ventasendpoints*.cs` | partials ≤920 | **Hecho** |
| `dashboardadminreadservice*.cs` | partial + Red SQL | **Hecho** |
| `AdminRedDashboard.tsx` | — | **Hecho** |
| `CobroBloquePage.tsx` | ~1511 | **Mejorado** — `CobroBloqueCuotasSubgrid` + `CobroBloqueColumns`; toolbar/offline opcionales |
| `creditofichagestionendpoints.cs` | ~1687 | Backlog split |
| `SimuladorCreditoPage.tsx` | ~1565 | Backlog split |
| `maestroreadendpoints.cs` | ~1518 | Backlog split |

---

## 3. Producto opcional (Fase C)

- Alertas persistentes / umbrales configurables en tablero
- Mini-comparativo sede vs promedio red

---

## 4. Datos / ranking

`usp_DashboardRanking` filtra `RolId = 6` (ANALISTA). Si en un ambiente el id difiere, ranking vacío es esperado — ver [dashboard_migracion.md](dashboard_migracion.md).

---

## 5. Hecho (no reabrir)

Tablero Red SQL multi-sede, ranking/meta analista, `COMO-LEER-EL-CODIGO`, inventario endpoints, partials bóveda/caja/cartera/ventas, extract cuotas + columns cobro, smokes Docker con Red 200/403 + analista, reconciliación SSD-01/03/05 vs SMOKE-SIGN-OFF (2026-11-18).
