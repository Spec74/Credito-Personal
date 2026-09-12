# SSD-10 — Ventas

**Estado:** as-built · 2026-09-11  
**Código:** `/ventas/*`, `/reportes/venta` · `/api/v1/ventas/*`, `/api/v1/lista-precios/*`  
**Fuente legacy:** `VentaRapidaController`, `OrdenVentaController`, `CanjearPuntosController`, `ListaPrecioController`, `Reporte/Venta`  
**Doc de ingeniería:** [ui_modernizacion_modulos.md](../ui_modernizacion_modulos.md) (no hay `*_migracion.md` propio)

## 1. Propósito y actores

Venta de mostrador, órdenes, lista de precios y canje de puntos de fidelidad.

| Rol | Uso |
|-----|-----|
| Cajero / gestor | Venta rápida (exige caja diario abierta de la oficina del JWT) |
| Operación (`CreditoRolOperador`) | Canje de puntos |
| Administración | Alta y activación de lista de precios |
| Informes | Índice Reportes → Venta (rentabilidad y lista) |

**Ámbito de menú.** El snapshot de `usp_MenuLst` de la oficina 1 (producto CREDITO) **no** incluye ítems de ventas. La SPA y la API están implementadas para oficinas o perfiles que sí las tengan en menú, y para el mapa de Inicio. No forman parte del flujo diario de crédito de esa oficina.

## 2. Alcance

**Entra**

- Hub `/ventas`
- Venta rápida, orden de venta (contado / crédito), canje de puntos
- Mantenimiento e informe de lista de precios
- Índice `/reportes/venta` (export del mes en curso)
- Listado de códigos de barras (`VENTAS.usp_CodigoBarras_Lst`; la pantalla vive en almacén)

**No entra**

- Caja diario como pantalla → SSD-03 (sí se usa su sesión en el pedido)
- Alta de persona → SSD-05
- Catálogo marca/artículo/almacén → SSD-09
- Entrada, salida, kardex → SSD-11
- Impresión de **etiquetas** RDLC (`rptCodigo`) → puente MVC opcional (SSD-07)

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/VentaRapida` | `/ventas/venta-rapida` | Pedido + cobro en caja |
| `/OrdenVenta` | `/ventas/orden-venta` | Detalle por serie; envío CON / CRE |
| `/CanjearPuntos` | `/ventas/canjear-puntos` | |
| `/ListaPrecio` | `/ventas/lista-precios` | CRUD |
| `Reporte/ReporteListaPrecio` | `/ventas/informe-lista-precios` | |
| `Reporte/Venta` | `/reportes/venta` | No cae al hub `/informes` |

## 4. Contrato de datos

Paridad con el BL/controlador MVC: donde el legado usaba `usp_*`, el moderno también; donde el legado insertaba en transacción (venta rápida, envío de orden), el C# replica ese SQL, no inventa un motor nuevo.

| Procedimiento / tabla | Uso |
|-----------------------|-----|
| `VENTAS.ListaPrecio`, `VENTAS.OrdenVenta`, `OrdenVentaDet`, `OrdenVentaDetSerie` | Pedido y órdenes |
| `ALMACEN.SerieArticulo` (EstadoId 2 = EN_ALMACEN) | Stock de venta rápida |
| `CREDITO.usp_PagarCuentaxCobrar` | Cobro del pedido de mostrador |
| `VENTAS.usp_OrdenVentaDet_Ins` / `_update`, `usp_OrdenVenta_Del` | Líneas de orden |
| `dbo.usp_CanjearPuntos` | Canje |
| `VENTAS.usp_RptRentabilidadVenta`, informe lista precio | Informes |
| `VENTAS.usp_CodigoBarras_Lst` | Listado (no stickers RDLC) |

Venta rápida: IGV 18 % para desglosar subtotal/impuesto, igual que el controlador legado. Envío a crédito: inicial 15 % del neto y alta de `CREDITO.Credito` como `OrdenVentaBL.EnviarOrdenVentaCredito` (no pasa por el simulador SSD-02).

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `GET caja-diario-venta-rapida`, `GET articulo-venta-rapida` | `CreditoUser` | `/ventas/venta-rapida` |
| `POST realizar-pedido` | `CreditoUser` (oficina y caja del JWT) | Venta rápida |
| `GET ordenes-venta`, `GET orden-venta/{id}` | `CreditoUser` | `/ventas/orden-venta` |
| `POST crear-orden-venta`, detalle, eliminar | `CreditoUser` | Orden |
| `POST enviar-orden-venta-contado` / `-credito` | `CreditoUser` | Orden |
| `GET tarjeta-puntos`, `GET articulos-canjear` | `CreditoUser` | Canje |
| `POST canjear-puntos` | `CreditoRolOperador` | Canje |
| `GET/POST /api/v1/lista-precios/*` | User / admin al guardar | `/ventas/lista-precios` |
| `GET rpt-lista-precio*`, `GET rpt-rentabilidad-venta*` | `CreditoUser` | Informe e índice |

## 6. Seguridad

- Hub `/ventas` habilita `/ventas/*`.
- Pedido: `oficinaId` y `cajaDiarioId` validados contra el JWT (misma guarda que caja).
- Canje: operador; `REPORTEPARCIAL` recibe 403.
- Reportes → VENTA no abre el hub de ventas genérico.

## 7. Criterios de aceptación

- [ ] Con caja diario abierta, venta rápida busca artículo, arma el carrito y `realizar-pedido` deja orden `ENV`/`CON` y movimiento de caja vía `usp_PagarCuentaxCobrar`.
- [ ] Sin caja abierta, la pantalla informa el mismo bloqueo que el MVC.
- [ ] Stock insuficiente (series EN_ALMACEN) no confirma el pedido.
- [ ] Orden: agregar/quitar detalle por serie; enviar contado o crédito replica el BL.
- [ ] Canje llama `usp_CanjearPuntos`; rol no operador no escribe.
- [ ] Oficina 1 sin ítem VENTAS: el menú CREDITO no abre estas rutas; el mapa de Inicio puede mostrar el enlace (filtrar por menú en cutover si negocio no quiere el atajo).

## 8. Desviaciones

- 2026-09-09 — esquema `ALMACEN` (lista de precios / artículos) ([BITACORA](../migration/BITACORA-DESVIACIONES.md))

Etiquetas de código de barras RDLC: no hay motor de stickers; el PDF moderno es tabular. PDF Credix ≠ píxel ReportViewer.

## 9. Pruebas y evidencia

- API: `ventasalmacenendpointtests`, `canjearpuntosendpointstests`, `listaprecioendpointtests`, `rptlistapreciogeneralendpointtests`, `rptrentabilidadventaendpointtests`, `codigobarraslstendpointtests`, `pagarcuentaxcobrarendpointtests`, `creditorolauthorizationendpointtests` (canje)
- SPA: `resolvespapathfrommenuitem.test.ts` (Venta rápida, índice VENTA REPORTES)
- Smoke: oficina **con** menú ventas — pedido de una serie; oficina 1 CREDITO — confirmar que el menú vivo no las lista

## 10. Go-live

Código listo en Development. **No es rebanada del menú vivo CREDITO (oficina 1).** Activar en cutover solo si esa oficina opera mostrador; si no, no hace falta piloto de ventas para el go-live de crédito/caja.
