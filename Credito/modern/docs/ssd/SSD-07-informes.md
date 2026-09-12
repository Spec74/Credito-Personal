# SSD-07 — Informes y reportes

**Estado:** as-built · 2026-09-11  
**Código:** `/informes/*`, `/reportes/*` · `/api/v1/reportes/*` + `rpt-*` de crédito/caja/ventas/almacén  
**Fuente legacy:** `ReporteController`, índices `Reporte/Credito`, `CobranzaPagos`, `Almacen`, `Venta`  
**Doc de ingeniería:** [CATALOGO-INFORMES-COBERTURA.md](../migration/CATALOGO-INFORMES-COBERTURA.md)

## 1. Propósito y actores

Consultar cartera, cobranza, caja, stock y ventas con la misma data que el MVC, exportando CSV/PDF tabulares Credix.

| Rol | Uso |
|-----|-----|
| Gestor | Cobro diario, morosidad, vencidos de su cartera (oficina JWT) |
| Aprobador / admin / `REPORTEPARCIAL` | Índice Reportes → Crédito (`canViewReporteCredito`) |
| Encargado | Caja diario, cajas asignadas, movimientos anulados |
| Analista | Informes de crédito si el menú lo concede |

## 2. Alcance

**Entra**

- Hub `/informes` y matriz `/informes/cobertura`
- Índices `/reportes/credito`, `/reportes/cobranza`, `/reportes/almacen`, `/reportes/venta`
- Pantallas `/informes/*` con JSON + CSV + PDF (mismos `usp_*` que el RDLC)
- Visor JWT `/reportes/visor?path=` (blob de la API, no ReportViewer)
- Central de riesgo (CSV/PDF/TXT)

**No entra**

- Impresos de un crédito abierto (plan, estado, movimiento, ficha) → SSD-02
- Informe movimiento bóveda como pantalla tesorería → SSD-04
- Layout píxel a píxel `.rdlc`
- CRUD de comisiones / usuarios → SSD-08

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `Reporte/Credito` | `/reportes/credito` | Menú CREDITO módulo REPORTES |
| `Reporte/CobranzaPagos` | `/reportes/cobranza` | Excel `.xlsx` coloreado |
| `Reporte/Almacen` | `/reportes/almacen` | Stock / anulados / kardex |
| `Reporte/Venta` | `/reportes/venta` | Lista precios + rentabilidad |
| `Dashboard/Admin` | `/inicio` | SSD-01, no Informes |
| Padre REPORTES | — | No se mapea a `/informes` |

Las pantallas de informe **no** llaman `openLegacy*` (helpers quedan por si negocio exige RDLC). Export por defecto: API JWT.

## 4. Contrato de datos

El C# no recalcula cartera ni mora. Cada informe invoca su `usp_Rpt*` / equivalente del `ReporteController`.

| Pieza | Uso |
|-------|-----|
| Catálogo MVC (52) | `GET /api/v1/reportes/catalogo` |
| Matriz cobertura | `GET .../catalogo-cobertura` — 43 `completo-datos`, `soloMvc = 0`, ≥9 `parcial`, 5 adicionales API, 3 `json-texto` |
| `CredixLegacyReportCatalog` | Título y columnas PDF (tildes normalizadas) |
| Tres JSON `{ texto }` | Resumen ingreso caja, tipo cuenta, cuenta bóveda (sin CSV/PDF de filas) |

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `GET /api/v1/reportes/catalogo`, `politica-exportacion`, `catalogo-cobertura` | `CreditoUser` | `/informes/cobertura` |
| `GET /api/v1/credito/rpt-*` (JSON/CSV/PDF) | `CreditoUser` (oficina JWT) | `/informes/*`, índice crédito |
| `GET /api/v1/ventas/rpt-*` | `CreditoUser` | Índice venta |
| `GET cobranza-pagos` Excel | `CreditoUser` | `/reportes/cobranza` |

Política de export: fase `pdf-tabular-completo` (cada `-csv` tiene `-pdf`). Motor RDLC idéntico = `layout-rdlc-fase4-opcional`.

## 6. Seguridad

- Hub `/reportes/credito` habilita `/informes/*` (`HUB_CHILDREN`).
- Índice crédito además filtra roles (`reporteCreditoAccess.ts`).
- Padre REPORTES no concede el hub.
- PDF sin Bearer → 401 (`reportpdfendpointsunauthorizedtests`).

## 7. Criterios de aceptación

- [ ] Ítem Reportes → Crédito abre `/reportes/credito`; Dashboard → `/inicio`.
- [ ] Un informe de cartera (p. ej. cobro diario) lista filas del `usp_*` y descarga PDF Credix con título legible.
- [ ] `/informes/cobertura` muestra 52 del catálogo y `soloMvc = 0`.
- [ ] Gestor sin rol de índice crédito no ve cajas de admin/aprobador que el MVC ocultaba.
- [ ] `/reportes/visor` abre el PDF de la API, no IIS ReportViewer.
- [ ] Tres resúmenes `{ texto }` no ofrecen CSV/PDF de grilla.

## 8. Desviaciones

- 2026-09-10 — PDFs tabulares: catálogo por título (tildes) y layout usable ([BITACORA](../migration/BITACORA-DESVIACIONES.md))

Producto: PDF Credix ≠ píxel RDLC. `VITE_LEGACY_ORIGIN` solo si negocio lo pide.

## 9. Pruebas y evidencia

- API: `reportescatalogocoberturaendpointtests` (52 / 43 / soloMvc 0), `reportesexportpoliticaendpointtests`, `credixlegacyreportcatalogtests`, `rpt*` endpoint/CSV/PDF por informe
- SPA: `resolvespapathfrommenuitem.test.ts` (CREDITO/COBRANZA/VENTA REPORTES)
- Smoke: índice crédito → PDF; cobertura; un informe de caja y uno de almacén

## 10. Go-live

Listo en Development para la operación diaria. Cutover: comparar un PDF Credix vs RDLC con negocio; retirar `VITE_LEGACY_ORIGIN` cuando firmen el tabular.
