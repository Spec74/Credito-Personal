# Deuda técnica — Credito.Modern

**Fecha:** 2026-11-23 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md) · Piloto Azure: [AZURE-PILOTO-BASIC.md](AZURE-PILOTO-BASIC.md)

---

## Veredicto (honesto)

| Dimensión | Estado |
|-----------|--------|
| Docs as-built (prosa / SSD / índices) | **100%** escrito y navegable |
| Organización código (fachadas / capas) | **~90%** — splits hechos; quedan archivos >1500 líneas |
| Ops / SSD §7 / cutover | **Parcial** — piloto Azure básico arriba; SSD-00 preprod y varios §7 módulo siguen abiertos |

No declarar «producto 100% cerrado» hasta cutover SSD-00 + humos humanos restantes.

---

## 1. Evidencia operativa (SSD §7)

Las casillas `[ ]` / `[x]` en SSD §7 registran **smoke ejecutado**, no huecos de prosa.

| Evidencia | Estado |
|-----------|--------|
| API Docker local `:5080` — health, Red 200/403, analista | **Hecho** — `smoke-local-api.ps1` 2026-11-18 |
| Strangler `:9080` | **Hecho** — `smoke-strangler-proxy.ps1` 2026-11-18 |
| Piloto Azure B1 + SQL Basic + contenedor OCR | **Hecho** 2026-11-23 — `/health`+`/ready`, oficinas; imagen `ghcr.io/spec74/credito-api:basic` |
| SPA Vercel → API contenedor | **Hecho** — `vercel.json` / `.env.vercel.example` → `api-credito-ocr-…` |
| SPA Cloudflare Workers+Assets | **Hecho** — `https://credito-personal.sulcaparedescristian.workers.dev`; CORS `BrowserCors__AllowedOrigins__1` sin `/` |
| ApiPerú en App Service | **Config** — requiere `ApiPeru__Token` (sin token = mensaje “no configurada”) |
| Login SPA real + cobro/ticket + GPS (cuenta Azure anterior) | **Hecho** — [SMOKE-SIGN-OFF.md](SMOKE-SIGN-OFF.md) 2026-10-09/12 |
| Cutover preprod SSD-00 §7 | **Pendiente ops humano** |
| SSD-01 refresh; SSD-03 impagos/mora/verificar/saldos PDF | **Pendiente humo oficina** |

Pista: [GUIA-PRUEBAS-GO-LIVE.md](GUIA-PRUEBAS-GO-LIVE.md) · [AZURE-PILOTO-BASIC.md](AZURE-PILOTO-BASIC.md).

---

## 2. Archivos grandes — estado

| Archivo | Líneas ~ | Estado |
|---------|----------|--------|
| Endpoints bóveda/caja/cartera/ventas (partials) | ≤1394 | **Hecho** |
| `dashboardadminreadservice` + Red | — | **Hecho** |
| `CobroBloquePage.tsx` | ~1511 | **Mejorado** — CuotasSubgrid + Columns |
| `creditofichagestionendpoints.cs` | ~1687 | Backlog split |
| `SimuladorCreditoPage.tsx` | ~1565 | Backlog split |
| `maestroreadendpoints.cs` | ~1518 | Backlog split |

---

## 3. Producto opcional (Fase C)

- Alertas persistentes / umbrales configurables en tablero
- Mini-comparativo sede vs promedio red

---

## 4. Datos / ranking

`usp_DashboardRanking` filtra `RolId = 6` (ANALISTA). Ver [dashboard_migracion.md](dashboard_migracion.md).

---

## 5. Hecho (no reabrir)

Tablero Red, ranking/meta, docs as-built, partials endpoints, CobroBloque extract, smokes local/strangler, piloto Azure B1+SQL Basic+contenedor Tesseract+Vercel+semilla (2026-11-23).
