# SSD-11 — Almacén (movimientos e inventario)

**Estado:** as-built · 2026-09-11  
**Código:** `/almacen/*`, `/reportes/almacen` · `/api/v1/almacen/*`  
**Fuente legacy:** `EntradaController`, `SalidaController`, `TransferenciaController`, `Movimiento` (almacén), `ReporteKardex`, `ConstanciaAlmacen`, `Reporte/Almacen`  
**Doc de ingeniería:** [ui_modernizacion_modulos.md](../ui_modernizacion_modulos.md)

## 1. Propósito y actores

Ingresar, sacar y transferir mercadería por serie; consultar kardex, stock y constancia.

| Rol | Uso |
|-----|-----|
| Operación de almacén | Entrada (asistente), salida por series, transferencia, movimiento por ID |
| Informes | Stock, stock anulados, kardex, códigos de barras |
| Maestros | Catálogo de almacenes (SSD-09), no esta rebanada |

**Ámbito de menú.** Igual que ventas: el menú vivo de oficina 1 (CREDITO) **no** lista operaciones de almacén. El índice Reportes → Almacén sí puede existir en REPORTES. La SPA cubre el módulo completo para quien tenga menú ALMACEN o el hub `/almacen`.

## 2. Alcance

**Entra**

- Hub `/almacen`
- Entrada, salida, transferencia, movimiento avanzado por ID
- Kardex, constancia PDF/CSV, códigos de barras (API de ventas)
- Informes stock / stock anulados (también en SSD-07)

**No entra**

- CRUD de almacenes, marcas, artículos → SSD-09
- Lista de precios y venta rápida → SSD-10
- Stickers RDLC de código de barras → puente MVC (catálogo: `reporte-cod-barras`)

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/Entrada` | `/almacen/entrada` | Asistente (crear movimiento, documentos, detalle, confirmar) |
| `/Salida` | `/almacen/salida` | Series; estado movimiento 3 |
| `/Transferencia` | `/almacen/transferencia` | Confirmar / desconfirmar |
| Movimiento por ID | `/almacen/movimiento` | Avanzado |
| `ReporteKardex` / `GenerarKardex` | `/almacen/kardex` | `usp_GenerarKardex` |
| `ConstanciaAlmacen` | `/almacen/constancia` | |
| `Reporte/Almacen` | `/reportes/almacen` | Stock y anulados |
| `/Almacen` (maestro) | `/maestros/almacenes` | SSD-09 |

## 4. Contrato de datos

Esquema **`ALMACEN`** (no `MAESTRO`) para `Articulo`, `Marca`, `Modelo`, `TipoArticulo`, `TipoMovimiento`, series y movimientos. Corrección 2026-09-09 en bitácora.

| Procedimiento / pieza | Uso |
|-----------------------|-----|
| `ALMACEN.usp_CrearMovimientoDet`, `usp_EliminarMovimientoDet`, `usp_Movimiento_Upd` | Detalle / cabecera |
| Entrada / confirmar / desconfirmar | Paridad `EntradaController` / `MovimientoBL` (SQL de transacción + SP de detalle) |
| Salida | Paridad `SalidaController.RealizarSalida` (series fuera de EN_ALMACEN) |
| Transferencia | Paridad controlador: crear, validar serie, confirmar |
| `ALMACEN.usp_GenerarKardex`, `usp_ListarSerieKardex`, `usp_ExisteSerieArticulo` | Kardex |
| `ALMACEN.usp_ReporteStock` | Informe stock |
| Stock anulados | `ReporteBL.ListarReporteStockAnulados` (mismo origen que el MVC) |

El C# no recalcula costo de inventario: mueve series y llama los mismos procedimientos o el mismo SQL que el BL.

## 5. API y SPA

Prefijo `/api/v1/almacen`. Política `CreditoUser`. `oficinaId` del JWT en escrituras.

| Método y ruta | Pantalla |
|---------------|----------|
| `GET movimientos-entrada`, `GET movimiento-entrada/{id}` | Entrada |
| `POST crear-movimiento`, documentos, importe | Entrada |
| `POST crear-movimiento-detalle`, `eliminar-movimiento-detalle` | Entrada / movimiento |
| `POST confirmar-movimiento`, `desconfirmar-movimiento`, `actualizar-movimiento` | Entrada / movimiento |
| `GET buscar-serie-salida`, `POST realizar-salida` | Salida |
| `GET transferencias`, `POST crear-transferencia`, validar/eliminar serie, confirmar/desconfirmar | Transferencia |
| `GET generar-kardex*`, `GET serie-kardex`, `GET existe-serie-articulo` | Kardex |
| `GET reporte-stock*`, `GET rpt-stock-anulados*` | Índice e `/informes/*` |
| `GET rpt-constancia-almacen*` | Constancia |

Códigos de barras: `GET /api/v1/ventas/codigo-barras-lst*` desde `/almacen/codigo-barras`.

## 6. Seguridad

- Hub `/almacen` habilita `/almacen/*`.
- Índice `/reportes/almacen` también habilita `/almacen/` (informes de stock desde REPORTES).
- Ítem maestro «Almacenes» no abre kardex ni entrada.
- Confirmaciones acotadas a la oficina del movimiento.

## 7. Criterios de aceptación

- [ ] Entrada: crear movimiento de la oficina sesión, cargar detalle/series, confirmar; el kardex del artículo refleja el ingreso.
- [ ] Salida con serie no disponible (no EN_ALMACEN) no confirma.
- [ ] Transferencia origen/destino: confirmar deja series en el almacén destino; desconfirmar revierte como el MVC.
- [ ] Kardex y stock PDF/CSV salen por API JWT (`usp_GenerarKardex` / `usp_ReporteStock`).
- [ ] Constancia de un `movimientoId` de la oficina se descarga en PDF.
- [ ] Oficina 1 CREDITO: menú vivo no exige estas pantallas para el go-live de crédito.

## 8. Desviaciones

- 2026-09-09 — esquema `ALMACEN` en catálogos (marcas, artículos, tipos de movimiento, stock anulados, salidas y transferencias) ([BITACORA](../migration/BITACORA-DESVIACIONES.md))

PDF tabular ≠ RDLC. Etiquetas de barras: listado moderno; stickers solo MVC si negocio los exige.

## 9. Pruebas y evidencia

- API: `ventasalmacenendpointtests`, `transferenciaendpointtests`, `generarkardexendpointtests`, `seriekardexendpointtests`, `existeseriearticuloendpointtests`, `reportestockendpointtests`, `rptstockanuladosendpointtests`, `rptconstanciaalmacenendpointtests`, `almacenendpointtests`, `tipomovimientoalmacenendpointtests`, `esquemacontratotests` (nombres de tabla)
- SPA: resolución de entrada/salida/kardex en `resolvespapathfrommenuitem.ts`
- Smoke: solo en oficina que opere inventario; no bloquear cutover CREDITO si el menú no las muestra

## 10. Go-live

Código listo en Development. **Fuera del menú vivo oficina 1.** Incluir en el piloto de cutover únicamente si esa sucursal mueve stock; en caso contrario el go-live de crédito/caja no depende de esta rebanada.
