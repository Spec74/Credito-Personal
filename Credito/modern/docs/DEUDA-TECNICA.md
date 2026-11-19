# Deuda técnica — Credito.Modern

**Fecha:** 2026-11-18 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md)

---

## 1. Evidencia operativa (SSD §7)

Las casillas `[ ]` / `[x]` en SSD §7 registran **smoke ejecutado**, no huecos de prosa.

| Evidencia | Estado |
|-----------|--------|
| API Docker `:5080` — health, `dev/token`, Tablero Red 200 Central / 403 sucursal, analista 200 | Hecho 2026-11-18 (ver SSD-01 §7 / §9) |
| Login SPA con usuario real + menú | Pendiente firma humana preprod |
| Cutover / casillas SSD-00 | Pendiente ops |

Pista: [GUIA-PRUEBAS-GO-LIVE.md](GUIA-PRUEBAS-GO-LIVE.md) · [SMOKE-SIGN-OFF.md](SMOKE-SIGN-OFF.md).

---

## 2. Archivos grandes — estado

| Archivo | Estado |
|---------|--------|
| `creditobovedaoperacionendpoints*.cs` | **Hecho** — facade + consultas / cierres / saldos |
| `creditoreportecreditocarteraendpoints*.cs` | **Hecho** — morosidad / rentabilidad / activos |
| `creditocajadiarioendpoints*.cs` | **Hecho** — sesion / movimientos / desembolso (mantiene `Map…Sesion` + `Map…Operacion`) |
| `ventas/ventasendpoints*.cs` | **Hecho** — reportes / ordenes / rapida |
| `dashboardadminreadservice*.cs` | **Hecho** — partial + Red SQL |
| `AdminRedDashboard.tsx` | **Hecho** |
| `Web/.../CobroBloquePage.tsx` | Pendiente menor — extraer subpaneles cuando toque cobro (no bloquea) |

---

## 3. Producto opcional (Fase C)

- Alertas persistentes / umbrales configurables en tablero
- Mini-comparativo sede vs promedio red

---

## 4. Hecho (no reabrir)

Tablero Red SQL multi-sede, ranking/meta analista, `COMO-LEER-EL-CODIGO`, `readme` corto, inventario en `API-ENDPOINTS-INVENTARIO.md`, partials de endpoints gordos (2026-11-18).
