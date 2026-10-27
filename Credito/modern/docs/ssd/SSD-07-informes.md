# SSD-07 — Informes y reportes

**Estado:** as-built · 2026-09-11 · cobertura informes 2026-09-27 (52/46) · Morosos empresariales 2026-10-07 · **alcance por sede 2026-10-24** · ver [SSD-12](SSD-12-validacion.md)  
**Código:** `/informes/*`, `/reportes/*` · `/api/v1/reportes/*` + `rpt-*` + `/api/v1/morosidad/*` + `/api/v1/cierre-gerencial/*`  
**Fuente legacy:** `ReporteController`, índices `Reporte/Credito`, `CobranzaPagos`, `Almacen`, `Venta`, `MorosidadController`, `CierreGerencialController`  
**Doc de ingeniería:** [CATALOGO-INFORMES-COBERTURA.md](../migration/CATALOGO-INFORMES-COBERTURA.md), [PARIDAD-MOROSIDAD.md](../migration/PARIDAD-MOROSIDAD.md)

## 1. Propósito y actores

Consultar cartera, cobranza, caja, stock y ventas con la misma data que el MVC, exportando CSV/PDF tabulares Credix. Además, el listado empresarial de morosos (ACL por usuario, no por menú).

| Rol | Uso |
|-----|-----|
| Gestor | Cobro diario, morosidad, vencidos de su cartera (oficina JWT) |
| Aprobador / admin / `REPORTEPARCIAL` | Índice Reportes → Crédito (`canViewReporteCredito`) |
| Encargado | Caja diario, cajas asignadas, movimientos anulados |
| Analista | Informes de crédito si el menú lo concede |
| Usuarios en `Morosidad:UsuarioConsultaIds` | `/informes/morosos` — sucursal ve su sede; Central la red o una sede |
| Usuarios en `CierreGerencial:UsuarioConsultaIds` | `/informes/cierre-gerencial` — mismo alcance por sede |
| Usuarios en `CierreGerencial:UsuarioMetasIds` | Editar metas definitivas (solo analistas de su sede si no es Central) |

## 2. Alcance

**Entra**

- Hub `/informes` y matriz `/informes/cobertura`
- Índices `/reportes/credito`, `/reportes/cobranza`, `/reportes/almacen`, `/reportes/venta` (tarjetas siguen tokens SSD-19: pie y PDF/XLS no quedan blancos en oscuro)
- Pantallas `/informes/*` con JSON + CSV + PDF (mismos `usp_*` que el RDLC)
- Visor JWT `/reportes/visor?path=` (blob de la API, no ReportViewer)
- Central de riesgo (CSV/PDF/TXT)
- **Morosos empresariales** `/informes/morosos` → `CREDITO.usp_MorosidadEmpresa` + Excel ClosedXML
- **Cierre y metas gerenciales** `/informes/cierre-gerencial` → `usp_ObtenerAvanceMetasGerenciales` / `usp_ListarMetasGerencialesDefinitivas` / `usp_GuardarMetaGerencialDefinitiva` + Excel ClosedXML

**No entra**

- Impresos de un crédito abierto (plan, estado, movimiento, ficha) → SSD-02
- Informe movimiento bóveda como pantalla tesorería → SSD-04
- Layout píxel a píxel `.rdlc`
- CRUD de comisiones / usuarios → SSD-08
- Snapshot oficial de fin de mes (`usp_GenerarCierreGerencialMensual`) — lo dispara el cierre de bóveda, no esta pantalla

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `Reporte/Credito` | `/reportes/credito` | Menú CREDITO módulo REPORTES |
| `Reporte/CobranzaPagos` | `/reportes/cobranza` | Excel `.xlsx` coloreado |
| `Reporte/Almacen` | `/reportes/almacen` | Stock / anulados / kardex |
| `Reporte/Venta` | `/reportes/venta` | Lista precios + rentabilidad |
| `Morosidad` / `MorosidadController` | `/informes/morosos` | ACL `UsuarioConsultaIds`, no menú `MAESTRO` |
| `CierreGerencial` | `/informes/cierre-gerencial` | ACL `UsuarioConsultaIds` / `UsuarioMetasIds`, no menú `MAESTRO` |
| `Dashboard/Admin` | `/inicio` | SSD-01, no Informes |
| Padre REPORTES | — | No se mapea a `/informes` |

Las pantallas de informe **no** llaman `openLegacy*` (helpers quedan por si negocio exige RDLC). Export por defecto: API JWT.

## 4. Contrato de datos

El C# no recalcula cartera ni mora de los `rpt-*`. Cada informe invoca su `usp_Rpt*` / equivalente del `ReporteController`.

| Pieza | Uso |
|-------|-----|
| Catálogo MVC (52) | `GET /api/v1/reportes/catalogo` |
| Matriz cobertura | `GET .../catalogo-cobertura` — 46 `completo-datos`, `soloMvc = 0`, ≥6 `parcial`, 5 adicionales API, 3 `json-texto` |
| `CredixLegacyReportCatalog` | Título y columnas PDF (tildes normalizadas) |
| Tres JSON `{ texto }` | Resumen ingreso caja, tipo cuenta, cuenta bóveda (sin CSV/PDF de filas) |
| `CREDITO.usp_MorosidadEmpresa` | Morosos empresa; `@OficinaId` opcional. Sucursal: la API fuerza JWT. Central: null = red o la sede elegida. Script `deploy/sql/2026-10-07-usp-morosidad-empresa.sql` |
| `CREDITO.usp_ObtenerAvanceMetasGerenciales` | Avance por `@Periodo` + `@OficinaId`. Sucursal: una llamada. Central: una por sede activa (o `sedeId`) y el C# sella `Oficina` |
| `CREDITO.usp_ListarMetasGerencialesDefinitivas` | 19 carteras de la empresa. Sucursal / sede: se filtra por `UsuarioOficina` en C# |
| `CREDITO.usp_GuardarMetaGerencialDefinitiva` | Upsert por analista/periodo. Sucursal no puede guardar `UsuarioId` de otra sede |

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `GET /api/v1/reportes/catalogo`, `politica-exportacion`, `catalogo-cobertura` | `CreditoUser` | `/informes/cobertura` |
| `GET /api/v1/credito/rpt-*` (JSON/CSV/PDF) | `CreditoUser` (oficina JWT) | `/informes/*`, índice crédito |
| `GET /api/v1/ventas/rpt-*` | `CreditoUser` | Índice venta |
| `GET cobranza-pagos` Excel | `CreditoUser` | `/reportes/cobranza` |
| `GET /api/v1/morosidad/permisos` | `CreditoUser` | Menú / hub (ACL) |
| `GET /api/v1/morosidad/empresa` | `CreditoUser` + ACL Morosidad | `/informes/morosos` |
| `GET /api/v1/morosidad/excel` | `CreditoUser` + ACL Morosidad | Export XLSX (mismo `@OficinaId` efectivo) |
| `GET /api/v1/cierre-gerencial/permisos` | `CreditoUser` | Menú / hub (ACL) |
| `GET /api/v1/cierre-gerencial/avance` | `CreditoUser` + ACL consulta | `/informes/cierre-gerencial` (`sedeId` solo honrado en Central) |
| `GET /api/v1/cierre-gerencial/metas` | `CreditoUser` + ACL consulta | Metas definitivas |
| `POST /api/v1/cierre-gerencial/metas` | `CreditoUser` + ACL metas | Guardar (acotado a sede si no es Central) |
| `GET /api/v1/cierre-gerencial/excel` | `CreditoUser` + ACL consulta | Export XLSX del avance visible |

Política de export (catálogo): fase `pdf-tabular-completo` (cada `-csv` tiene `-pdf`). Motor RDLC idéntico = `layout-rdlc-fase4-opcional`.

## 6. Seguridad

- Hub `/reportes/credito` habilita `/informes/*` (`HUB_CHILDREN`), **excepto** rutas exactas con ACL extra.
- `/informes/morosos` y `/informes/cierre-gerencial` están en `EXACT_MENU_ROUTES`: el hub de reportes **no** las abre; hace falta `extraAllowedPaths` tras los `GET .../permisos`.
- Alcance de datos (2026-10-24): `IndPrincipal` = Central. Sucursal ignora `oficinaId`/`sedeId` del query y usa el JWT. Central honra el filtro o ve la red. No se ensancha RoleHome ni las listas ACL.
- Cobranza / cobro diario / morosidad gestor (004): el rol ADMIN/APROBADOR/REPORTEPARCIAL **en sucursal** ya no abre otra oficina ni “todas”. Solo Central conserva el combo. Spec Kit [004-alcance-red](../speckit/004-alcance-red/spec.md).
- Índice crédito además filtra roles (`reporteCreditoAccess.ts`).
- Padre REPORTES no concede el hub.
- PDF sin Bearer → 401 (`reportpdfendpointsunauthorizedtests`).

## 7. Criterios de aceptación

> **Documentación:** criterios redactados al 100%. Las casillas registran **evidencia de ejecución** (Development / preprod), no huecos de spec.

- [ ] Ítem Reportes → Crédito abre `/reportes/credito`; Dashboard → `/inicio`.
- [ ] Un informe de cartera (p. ej. cobro diario) lista filas del `usp_*` y descarga PDF Credix con título legible.
- [ ] `/informes/cobertura` muestra 52 del catálogo, 46 completo-datos y `soloMvc = 0`.
- [ ] Gestor sin rol de índice crédito no ve cajas de admin/aprobador que el MVC ocultaba.
- [ ] `/reportes/visor` abre el PDF de la API, no IIS ReportViewer.
- [ ] Tres resúmenes `{ texto }` no ofrecen CSV/PDF de grilla.
- [ ] Usuario en `Morosidad:UsuarioConsultaIds` ve Morosos en menú/hub; otro usuario con menú de informes, no.
- [ ] `/informes/morosos` lista filas de `usp_MorosidadEmpresa` y exporta Excel del rango.
- [ ] Sucursal en Morosos / Cierre solo ve su sede; Central ve la red y puede acotar.
- [ ] Sucursal no guarda metas de analistas que no están en su `UsuarioOficina`.

## 8. Desviaciones

- 2026-09-10 — PDFs tabulares: catálogo por título (tildes) y layout usable ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- 2026-10-07 — Morosos empresariales: módulo ACL + SP (ver [PARIDAD-MOROSIDAD](../migration/PARIDAD-MOROSIDAD.md))
- **2026-10-24 — alcance por sede:** Morosos y Cierre gerencial dejan de pedir toda la red desde sucursal. No se inventa USP ni se toca el cierre mensual oficial ([BITACORA](../migration/BITACORA-DESVIACIONES.md)).
- **2026-10-24 — 004 cobranza:** sucursal con rol de reporte ya no consulta la red. Los ~28 informes con oficina oculta siguen siendo JWT (Central entra a la sede).

Producto: PDF Credix ≠ píxel RDLC. `VITE_LEGACY_ORIGIN` solo si negocio lo pide.

## 9. Pruebas y evidencia

- API: `reportescatalogocoberturaendpointtests` (52 / **46** completo-datos / soloMvc 0), `reportesexportpoliticaendpointtests`, `credixlegacyreportcatalogtests`, `rpt*` endpoint/CSV/PDF por informe, `morosidadendpointtests`
- SPA: `resolvespapathfrommenuitem.test.ts` (CREDITO/COBRANZA/VENTA REPORTES), `menuRouteAccess.test.ts` (morosos solo con `extraAllowedPaths`)
- Smoke: índice crédito → PDF; cobertura; un informe de caja y uno de almacén; Morosos con usuario ACL

## 10. Go-live

Listo en Development para la operación diaria. Cutover: comparar un PDF Credix vs RDLC con negocio; retirar `VITE_LEGACY_ORIGIN` cuando firmen el tabular. En Azure con bak 2026-09-22, confirmar que existe `CREDITO.usp_MorosidadEmpresa` (ya venía en el bak).
