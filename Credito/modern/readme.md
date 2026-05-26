# Credito.Modern

**Migración strangler:** **TERMINADA en repositorio** (API + SPA + deploy). Al subir servidores: [docs/migration/DEPLOY-AL-SUBIR.md](../docs/migration/DEPLOY-AL-SUBIR.md).

Arquitectura: **Domain → Application → Infrastructure → Api** + **Credito.Modern.Web** (SPA).

Arquitectura: **Domain → Application → Infrastructure → Api**.

API ASP.NET Core **.NET 10** (TFM `net10.0`) en paralelo al legado (`Web` MVC). Se eligió `net10.0` para que **restauración, compilación y pruebas** funcionen con el runtime ya instalado en el equipo de desarrollo (solo ASP.NET Core 10). Para alinear con la hoja de ruta **.NET 8 LTS**, cambia `<TargetFramework>` a `net8.0` en todos los `.csproj` de esta carpeta e instala el [runtime/hosting bundle 8](https://dotnet.microsoft.com/download/dotnet/8.0); el workflow de GitHub puede usar `8.0.x` en lugar de `10.0.x`.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (o 8 si retargeteas a `net8.0`).
- Node.js 20+ para la SPA (`Credito.Modern.Web`).

## Build SPA (cutover `/app/`)

Desde la carpeta **`modern`** (no hace falta entrar a `Credito.Modern.Web`):

```powershell
cd Credito\modern
.\deploy\scripts\build-spa.ps1
# o bien:
npm run build
```

El `package.json` de la raíz `modern` delega en `Credito.Modern.Web`. Para desarrollo: `npm run dev:spa`.

## Ejecutar en local

```powershell
cd Credito\modern
dotnet restore Credito.Modern.sln
dotnet run --project Credito.Modern.Api
```

- Swagger (solo Development): `https://localhost:7288/swagger`
- **Health:** `GET /health` — comprobación `self` y, si hay `CreditoDatabase:ConnectionString`, chequeo SQL opcional `database` (AspNetCore.HealthChecks.SqlServer).
- **Hora SQL (solo lectura):** `GET /api/v1/database-time` — `CREDITO.usp_FechaBD` (Dapper).
- **Login (JWT):** `POST /api/v1/auth/login` con JSON `{ "nombreUsuario", "clave", "oficinaId", "clienteAcceso?" }` — misma regla que el MVC (`Usuario` + `UsuarioOficina` + opcional `MAESTRO.Acceso` vía `Auth:RequerirClienteAcceso`). **`MAESTRO.Usuario.ClaveUsuario`** puede estar en **claro** (legado) o en hash **PBKDF2** con prefijo **`$pbk2$`** (ver `UsuarioPasswordHasher`); la verificación ya no compara la clave en SQL. Respuesta `accessToken`, `expiresInSeconds`, `refreshToken`, `refreshExpiresInSeconds`, ids. El access JWT incluye claims `vendix:usuario_id`, `vendix:oficina_id`, `vendix:usuario_oficina_id` y roles MAESTRO como `ClaimTypes.Role`. **Refresh:** `POST /api/v1/auth/refresh` con `{ "refreshToken" }`; audiencia `Jwt:RefreshAudience`; claim `vendix:refresh_ver` = `Jwt:RefreshTokenVersion`. Rate limit: `auth-login` y `auth-refresh` (`RateLimiting:*`).
- **Quién soy (JWT):** `GET /api/v1/auth/me` con Bearer — devuelve `usuarioId`, `oficinaId`, `usuarioOficinaId` y lista `roles` del token.
- **Login (JWT):** `POST /api/v1/auth/login` con JSON `{ "nombreUsuario", "clave", "oficinaId", "clienteAcceso?" }` — misma regla que el MVC (`Usuario` + `UsuarioOficina` + opcional `MAESTRO.Acceso` vía `Auth:RequerirClienteAcceso`). Respuesta `accessToken`, `expiresInSeconds`, ids. El JWT incluye claims `vendix:usuario_id`, `vendix:oficina_id` y `vendix:usuario_oficina_id`. `GET /api/v1/menu?oficinaId=1&usuarioId=2` — `MAESTRO.usp_MenuLst` solo si `Menu:PermiteParametrosQuery=true` (por defecto **false** en `appsettings.json` / producción). También puedes enviar **`Authorization: Bearer <access jwt>`** con claims `vendix:oficina_id` y `vendix:usuario_id`. Si envías Bearer inválido sin query, la API responde **401**. En **Development**, sin `CI=true` y con **`Hosting:AllowDevToken=true`**, **`POST /api/v1/dev/token`** con body JSON `{ "usuarioId": 1, "oficinaId": 1 }` devuelve un JWT de prueba (sin roles; no usar en producción).
- **Oficinas activas (solo lectura, Fase 2):** `GET /api/v1/oficinas` — consulta `MAESTRO.Oficina` donde `Estado = 1` (equivalente a `OficinaBL.Listar(x => x.Estado)`).
- **Tipos de documento (solo lectura, Fase 2):** `GET /api/v1/tipos-documento` — activos (`Estado = 1`). Query opcional **`paraVenta=true`** filtra `IndVenta = 1` (como en ventas del legado).
- **Tipos de documento (solo lectura, Fase 2):** `GET /api/v1/tipos-documento` — activos (`Estado = 1`). Query opcional **`paraVenta=true`** filtra `IndVenta = 1` (como en ventas del legado).
- **Productos de crédito (solo lectura):** `GET /api/v1/productos` — `CREDITO.Producto` con `Estado = 1`.
- **Marcas (solo lectura, maestro):** `GET /api/v1/marcas` — `MAESTRO.Marca` con `Estado = 1` (strangler: catálogo para inventario / ventas).
- **Modelos (solo lectura, maestro):** `GET /api/v1/modelos` — `MAESTRO.Modelo` activos; query opcional **`marcaId`** (≥ 1) filtra por marca.
- **Tipos de artículo (solo lectura, maestro):** `GET /api/v1/tipos-articulo` — `MAESTRO.TipoArticulo` con `Estado = 1`.
- **Ubigeo (solo lectura, maestro):** `GET /api/v1/departamentos`; `GET /api/v1/provincias` con query opcional **`departamentoId`** (≥ 1); `GET /api/v1/distritos` con query opcional **`provinciaId`** (≥ 1).
- **Tipos de movimiento de almacén (solo lectura):** `GET /api/v1/tipos-movimiento-almacen` — `MAESTRO.TipoMovimiento` con `Estado = 1` (no confundir con `GET /api/v1/tipo-operaciones`, que es crédito).
- **Almacenes (solo lectura):** `GET /api/v1/almacenes` — **`ALMACEN.Almacen`** activos (`Estado = 1`); query opcional **`oficinaId`** (≥ 1), como entradas/salidas en MVC.
- **Ocupaciones / actividad económica (solo lectura):** `GET /api/v1/ocupaciones` — `MAESTRO.Ocupacion` con `Estado = 1` (combos de cliente).
- **Artículos (solo lectura, inventario):** `GET /api/v1/articulos` — **`ALMACEN.Articulo`** activos; queries opcionales **`modeloId`** y **`tipoArticuloId`** (≥ 1). La lista no incluye la columna `Imagen` (varchar(max)).
- **Lista de precios (solo lectura, ventas):** `GET /api/v1/lista-precios` — **`VENTAS.ListaPrecio`** activos (`Estado = 1`); query opcional **`articuloId`** (≥ 1).
- **Valores de tabla / combos (solo lectura):** `GET /api/v1/valores-tabla?tablaId=` — **`MAESTRO.ValorTabla`**; **`tablaId` obligatorio** (≥ 1). Query opcional **`soloItemIdPositivo`** (por defecto true: solo `ItemId > 0`, como en MVC).
- **Series en almacén (solo lectura):** `GET /api/v1/series-articulo` — **`ALMACEN.SerieArticulo`**; **`almacenId`** y **`articuloId`** obligatorios; **`estadoId`** opcional (default **2** = en almacén); **`limite`** 1–500 (default 200).
- **Códigos de barras por movimiento (solo lectura):** `GET /api/v1/ventas/codigo-barras-lst?movimientoId=` — **`VENTAS.usp_CodigoBarras_Lst`** (`pMovimientoId` obligatorio ≥ 1).
- **Rentabilidad de ventas (JWT, política `CreditoUser`):** `GET /api/v1/ventas/rpt-rentabilidad-venta?fechaIni=&fechaFin=&oficinaId=` — **`VENTAS.usp_RptRentabilidadVenta`**; `fechaIni` y `fechaFin` obligatorias (año 1900–2100; `fechaIni` ≤ `fechaFin`); **`oficinaId`** obligatorio y = **`vendix:oficina_id`**; **`indContado`** y **`indCredito`** opcionales (default `false`). Origen: `ReporteBL.ListarReporteRentabilidadVenta` (sin modo “todas las oficinas” desde la API).
- **Rentabilidad de ventas CSV (JWT, política `CreditoUser`):** `GET /api/v1/ventas/rpt-rentabilidad-venta-csv?fechaIni=&fechaFin=&oficinaId=` — mismos parámetros y datos que **`rpt-rentabilidad-venta`**; respuesta **`text/csv; charset=utf-8`**, descarga **`rentabilidad-venta.csv`** (UTF-8 con BOM).
- **Rentabilidad de ventas CSV (JWT, política `CreditoUser`):** `GET /api/v1/ventas/rpt-rentabilidad-venta-csv?fechaIni=&fechaFin=&oficinaId=` — mismos parámetros y datos que **`rpt-rentabilidad-venta`**; respuesta **`text/csv; charset=utf-8`**, descarga **`rentabilidad-venta.csv`** (UTF-8 con BOM).
- **Despliegue y auth MVC:** guía [docs/migration/PROXY-AND-MVC-AUTH-BACKLOG.md](../docs/migration/PROXY-AND-MVC-AUTH-BACKLOG.md) (proxy `/api/v1` + backlog de controladores sin `[Autenticado]`).
- **POST cálculo TEM (JWT, política `CreditoUser`):** `POST /api/v1/credito/calcular-tem` — `CREDITO.usp_CalcularTEM`; `formaPago` = **D/M/Q/S** (como el MVC), no el texto "MENSUAL". Otras rutas pueden usar políticas por rol (`CreditoRolAdministrador`, `CreditoRolEncargado`, `CreditoRolAprobador1`) alineadas a `MAESTRO.Rol.Denominacion`.
- **POST simulador de crédito (JWT, política `CreditoUser`):** `POST /api/v1/credito/simulador-credito` — **`CREDITO.usp_SimuladorCredito`**; cuerpo JSON alineado a **`CreditoBL.SimuladorCredito`** (`monto`, `formaPago` D/M/Q/S, `nroCuotas`, `interesMensual`, `fechaPrimerPago`, `gastosAdm` opcional). Si **`monto` <= 0**, devuelve lista vacía sin llamar al proc (como el MVC). Respuesta: lista de cuotas (`numero`, `capital`, `fechaPago`, `amortizacion`, `interes`, `gastosAdm`, `cuota`).
- **Estado del plan de pagos (JWT, política `CreditoUser`):** `GET /api/v1/credito/estado-plan-pago?creditoId=` — **`CREDITO.usp_EstadoPlanPago`**; `creditoId` obligatorio (≥ 1).
- **POST simulador de crédito (JWT, política `CreditoUser`):** `POST /api/v1/credito/simulador-credito` — **`CREDITO.usp_SimuladorCredito`**; cuerpo JSON alineado a **`CreditoBL.SimuladorCredito`** (`monto`, `formaPago` D/M/Q/S, `nroCuotas`, `interesMensual`, `fechaPrimerPago`, `gastosAdm` opcional). Si **`monto` <= 0**, devuelve lista vacía sin llamar al proc (como el MVC). Respuesta: lista de cuotas (`numero`, `capital`, `fechaPago`, `amortizacion`, `interes`, `gastosAdm`, `cuota`).
- **Estado del plan de pagos (JWT, política `CreditoUser`):** `GET /api/v1/credito/estado-plan-pago?creditoId=` — **`CREDITO.usp_EstadoPlanPago`**; `creditoId` obligatorio (≥ 1).
- **Cuotas pendientes (JWT, política `CreditoUser`):** `GET /api/v1/credito/cuotas-pendientes?creditoId=` — **`CREDITO.usp_CuotasPendientes`**; `creditoId` obligatorio; **`fechaCalculo`** opcional (fecha, default hoy local); **`indCancelacion`** opcional (default `false`).
- **Saldo cartera por periodo (JWT, política `CreditoUser`):** `GET /api/v1/credito/listar-saldo-cartera?anio=&mes=&oficinaId=&usuarioId=` — **`CREDITO.usp_ListarSaldoCartera`**; parámetros obligatorios (`anio` 1900–2100, `mes` 1–12); **`oficinaId`** y **`usuarioId`** deben coincidir con **`vendix:oficina_id`** y **`vendix:usuario_id`** del token.
- **Saldo cartera por periodo CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/listar-saldo-cartera-csv?anio=&mes=&oficinaId=&usuarioId=` — mismos datos y reglas que **`listar-saldo-cartera`**; **`text/csv; charset=utf-8`**, **`listar-saldo-cartera.csv`** (UTF-8 con BOM). Distinto de **`rpt-saldo-cartera-caja-diario-csv`**.
- **Reporte saldo cartera por caja diario (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldo-cartera-caja-diario?oficinaId=&anioIni=&mesIni=&anioFin=&mesFin=` — **`CREDITO.usp_RptSaldoCarteraCajaDiario`** (rango de meses); **`usuarioId`** opcional acotado al token. Equivale a **`ReporteController.ReporteSaldoCarteraCajaDiario`** sin oficina/usuario null. **No** es **`listar-saldo-cartera`**.
- **Saldo cartera caja diario CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldo-cartera-caja-diario-csv?oficinaId=&anioIni=&mesIni=&anioFin=&mesFin=` — mismos datos y reglas que **`rpt-saldo-cartera-caja-diario`**; **`text/csv; charset=utf-8`**, **`saldo-cartera-caja-diario.csv`** (UTF-8 con BOM).
- **Pagos no verificados Yape/Plin/transferencias (JWT, política `CreditoUser`):** `GET /api/v1/credito/pagos-no-verificados?oficinaId=` — **`CREDITO.usp_PagosNoVerificados`**; misma fuente que **`CreditoBL.LstVerificarPagosJGrid`**. **`oficinaId`** = token; el SP no tiene parámetros (no filtra por oficina). No equivale al conteo **`ValidarPagosNoVerificados`** (solo caja diario de sesión).
- **Pagos no verificados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/pagos-no-verificados-csv?oficinaId=` — mismos datos y reglas que **`pagos-no-verificados`**; **`text/csv; charset=utf-8`**, **`pagos-no-verificados.csv`** (UTF-8 con BOM).
- **Validar impagos pendientes caja (JWT, política `CreditoUser`):** `GET /api/v1/credito/completar-impagos-validacion?oficinaId=&cajaDiarioId=` — **`CREDITO.usp_CompletarImpagosValidacion`**; devuelve `cantidadImpagosPendientes` (como **`CreditoBL.CompletarImpagosValidar`**). La caja diario debe pertenecer a la oficina del token. **No** ejecuta **`usp_CompletarImpagos`** (escritura).
- **Completar impagos caja (JWT, escritura, política `CreditoUser`):** `POST /api/v1/credito/completar-impagos` — body JSON `{ "oficinaId", "cajaDiarioId" }`; ejecuta **`CREDITO.usp_CompletarImpagos`**. Devuelve **409** si la validación previa reporta impagos pendientes. Paridad **`CreditoBL.CompletarImpagos`**.
- **Pagar cuotas (JWT, escritura):** `POST /api/v1/credito/pagar-cuotas` — **`CREDITO.usp_PagarCuotas`**; body con `listaPlanPagoId`, `importeRecibido`, `tipoPagoId`, etc.; `usuarioId`/`fechaPago` desde token y `usp_FechaBD`. **422** si el proc devuelve resultado negativo.
- **Pagar cuota importe libre (JWT, escritura):** `POST /api/v1/credito/pagar-cuota-importe-libre` — **`usp_PagarCuotaPagoLibre`**.
- **Pagar cuotas cancelación (JWT, escritura):** `POST /api/v1/credito/pagar-cuotas-cancelacion` — **`usp_PagarCuotasCancelacion`**.
- **Reconciliar caja diario (JWT, escritura):** `POST /api/v1/credito/reconciliar-caja-diario` — **`usp_ReconciliarCajaDiario`**; body `{ oficinaId, cajaDiarioId }`.
- **Recalcular caja diario (JWT, escritura):** `POST /api/v1/credito/recalcular-caja-diario` — **`usp_RecalcularCajaDiario`**; body `{ oficinaId, cajaDiarioId }`; respuesta `{ resultCode, cajaDiarioId }`.
- **Transferir saldos caja diario (JWT, escritura):** `POST /api/v1/credito/transferir-saldos-caja-diario` — paridad **`CajaDiarioController.TransferirSaldos`**; body `{ oficinaId, cajaDiarioId, importe, descripcion, cajaIdDestino? }` (`cajaIdDestino` null = bóveda).
- **Confirmar clave egreso caja (JWT):** `POST /api/v1/credito/confirmar-clave-caja-diario` — paridad **`ConfirmarClave`** (usuario `ADMVENDIX`); body `{ clave }`; respuesta `{ autorizado, mensaje }`.
- **Tiene CxC pendiente por crédito (JWT):** `GET /api/v1/credito/tiene-cxc-pendiente?creditoId=` — paridad **`CreditoController.TieneCxcPendiente`**; respuesta `{ tienePendientes }`.
- **Créditos DES del gestor (JWT):** `GET /api/v1/credito/creditos-gestor-desembolsados` — paridad **`CajaDiarioBL.LstCreditoPendienteJGrid`** (modal «Cuotas pendientes» del MVC). Ver **`docs/CAJA_DIARIO_MIGRACION.md`** para el mapa completo UI ↔ legacy.
- **Validar anular movimiento caja (JWT):** `GET /api/v1/credito/validar-anular-movimiento-caja?oficinaId=&movimientoCajaId=` — paridad **`ValidarAnularMovimientoCaja`**; `{ requiereConfirmacion }`.
- **Anular movimiento caja (JWT, escritura):** `POST /api/v1/credito/anular-movimiento-caja` — **`usp_MovimientoCaja_Del`**; body `{ oficinaId, movimientoCajaId, observacion }`; `usuarioId` desde JWT; **409** si ya anulado o caja cerrada.
- **Desembolsos pendientes (JWT):** `GET /api/v1/credito/desembolsos-pendientes?oficinaId=&personaId=0` — créditos **APR** (paridad **`LstDesembolsoJGrid`**).
- **Validar desembolso (JWT):** `GET /api/v1/credito/validar-desembolso?oficinaId=&cajaDiarioId=&creditoId=` — CxC pendientes y saldo caja.
- **Realizar desembolso (JWT, escritura):** `POST /api/v1/credito/realizar-desembolso` — paridad **`RealizarDesembolso`**; body `{ oficinaId, cajaDiarioId, creditoId }`; idempotente si ya hay movimiento **DES**.
- **Entrada/salida caja diario (JWT, escritura):** `POST /api/v1/credito/entrada-salida-caja-diario` — **`usp_EntradaSalidaCajaDiario`**; body `{ oficinaId, cajaDiarioId, personaId, tipoOperacionId, importe, descripcion, tipoPagoId }`; valida saldo en egresos (paridad **`EntradaSalida`**).
- **Cuentas por cobrar pendientes (JWT):** `GET /api/v1/credito/cuentas-por-cobrar-pendientes?oficinaId=&cajaDiarioId=&personaId=0` — paridad **`LstCuentasxCobrarJGrid`** (CxC `PEN` + orden `CON`/`ENV` si `personaId > 0`).
- **Pagar cuenta por cobrar (JWT, escritura):** `POST /api/v1/credito/pagar-cuenta-por-cobrar` — **`usp_PagarCuentaxCobrar`**; body `{ oficinaId, cajaDiarioId, ordenVentaId, cuentaxCobrarId }` (`cuentaxCobrarId=0` = venta contado); respuesta `{ resultId }`; **422** si el proc falla.
- **Bóveda temporal (JWT):** `GET /api/v1/credito/existe-boveda-temporal?oficinaId=` — paridad **`ExisteBovedaTemporal`**.
- **Cerrar bóveda (JWT, escritura):** `POST /api/v1/credito/cerrar-boveda` — **`usp_CerrarBoveda`**; body `{ oficinaId }`; respuesta `{ resultCode }`.
- **Cerrar bóveda temporal (JWT, escritura):** `POST /api/v1/credito/cerrar-boveda-temporal` — **`usp_CerrarBovedaTemporal`**.
- **Transferir bóveda (JWT, escritura):** `POST /api/v1/credito/transferir-boveda` — **`usp_TransferirBoveda`**; body con bóvedas origen/destino o `bovedaMovTempId` + `flagAceptar` (aceptación inter-oficina).
- **Asignar caja (JWT, escritura):** `GET /api/v1/credito/cajas-para-asignar?oficinaId=` — combo cajas cerradas; `GET /api/v1/credito/monto-boveda-asignacion?oficinaId=` — saldo bóveda (ENCARGADO → temporal); `POST /api/v1/credito/asignar-caja` — paridad **`SaldosController.AsignarCaja`** / **`CajaDiarioBL.AsignarUsuarioCaja`**; body `{ oficinaId, cajaId, saldoInicial }`; **422** con mensaje MVC si falla validación.
- **Validar cierre masivo Saldos (JWT):** `GET /api/v1/credito/validar-cierre-saldos?oficinaId=` — paridad **`SaldosController.ValidarCierre`**; `{ puedeCerrar, mensaje }` (mensaje vacío = OK). Previo a **`cerrar-cajas-diarios`**.
- **Validar cierre caja chica (JWT):** `GET /api/v1/credito/validar-cierre-caja-chica?oficinaId=` — paridad **`ValidarCierreCajaChica`** (sin filtro oficina en BD, como MVC).
- **Actualizar datos post cierre bóveda (JWT, escritura):** `POST /api/v1/credito/actualizar-datos-post-cierre-boveda` — **`usp_ActualizarSaldoCartera`** + **`usp_CalificarCliente`**; body `{ oficinaId }`.
- **Ingreso/egreso bóveda (JWT, escritura):** `POST /api/v1/credito/ingreso-egreso-boveda` — paridad **`BovedaMovBL.IngresoEgresoBovedaCaja`**; body `{ oficinaId, importe, descripcion, tipoOperacionId, tipoPagoId }`; tipo operación con `IndBoveda`.
- **Transferir bóveda a caja (JWT, escritura):** `GET /api/v1/credito/cajas-abiertas-transferencia-boveda?oficinaId=`; `POST /api/v1/credito/transferir-boveda-caja` — paridad **`TransferirBovedaCaja`** (BovedaMov + MovimientoCaja TRE).
- **Transferir bóveda a caja chica (JWT, escritura):** `POST /api/v1/credito/transferir-boveda-caja-chica` — paridad **`TransferirBovedaCajaChica`**.
- **Transferir cierre caja chica a bóveda (JWT, escritura):** `POST /api/v1/credito/transferir-cierre-caja-chica` — tras **`GET validar-cierre-caja-chica`**; respuesta `{ cajasTransferidas }`.
- **Asignar bóveda temporal (JWT, escritura):** `POST /api/v1/credito/asignar-boveda-temporal` — body `{ oficinaId, importe, descripcion, usuarioId }` (`usuarioId=0` solo transferir a temporal existente).
- **Entrada/salida caja chica (JWT, escritura):** `POST /api/v1/credito/entrada-salida-caja-chica-diario` — **`usp_EntradaSalidaCajaChicaDiario`**; caja chica abierta del usuario del token.
- **Validar anular crédito (JWT):** `GET /api/v1/credito/validar-anular-credito?oficinaId=&creditoId=` — paridad **`ValidarAnularCredito`**; `{ puedeAnular }`.
- **Aprobar crédito (JWT, escritura):** `POST /api/v1/credito/aprobar-credito` — **`usp_Credito_Upd`**; body `{ oficinaId, creditoId, opcion }` (`0` = 1.ª aprobación, `1` = 2.ª).
- **Anular crédito (JWT, escritura):** `POST /api/v1/credito/anular-credito` — **`usp_Credito_Del`**; **409** si `validar-anular-credito` falla.
- **Reprogramar crédito (JWT, escritura):** `POST /api/v1/credito/reprogramar-credito` — **`usp_ReprogramarCredito`**.
- **Prorrogar crédito (JWT, escritura):** `POST /api/v1/credito/prorrogar-credito` — **`usp_ProrrogarCredito`**; body `{ oficinaId, creditoId, dias }`.
- **Crear solicitud crédito (JWT, escritura):** `POST /api/v1/credito/crear-solicitud-credito` — paridad **`CrearSolicitudCredito`**; body `{ oficinaId, personaId }`; devuelve `solicitudCreditoId`.
- **Generar crédito (JWT, escritura):** `POST /api/v1/credito/crear-credito` — **`usp_Credito_Ins`**; body con `solicitudCreditoId` y datos del simulador (paridad **`GenerarCredito`**).
- **Rechazar crédito (JWT, escritura):** `POST /api/v1/credito/rechazar-credito` — paridad **`RechazarCredito`**.
- **Orden de venta — detalle (JWT, escritura):** `POST /api/v1/ventas/agregar-orden-venta-detalle`, `actualizar-orden-venta-detalle`, `eliminar-orden-venta-detalle`, `eliminar-orden-venta` — procs `usp_OrdenVentaDet_*` / `usp_OrdenVenta_Del`.
- **Movimiento almacén (JWT, escritura):** `POST /api/v1/almacen/confirmar-movimiento`, `desconfirmar-movimiento`, `actualizar-movimiento`, `crear-movimiento-detalle`, `eliminar-movimiento-detalle` — paridad **`MovimientoBL`** / **`MovimientoDetBL`**.
- **Enviar orden de venta (JWT, escritura):** `POST /api/v1/ventas/enviar-orden-venta-contado` — paridad **`EnviarOrdenVentaContado`**; `POST /api/v1/ventas/enviar-orden-venta-credito` — paridad **`EnviarOrdenVentaCredito`** (devuelve `creditoId`).
- **Venta rápida (JWT, escritura):** `POST /api/v1/ventas/realizar-pedido` — paridad **`RealizarPedido`**; body `{ oficinaId, cajaDiarioId, personaId, pedidos[] }`; devuelve `{ ordenVentaId, resultId }`.
- **Artículo venta rápida (JWT, lectura):** `GET /api/v1/ventas/articulo-venta-rapida?oficinaId=&codigo=` — paridad **`ObtenerArticulo`** (`articuloId`, `precioVenta`, `stock`).
- **Caja diario venta rápida (JWT, lectura):** `GET /api/v1/ventas/caja-diario-venta-rapida?oficinaId=` — paridad cabecera **`Index`** (`cajaDiarioId`, `cajaDenominacion`, `fechaIniOperacion`).
- **Catálogo informes MVC (JWT, lectura):** `GET /api/v1/reportes/catalogo` — lista acciones **`ReporteController`** (sin render RDLC); base para BFF/SPA.
- **Cobertura catálogo vs API (JWT):** `GET /api/v1/reportes/catalogo-cobertura` — matriz 52 ítems (completo-datos / solo-mvc / parcial / …); ver [CATALOGO-INFORMES-COBERTURA.md](../docs/migration/CATALOGO-INFORMES-COBERTURA.md).
- **Política exportación informes (JWT, lectura):** `GET /api/v1/reportes/politica-exportacion` — fase **`pdf-tabular-completo`**: cada informe con **`-csv`** tiene **`-pdf`** (32 rutas); tres informes solo JSON `{ texto }`; layout `.rdlc` idéntico = **`layout-rdlc-fase4-opcional`** (ver [MIGRATION-CLOSURE.md](../docs/migration/MIGRATION-CLOSURE.md)).
- **Export PDF tabular (JWT):** toda ruta `GET .../*-csv` tiene gemela `GET .../*-pdf` — **`application/pdf`**, columnas = CSV (`TabularPdfDocument` / QuestPDF). Ej.: `rpt-credito-observado-pdf`, `rpt-clientes-bloqueados-pdf`, `almacen/rpt-stock-anulados-pdf`.
- **Informes texto RDLC (JWT, sin CSV ni PDF):** `GET .../rpt-saldos-caja-resumen-ingreso`, `.../rpt-saldos-caja-resumen-tipo-cuenta`, `.../resumen-cuenta-boveda` — JSON `{ texto }`.
- **Cerrar cajas diarios / transferir bóveda (JWT, escritura):** `POST /api/v1/credito/cerrar-cajas-diarios` — **`usp_CerrarCajasDiarios`**; body `{ oficinaId, sobrante }` (como **`SaldosController.Transferir`**). Cierre masivo de oficina, no el del cajero.
- **Validar cierre caja diario (JWT):** `GET /api/v1/credito/validar-cierre-caja-diario?oficinaId=&cajaDiarioId=` — paridad **`ValidarCierreCajaDiario`**; respuesta `{ puedeCerrar, blockers }`.
- **Cerrar caja diario cajero (JWT, escritura):** `POST /api/v1/credito/cerrar-caja-diario` — paridad **`CerrarCajaDiario`** (UPDATE + **`ActualizarClientesNuevos`**); **409** si hay bloqueos de validación.
- **Créditos observados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-observado?oficinaId=` — consulta SQL equivalente a **`CreditoBL.ReporteCreditoObservado`** (estados PEN/DES con observación); **`usuarioId`** opcional acotado al token. JSON sustituto del informe **`rptCreditoObservado.rdlc`**.
- **Créditos observados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-observado-csv?oficinaId=` — mismos datos y reglas que **`rpt-credito-observado`**; **`text/csv; charset=utf-8`**, **`credito-observado.csv`** (UTF-8 con BOM).

- **Créditos condonados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-condonado?oficinaId=&fechaIni=&fechaFin=` — equivalente a **`CreditoBL.ReporteCreditoCondonado`**; **`usuarioId`** opcional. JSON para **`rptCreditoCondonado.rdlc`**.
- **Créditos condonados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-condonado-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-credito-condonado`**; **`text/csv; charset=utf-8`**, **`credito-condonado.csv`** (UTF-8 con BOM).
- **Créditos condonados PDF (JWT):** `GET /api/v1/credito/rpt-credito-condonado-pdf?oficinaId=&fechaIni=&fechaFin=` — mismos datos que CSV; ver política de exportación para el listado completo.
- **Clientes nuevos del mes (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-nuevos-mes?oficinaId=` — equivalente a **`CreditoBL.ReporteClientesNuevosMes`**; **`fechaIni`/`fechaFin`** opcionales (mes calendario actual si se omiten). Reutiliza el mismo shape que observados.
- **Clientes nuevos del mes CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-nuevos-mes-csv?oficinaId=` — mismas reglas que **`rpt-clientes-nuevos-mes`**; **`text/csv; charset=utf-8`**, **`clientes-nuevos-mes.csv`** (UTF-8 con BOM; columnas alineadas al DTO compartido con observados).
- **Stock anulados (JWT, política `CreditoUser`):** `GET /api/v1/almacen/rpt-stock-anulados` — equivalente a **`ReporteBL.ListarReporteStockAnulados`** (sin filtro de oficina, paridad MVC).
- **Stock anulados CSV (JWT, política `CreditoUser`):** `GET /api/v1/almacen/rpt-stock-anulados-csv` — mismos datos que **`rpt-stock-anulados`**; **`text/csv; charset=utf-8`**, **`stock-anulados.csv`** (UTF-8 con BOM).
- **Lista de precios informe (JWT, política `CreditoUser`):** `GET /api/v1/ventas/rpt-lista-precio?marcaId=&indDescuento=&indPuntos=` — equivalente a **`ReporteBL.ListarReporteListaPrecio`**. Distinto de **`GET /api/v1/lista-precios`** (catálogo activo).
- **Lista de precios informe CSV (JWT, política `CreditoUser`):** `GET /api/v1/ventas/rpt-lista-precio-csv?marcaId=&indDescuento=&indPuntos=` — mismos datos que **`rpt-lista-precio`**; respuesta **`text/csv; charset=utf-8`**, descarga **`lista-precio.csv`** (UTF-8 con BOM para Excel).
- **Saldo cartera por periodo CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/listar-saldo-cartera-csv?anio=&mes=&oficinaId=&usuarioId=` — mismos datos y reglas que **`listar-saldo-cartera`**; **`text/csv; charset=utf-8`**, **`listar-saldo-cartera.csv`** (UTF-8 con BOM). Distinto de **`rpt-saldo-cartera-caja-diario-csv`**.
- **Reporte saldo cartera por caja diario (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldo-cartera-caja-diario?oficinaId=&anioIni=&mesIni=&anioFin=&mesFin=` — **`CREDITO.usp_RptSaldoCarteraCajaDiario`** (rango de meses); **`usuarioId`** opcional acotado al token. Equivale a **`ReporteController.ReporteSaldoCarteraCajaDiario`** sin oficina/usuario null. **No** es **`listar-saldo-cartera`**.
- **Saldo cartera caja diario CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldo-cartera-caja-diario-csv?oficinaId=&anioIni=&mesIni=&anioFin=&mesFin=` — mismos datos y reglas que **`rpt-saldo-cartera-caja-diario`**; **`text/csv; charset=utf-8`**, **`saldo-cartera-caja-diario.csv`** (UTF-8 con BOM).
- **Pagos no verificados Yape/Plin/transferencias (JWT, política `CreditoUser`):** `GET /api/v1/credito/pagos-no-verificados?oficinaId=` — **`CREDITO.usp_PagosNoVerificados`**; misma fuente que **`CreditoBL.LstVerificarPagosJGrid`**. **`oficinaId`** = token; el SP no tiene parámetros (no filtra por oficina). No equivale al conteo **`ValidarPagosNoVerificados`** (solo caja diario de sesión).
- **Pagos no verificados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/pagos-no-verificados-csv?oficinaId=` — mismos datos y reglas que **`pagos-no-verificados`**; **`text/csv; charset=utf-8`**, **`pagos-no-verificados.csv`** (UTF-8 con BOM).
- **Validar impagos pendientes caja (JWT, política `CreditoUser`):** `GET /api/v1/credito/completar-impagos-validacion?oficinaId=&cajaDiarioId=` — **`CREDITO.usp_CompletarImpagosValidacion`**; devuelve `cantidadImpagosPendientes` (como **`CreditoBL.CompletarImpagosValidar`**). La caja diario debe pertenecer a la oficina del token. **No** ejecuta **`usp_CompletarImpagos`** (escritura).
- **Completar impagos caja (JWT, escritura, política `CreditoUser`):** `POST /api/v1/credito/completar-impagos` — body JSON `{ "oficinaId", "cajaDiarioId" }`; ejecuta **`CREDITO.usp_CompletarImpagos`**. Devuelve **409** si la validación previa reporta impagos pendientes. Paridad **`CreditoBL.CompletarImpagos`**.
- **Pagar cuotas (JWT, escritura):** `POST /api/v1/credito/pagar-cuotas` — **`CREDITO.usp_PagarCuotas`**; body con `listaPlanPagoId`, `importeRecibido`, `tipoPagoId`, etc.; `usuarioId`/`fechaPago` desde token y `usp_FechaBD`. **422** si el proc devuelve resultado negativo.
- **Pagar cuota importe libre (JWT, escritura):** `POST /api/v1/credito/pagar-cuota-importe-libre` — **`usp_PagarCuotaPagoLibre`**.
- **Pagar cuotas cancelación (JWT, escritura):** `POST /api/v1/credito/pagar-cuotas-cancelacion` — **`usp_PagarCuotasCancelacion`**.
- **Reconciliar caja diario (JWT, escritura):** `POST /api/v1/credito/reconciliar-caja-diario` — **`usp_ReconciliarCajaDiario`**; body `{ oficinaId, cajaDiarioId }`.
- **Recalcular caja diario (JWT, escritura):** `POST /api/v1/credito/recalcular-caja-diario` — **`usp_RecalcularCajaDiario`**; body `{ oficinaId, cajaDiarioId }`; respuesta `{ resultCode, cajaDiarioId }`.
- **Transferir saldos caja diario (JWT, escritura):** `POST /api/v1/credito/transferir-saldos-caja-diario` — paridad **`CajaDiarioController.TransferirSaldos`**; body `{ oficinaId, cajaDiarioId, importe, descripcion, cajaIdDestino? }` (`cajaIdDestino` null = bóveda).
- **Confirmar clave egreso caja (JWT):** `POST /api/v1/credito/confirmar-clave-caja-diario` — paridad **`ConfirmarClave`** (usuario `ADMVENDIX`); body `{ clave }`; respuesta `{ autorizado, mensaje }`.
- **Tiene CxC pendiente por crédito (JWT):** `GET /api/v1/credito/tiene-cxc-pendiente?creditoId=` — paridad **`CreditoController.TieneCxcPendiente`**; respuesta `{ tienePendientes }`.
- **Créditos DES del gestor (JWT):** `GET /api/v1/credito/creditos-gestor-desembolsados` — paridad **`CajaDiarioBL.LstCreditoPendienteJGrid`** (modal «Cuotas pendientes» del MVC). Ver **`docs/CAJA_DIARIO_MIGRACION.md`** para el mapa completo UI ↔ legacy.
- **Validar anular movimiento caja (JWT):** `GET /api/v1/credito/validar-anular-movimiento-caja?oficinaId=&movimientoCajaId=` — paridad **`ValidarAnularMovimientoCaja`**; `{ requiereConfirmacion }`.
- **Anular movimiento caja (JWT, escritura):** `POST /api/v1/credito/anular-movimiento-caja` — **`usp_MovimientoCaja_Del`**; body `{ oficinaId, movimientoCajaId, observacion }`; `usuarioId` desde JWT; **409** si ya anulado o caja cerrada.
- **Desembolsos pendientes (JWT):** `GET /api/v1/credito/desembolsos-pendientes?oficinaId=&personaId=0` — créditos **APR** (paridad **`LstDesembolsoJGrid`**).
- **Validar desembolso (JWT):** `GET /api/v1/credito/validar-desembolso?oficinaId=&cajaDiarioId=&creditoId=` — CxC pendientes y saldo caja.
- **Realizar desembolso (JWT, escritura):** `POST /api/v1/credito/realizar-desembolso` — paridad **`RealizarDesembolso`**; body `{ oficinaId, cajaDiarioId, creditoId }`; idempotente si ya hay movimiento **DES**.
- **Entrada/salida caja diario (JWT, escritura):** `POST /api/v1/credito/entrada-salida-caja-diario` — **`usp_EntradaSalidaCajaDiario`**; body `{ oficinaId, cajaDiarioId, personaId, tipoOperacionId, importe, descripcion, tipoPagoId }`; valida saldo en egresos (paridad **`EntradaSalida`**).
- **Cuentas por cobrar pendientes (JWT):** `GET /api/v1/credito/cuentas-por-cobrar-pendientes?oficinaId=&cajaDiarioId=&personaId=0` — paridad **`LstCuentasxCobrarJGrid`** (CxC `PEN` + orden `CON`/`ENV` si `personaId > 0`).
- **Pagar cuenta por cobrar (JWT, escritura):** `POST /api/v1/credito/pagar-cuenta-por-cobrar` — **`usp_PagarCuentaxCobrar`**; body `{ oficinaId, cajaDiarioId, ordenVentaId, cuentaxCobrarId }` (`cuentaxCobrarId=0` = venta contado); respuesta `{ resultId }`; **422** si el proc falla.
- **Bóveda temporal (JWT):** `GET /api/v1/credito/existe-boveda-temporal?oficinaId=` — paridad **`ExisteBovedaTemporal`**.
- **Cerrar bóveda (JWT, escritura):** `POST /api/v1/credito/cerrar-boveda` — **`usp_CerrarBoveda`**; body `{ oficinaId }`; respuesta `{ resultCode }`.
- **Cerrar bóveda temporal (JWT, escritura):** `POST /api/v1/credito/cerrar-boveda-temporal` — **`usp_CerrarBovedaTemporal`**.
- **Transferir bóveda (JWT, escritura):** `POST /api/v1/credito/transferir-boveda` — **`usp_TransferirBoveda`**; body con bóvedas origen/destino o `bovedaMovTempId` + `flagAceptar` (aceptación inter-oficina).
- **Asignar caja (JWT, escritura):** `GET /api/v1/credito/cajas-para-asignar?oficinaId=` — combo cajas cerradas; `GET /api/v1/credito/monto-boveda-asignacion?oficinaId=` — saldo bóveda (ENCARGADO → temporal); `POST /api/v1/credito/asignar-caja` — paridad **`SaldosController.AsignarCaja`** / **`CajaDiarioBL.AsignarUsuarioCaja`**; body `{ oficinaId, cajaId, saldoInicial }`; **422** con mensaje MVC si falla validación.
- **Validar cierre masivo Saldos (JWT):** `GET /api/v1/credito/validar-cierre-saldos?oficinaId=` — paridad **`SaldosController.ValidarCierre`**; `{ puedeCerrar, mensaje }` (mensaje vacío = OK). Previo a **`cerrar-cajas-diarios`**.
- **Validar cierre caja chica (JWT):** `GET /api/v1/credito/validar-cierre-caja-chica?oficinaId=` — paridad **`ValidarCierreCajaChica`** (sin filtro oficina en BD, como MVC).
- **Actualizar datos post cierre bóveda (JWT, escritura):** `POST /api/v1/credito/actualizar-datos-post-cierre-boveda` — **`usp_ActualizarSaldoCartera`** + **`usp_CalificarCliente`**; body `{ oficinaId }`.
- **Ingreso/egreso bóveda (JWT, escritura):** `POST /api/v1/credito/ingreso-egreso-boveda` — paridad **`BovedaMovBL.IngresoEgresoBovedaCaja`**; body `{ oficinaId, importe, descripcion, tipoOperacionId, tipoPagoId }`; tipo operación con `IndBoveda`.
- **Transferir bóveda a caja (JWT, escritura):** `GET /api/v1/credito/cajas-abiertas-transferencia-boveda?oficinaId=`; `POST /api/v1/credito/transferir-boveda-caja` — paridad **`TransferirBovedaCaja`** (BovedaMov + MovimientoCaja TRE).
- **Transferir bóveda a caja chica (JWT, escritura):** `POST /api/v1/credito/transferir-boveda-caja-chica` — paridad **`TransferirBovedaCajaChica`**.
- **Transferir cierre caja chica a bóveda (JWT, escritura):** `POST /api/v1/credito/transferir-cierre-caja-chica` — tras **`GET validar-cierre-caja-chica`**; respuesta `{ cajasTransferidas }`.
- **Asignar bóveda temporal (JWT, escritura):** `POST /api/v1/credito/asignar-boveda-temporal` — body `{ oficinaId, importe, descripcion, usuarioId }` (`usuarioId=0` solo transferir a temporal existente).
- **Entrada/salida caja chica (JWT, escritura):** `POST /api/v1/credito/entrada-salida-caja-chica-diario` — **`usp_EntradaSalidaCajaChicaDiario`**; caja chica abierta del usuario del token.
- **Validar anular crédito (JWT):** `GET /api/v1/credito/validar-anular-credito?oficinaId=&creditoId=` — paridad **`ValidarAnularCredito`**; `{ puedeAnular }`.
- **Aprobar crédito (JWT, escritura):** `POST /api/v1/credito/aprobar-credito` — **`usp_Credito_Upd`**; body `{ oficinaId, creditoId, opcion }` (`0` = 1.ª aprobación, `1` = 2.ª).
- **Anular crédito (JWT, escritura):** `POST /api/v1/credito/anular-credito` — **`usp_Credito_Del`**; **409** si `validar-anular-credito` falla.
- **Reprogramar crédito (JWT, escritura):** `POST /api/v1/credito/reprogramar-credito` — **`usp_ReprogramarCredito`**.
- **Prorrogar crédito (JWT, escritura):** `POST /api/v1/credito/prorrogar-credito` — **`usp_ProrrogarCredito`**; body `{ oficinaId, creditoId, dias }`.
- **Crear solicitud crédito (JWT, escritura):** `POST /api/v1/credito/crear-solicitud-credito` — paridad **`CrearSolicitudCredito`**; body `{ oficinaId, personaId }`; devuelve `solicitudCreditoId`.
- **Generar crédito (JWT, escritura):** `POST /api/v1/credito/crear-credito` — **`usp_Credito_Ins`**; body con `solicitudCreditoId` y datos del simulador (paridad **`GenerarCredito`**).
- **Rechazar crédito (JWT, escritura):** `POST /api/v1/credito/rechazar-credito` — paridad **`RechazarCredito`**.
- **Orden de venta — detalle (JWT, escritura):** `POST /api/v1/ventas/agregar-orden-venta-detalle`, `actualizar-orden-venta-detalle`, `eliminar-orden-venta-detalle`, `eliminar-orden-venta` — procs `usp_OrdenVentaDet_*` / `usp_OrdenVenta_Del`.
- **Movimiento almacén (JWT, escritura):** `POST /api/v1/almacen/confirmar-movimiento`, `desconfirmar-movimiento`, `actualizar-movimiento`, `crear-movimiento-detalle`, `eliminar-movimiento-detalle` — paridad **`MovimientoBL`** / **`MovimientoDetBL`**.
- **Enviar orden de venta (JWT, escritura):** `POST /api/v1/ventas/enviar-orden-venta-contado` — paridad **`EnviarOrdenVentaContado`**; `POST /api/v1/ventas/enviar-orden-venta-credito` — paridad **`EnviarOrdenVentaCredito`** (devuelve `creditoId`).
- **Venta rápida (JWT, escritura):** `POST /api/v1/ventas/realizar-pedido` — paridad **`RealizarPedido`**; body `{ oficinaId, cajaDiarioId, personaId, pedidos[] }`; devuelve `{ ordenVentaId, resultId }`.
- **Artículo venta rápida (JWT, lectura):** `GET /api/v1/ventas/articulo-venta-rapida?oficinaId=&codigo=` — paridad **`ObtenerArticulo`** (`articuloId`, `precioVenta`, `stock`).
- **Caja diario venta rápida (JWT, lectura):** `GET /api/v1/ventas/caja-diario-venta-rapida?oficinaId=` — paridad cabecera **`Index`** (`cajaDiarioId`, `cajaDenominacion`, `fechaIniOperacion`).
- **Catálogo informes MVC (JWT, lectura):** `GET /api/v1/reportes/catalogo` — lista acciones **`ReporteController`** (sin render RDLC); base para BFF/SPA.
- **Cobertura catálogo vs API (JWT):** `GET /api/v1/reportes/catalogo-cobertura` — matriz 52 ítems (completo-datos / solo-mvc / parcial / …); ver [CATALOGO-INFORMES-COBERTURA.md](../docs/migration/CATALOGO-INFORMES-COBERTURA.md).
- **Política exportación informes (JWT, lectura):** `GET /api/v1/reportes/politica-exportacion` — fase **`pdf-tabular-completo`**: cada informe con **`-csv`** tiene **`-pdf`** (32 rutas); tres informes solo JSON `{ texto }`; layout `.rdlc` idéntico = **`layout-rdlc-fase4-opcional`** (ver [MIGRATION-CLOSURE.md](../docs/migration/MIGRATION-CLOSURE.md)).
- **Export PDF tabular (JWT):** toda ruta `GET .../*-csv` tiene gemela `GET .../*-pdf` — **`application/pdf`**, columnas = CSV (`TabularPdfDocument` / QuestPDF). Ej.: `rpt-credito-observado-pdf`, `rpt-clientes-bloqueados-pdf`, `almacen/rpt-stock-anulados-pdf`.
- **Informes texto RDLC (JWT, sin CSV ni PDF):** `GET .../rpt-saldos-caja-resumen-ingreso`, `.../rpt-saldos-caja-resumen-tipo-cuenta`, `.../resumen-cuenta-boveda` — JSON `{ texto }`.
- **Cerrar cajas diarios / transferir bóveda (JWT, escritura):** `POST /api/v1/credito/cerrar-cajas-diarios` — **`usp_CerrarCajasDiarios`**; body `{ oficinaId, sobrante }` (como **`SaldosController.Transferir`**). Cierre masivo de oficina, no el del cajero.
- **Validar cierre caja diario (JWT):** `GET /api/v1/credito/validar-cierre-caja-diario?oficinaId=&cajaDiarioId=` — paridad **`ValidarCierreCajaDiario`**; respuesta `{ puedeCerrar, blockers }`.
- **Cerrar caja diario cajero (JWT, escritura):** `POST /api/v1/credito/cerrar-caja-diario` — paridad **`CerrarCajaDiario`** (UPDATE + **`ActualizarClientesNuevos`**); **409** si hay bloqueos de validación.
- **Créditos observados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-observado?oficinaId=` — consulta SQL equivalente a **`CreditoBL.ReporteCreditoObservado`** (estados PEN/DES con observación); **`usuarioId`** opcional acotado al token. JSON sustituto del informe **`rptCreditoObservado.rdlc`**.
- **Créditos observados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-observado-csv?oficinaId=` — mismos datos y reglas que **`rpt-credito-observado`**; **`text/csv; charset=utf-8`**, **`credito-observado.csv`** (UTF-8 con BOM).
- **Créditos condonados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-condonado?oficinaId=&fechaIni=&fechaFin=` — equivalente a **`CreditoBL.ReporteCreditoCondonado`**; **`usuarioId`** opcional. JSON para **`rptCreditoCondonado.rdlc`**.
- **Créditos condonados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-condonado-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-credito-condonado`**; **`text/csv; charset=utf-8`**, **`credito-condonado.csv`** (UTF-8 con BOM).
- **Créditos condonados PDF (JWT):** `GET /api/v1/credito/rpt-credito-condonado-pdf?oficinaId=&fechaIni=&fechaFin=` — mismos datos que CSV; ver política de exportación para el listado completo.
- **Clientes nuevos del mes (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-nuevos-mes?oficinaId=` — equivalente a **`CreditoBL.ReporteClientesNuevosMes`**; **`fechaIni`/`fechaFin`** opcionales (mes calendario actual si se omiten). Reutiliza el mismo shape que observados.
- **Clientes nuevos del mes CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-nuevos-mes-csv?oficinaId=` — mismas reglas que **`rpt-clientes-nuevos-mes`**; **`text/csv; charset=utf-8`**, **`clientes-nuevos-mes.csv`** (UTF-8 con BOM; columnas alineadas al DTO compartido con observados).
- **Stock anulados (JWT, política `CreditoUser`):** `GET /api/v1/almacen/rpt-stock-anulados` — equivalente a **`ReporteBL.ListarReporteStockAnulados`** (sin filtro de oficina, paridad MVC).
- **Stock anulados CSV (JWT, política `CreditoUser`):** `GET /api/v1/almacen/rpt-stock-anulados-csv` — mismos datos que **`rpt-stock-anulados`**; **`text/csv; charset=utf-8`**, **`stock-anulados.csv`** (UTF-8 con BOM).
- **Lista de precios informe (JWT, política `CreditoUser`):** `GET /api/v1/ventas/rpt-lista-precio?marcaId=&indDescuento=&indPuntos=` — equivalente a **`ReporteBL.ListarReporteListaPrecio`**. Distinto de **`GET /api/v1/lista-precios`** (catálogo activo).
- **Lista de precios informe CSV (JWT, política `CreditoUser`):** `GET /api/v1/ventas/rpt-lista-precio-csv?marcaId=&indDescuento=&indPuntos=` — mismos datos que **`rpt-lista-precio`**; respuesta **`text/csv; charset=utf-8`**, descarga **`lista-precio.csv`** (UTF-8 con BOM para Excel).
- **Mora pendiente (JWT, política `CreditoUser`):** `GET /api/v1/credito/calcular-mora-pendiente?creditoId=` — **`CREDITO.usp_CalcularMoraPendiente`**; `creditoId` obligatorio (≥ 1). Respuesta JSON `moraPendiente` (`decimal` o `null` si el proc no devuelve filas).
- **Movimientos de crédito / kardex caja (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-credito?creditoId=` — **`CREDITO.usp_RptMovimientoCredito`**; `creditoId` obligatorio (≥ 1). Lista de filas (fecha, operación, glosa, importes, saldo); mismo origen que `CreditoBL.ReporteCreditoMovimiento`.
- **Movimientos de crédito CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-credito-csv?creditoId=` — mismos datos que **`rpt-movimiento-credito`**; **`text/csv; charset=utf-8`**, **`movimiento-credito.csv`** (UTF-8 con BOM).
- **Movimientos de crédito CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-credito-csv?creditoId=` — mismos datos que **`rpt-movimiento-credito`**; **`text/csv; charset=utf-8`**, **`movimiento-credito.csv`** (UTF-8 con BOM).
- **Monto pendiente plan de pago por oficina (JWT, política `CreditoUser`):** `GET /api/v1/credito/obtener-monto-pendiente-plan-pago?oficinaId=` — **`CREDITO.usp_ObtenerMontoPendientePlanPago`**; `oficinaId` obligatorio y debe coincidir con **`vendix:oficina_id`** del token. Respuesta `montoPendiente` (`decimal` o `null`).
- **Usuarios sin caja asignada (JWT, política `CreditoUser`):** `GET /api/v1/credito/usuarios-no-asignados-caja?oficinaId=` — **`CREDITO.usp_UsuariosNoAsignadosCaja`**; `oficinaId` obligatorio y debe coincidir con **`vendix:oficina_id`** del token. Lista `id` / `valor` (combos caja, como `CajaBL.ListaUsuariosNoAsignado`).
- **Cajas asignadas por oficina (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cajas-asignadas?oficinaId=` — **`CREDITO.usp_RptCajasAsignadas`**; `oficinaId` obligatorio y debe coincidir con **`vendix:oficina_id`** del token. Lista caja diaria, cajero, saldos (como `CajaBL.LstCajaDiarioOficina`).
- **Cajas asignadas CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cajas-asignadas-csv?oficinaId=` — mismos datos y reglas que **`rpt-cajas-asignadas`**; **`text/csv; charset=utf-8`**, **`cajas-asignadas.csv`** (UTF-8 con BOM).
- **Cajas asignadas CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cajas-asignadas-csv?oficinaId=` — mismos datos y reglas que **`rpt-cajas-asignadas`**; **`text/csv; charset=utf-8`**, **`cajas-asignadas.csv`** (UTF-8 con BOM).
- **Cobro diario por gestor (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cobro-diario?usuarioId=&oficinaId=` — **`CREDITO.usp_RptCobroDiario`**; ambos obligatorios y deben coincidir con **`vendix:usuario_id`** y **`vendix:oficina_id`** del token (como `CreditoBL.ReporteCobroDiario`).
- **Cobro diario CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cobro-diario-csv?usuarioId=&oficinaId=` — mismos datos y reglas que **`rpt-cobro-diario`**; **`text/csv; charset=utf-8`**, **`cobro-diario.csv`** (UTF-8 con BOM).
- **Cobro diario CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cobro-diario-csv?usuarioId=&oficinaId=` — mismos datos y reglas que **`rpt-cobro-diario`**; **`text/csv; charset=utf-8`**, **`cobro-diario.csv`** (UTF-8 con BOM).
- **Cobro diario detalle por gestor (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cobro-diario-detalle?usuarioId=&oficinaId=` — **`CREDITO.usp_RptCobroDiarioDetalle`**; misma regla de token que `rpt-cobro-diario` (como `CreditoBL.ReporteCobranzaGestor`).
- **Cobro diario detalle CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cobro-diario-detalle-csv?usuarioId=&oficinaId=` — mismos datos y reglas que **`rpt-cobro-diario-detalle`**; **`text/csv; charset=utf-8`**, **`cobro-diario-detalle.csv`** (UTF-8 con BOM).
- **Cobro diario detalle CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-cobro-diario-detalle-csv?usuarioId=&oficinaId=` — mismos datos y reglas que **`rpt-cobro-diario-detalle`**; **`text/csv; charset=utf-8`**, **`cobro-diario-detalle.csv`** (UTF-8 con BOM).
- **Clientes bloqueados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-bloqueados?oficinaId=&usuarioId=` — **`CREDITO.usp_RptClientesBloqueados`**; ambos obligatorios y deben coincidir con **`vendix:oficina_id`** y **`vendix:usuario_id`** (como `CreditoBL.ReporteClientesBloqueados`; orden de parámetros del proc: oficina, usuario).
- **Clientes bloqueados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-bloqueados-csv?oficinaId=&usuarioId=` — mismos datos y reglas que **`rpt-clientes-bloqueados`**; **`text/csv; charset=utf-8`**, **`clientes-bloqueados.csv`** (UTF-8 con BOM).
- **Clientes bloqueados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-bloqueados-csv?oficinaId=&usuarioId=` — mismos datos y reglas que **`rpt-clientes-bloqueados`**; **`text/csv; charset=utf-8`**, **`clientes-bloqueados.csv`** (UTF-8 con BOM).
- **Clientes con tope de crédito (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-tope-credito?oficinaId=&usuarioId=` — **`CREDITO.usp_RptClientesTopeCredito`**; misma regla de token que clientes bloqueados (`CreditoBL.ReporteClientesTopeCredito`).
- **Clientes tope crédito CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-tope-credito-csv?oficinaId=&usuarioId=` — mismos datos y reglas que **`rpt-clientes-tope-credito`**; **`text/csv; charset=utf-8`**, **`clientes-tope-credito.csv`** (UTF-8 con BOM).
- **Clientes tope crédito CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-tope-credito-csv?oficinaId=&usuarioId=` — mismos datos y reglas que **`rpt-clientes-tope-credito`**; **`text/csv; charset=utf-8`**, **`clientes-tope-credito.csv`** (UTF-8 con BOM).
- **Avales por persona (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-aval?personaId=` — **`CREDITO.usp_RptAval`**; `personaId` obligatorio (≥ 1), como `CreditoBL.ReporteAval`.
- **Avales por persona CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-aval-csv?personaId=` — mismos datos que **`rpt-aval`**; **`text/csv; charset=utf-8`**, **`aval-persona.csv`** (UTF-8 con BOM).
- **Avales por persona CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-aval-csv?personaId=` — mismos datos que **`rpt-aval`**; **`text/csv; charset=utf-8`**, **`aval-persona.csv`** (UTF-8 con BOM).
- **Saldos de caja diario / chica (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldos-caja?cajaDiarioId=` — **`CREDITO.usp_RptSaldosCaja`**; `cajaDiarioId` obligatorio; **`indCajaChica`** opcional (default `false`, caja normal; `true` para caja chica, como en `CajaDiarioBL` / `CajaChicaDiarioBL`).
- **Saldos de caja CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldos-caja-csv?cajaDiarioId=` — mismos datos y reglas que **`rpt-saldos-caja`**; **`text/csv; charset=utf-8`**, **`saldos-caja.csv`** (UTF-8 con BOM).
- **Saldos de caja CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldos-caja-csv?cajaDiarioId=` — mismos datos y reglas que **`rpt-saldos-caja`**; **`text/csv; charset=utf-8`**, **`saldos-caja.csv`** (UTF-8 con BOM).
- **Saldo de cuenta por caja diario (JWT, política `CreditoUser`):** `GET /api/v1/credito/obtener-saldo-cuenta-caja-diario?cajaDiarioId=` — **`CREDITO.usp_ObtenerSaldoCuentaCajadiario`**; `cajaDiarioId` obligatorio; **`tipoPagoId`** opcional (default **1**, como `CajaDiarioBL.ObtenerSaldoCuentaCajadiario`). Respuesta JSON **`saldo`** (`decimal` o `null`).
- **Resumen de ingresos caja diario (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldos-caja-resumen-ingreso?cajaDiarioId=&oficinaId=` — **`CREDITO.usp_RptSaldosCajaResumenIngreso`**; ambos obligatorios; **`oficinaId`** debe coincidir con **`vendix:oficina_id`** (texto para RDLC; primera fila como `CajaDiarioBL.ObtenerResumenIngresoCajaDiario`). Respuesta **`texto`** (`string` o `null`).
- **Resumen saldos caja por tipo de cuenta (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-saldos-caja-resumen-tipo-cuenta?oficinaId=` — **`CREDITO.usp_RptSaldosCajaResumenTipoCuenta`**; `oficinaId` obligatorio y debe coincidir con **`vendix:oficina_id`** (texto para RDLC; primera fila como `CajaDiarioBL.ObtenerResumenCuentaCajaDiarios`). Respuesta **`texto`** (`string` o `null`).
- **Movimiento de bóveda (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-boveda?bovedaId=` — **`CREDITO.usp_RptMovimientoBoveda`**; `bovedaId` obligatorio; la fila en **`CREDITO.Boveda`** debe existir y su **`OficinaId`** debe coincidir con **`vendix:oficina_id`** (404 / 403 si no). Lista como `BovedaBL.ReporteMovimientoBoveda`.
- **Movimiento de bóveda CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-boveda-csv?bovedaId=` — mismos datos y reglas que **`rpt-movimiento-boveda`**; **`text/csv; charset=utf-8`**, **`movimiento-boveda.csv`** (UTF-8 con BOM).
- **Movimiento de bóveda CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-boveda-csv?bovedaId=` — mismos datos y reglas que **`rpt-movimiento-boveda`**; **`text/csv; charset=utf-8`**, **`movimiento-boveda.csv`** (UTF-8 con BOM).
- **Resumen cuenta bóveda (JWT, política `CreditoUser`):** `GET /api/v1/credito/resumen-cuenta-boveda?bovedaId=` — **`CREDITO.usp_ResumenCuentaBoveda`**; misma regla de bóveda/oficina que `rpt-movimiento-boveda`. Respuesta **`texto`** (`string` o `null`, primera fila como `BovedaBL.ResumenCuentaBoveda`).
- **Métricas de vencimiento por crédito (JWT, política `CreditoUser`):** `GET /api/v1/credito/metricas-vencimiento-credito?creditoId=` — **`CREDITO.uspCreditoVencido`**; `creditoId` obligatorio; **`CREDITO.Credito.OficinaId`** debe coincidir con **`vendix:oficina_id`**. JSON `creditoVencido`, `vencidoMenor60`, `vencidoMayor60`, `vencidoIrrecuperable` (como `BovedaBL.CreditoVencido`).
- **Central de riesgos generar (JWT, política `CreditoUser`):** `GET /api/v1/credito/central-riesgo-generar?oficinaId=&anio=&mes=` — **`CREDITO.usp_CentralRiesgoGenerar`**; los tres parámetros obligatorios (`anio` 1900–2100, `mes` 1–12); **`oficinaId`** debe coincidir con **`vendix:oficina_id`**. Lista como `ReporteBL.ListarReporteCentralRiesgo` (sin `oficinaId` null para “todas”).
- **Central de riesgos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/central-riesgo-generar-csv?oficinaId=&anio=&mes=` — mismos datos y reglas que **`central-riesgo-generar`**; **`text/csv; charset=utf-8`**, **`central-riesgo-generar.csv`** (UTF-8 con BOM).
- **Central de riesgos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/central-riesgo-generar-csv?oficinaId=&anio=&mes=` — mismos datos y reglas que **`central-riesgo-generar`**; **`text/csv; charset=utf-8`**, **`central-riesgo-generar.csv`** (UTF-8 con BOM).
- **Reporte de créditos (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito?oficinaId=&fechaIni=&fechaFin=&estadoCredito=` — **`CREDITO.usp_RptCredito`**; fechas y estado obligatorios; **`oficinaId`** = **`vendix:oficina_id`**. **`gestorId`** opcional; si se envía debe coincidir con **`vendix:usuario_id`** (equivalente a filtrar por gestor en el MVC). Sin modo “todas las oficinas / todos los gestores” desde la API.
- **Reporte de créditos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-csv?oficinaId=&fechaIni=&fechaFin=&estadoCredito=` — mismos datos y reglas que **`rpt-credito`**; **`text/csv; charset=utf-8`**, **`reporte-credito.csv`** (UTF-8 con BOM).
- **Reporte de créditos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-csv?oficinaId=&fechaIni=&fechaFin=&estadoCredito=` — mismos datos y reglas que **`rpt-credito`**; **`text/csv; charset=utf-8`**, **`reporte-credito.csv`** (UTF-8 con BOM).
- **Morosidad de créditos (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-morosidad?oficinaId=&hastaFecha=&diasAtrazoIni=&diasAtrazoFin=` — **`CREDITO.usp_RptCreditoMorosidad`**; todos obligatorios; **`oficinaId`** = **`vendix:oficina_id`**; **`diasAtrazoIni`** ≤ **`diasAtrazoFin`**. Equivale a **`CreditoBL.ReporteCreditoMorosidad`** / **`ReporteController.ReporteCreditoMorosidad`** (`pFechaHasta` → `hastaFecha`; sin `pOficinaId` null).
- **Morosidad de créditos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-morosidad-csv?oficinaId=&hastaFecha=&diasAtrazoIni=&diasAtrazoFin=` — mismos datos y reglas que **`rpt-credito-morosidad`**; **`text/csv; charset=utf-8`**, **`credito-morosidad.csv`** (UTF-8 con BOM).
- **Morosidad de créditos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-morosidad-csv?oficinaId=&hastaFecha=&diasAtrazoIni=&diasAtrazoFin=` — mismos datos y reglas que **`rpt-credito-morosidad`**; **`text/csv; charset=utf-8`**, **`credito-morosidad.csv`** (UTF-8 con BOM).
- **Rentabilidad de créditos (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-rentabilidad?oficinaId=&fechaIni=&fechaFin=&estadoCredito=` — **`CREDITO.usp_RptCreditoRentabilidad`**; el proc usa el parámetro **`OficnaId`** (typo del EDMX); **`oficinaId`** en query = oficina del token. No replica **`indTodos`** del MVC (fechas siempre explícitas).
- **Rentabilidad de créditos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-rentabilidad-csv?oficinaId=&fechaIni=&fechaFin=&estadoCredito=` — mismos datos y reglas que **`rpt-credito-rentabilidad`**; **`text/csv; charset=utf-8`**, **`credito-rentabilidad.csv`** (UTF-8 con BOM).
- **Rentabilidad de créditos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-rentabilidad-csv?oficinaId=&fechaIni=&fechaFin=&estadoCredito=` — mismos datos y reglas que **`rpt-credito-rentabilidad`**; **`text/csv; charset=utf-8`**, **`credito-rentabilidad.csv`** (UTF-8 con BOM).
- **Créditos aprobados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-aprobacion?oficinaId=&fechaAprobacion=` — **`CREDITO.usp_RptCreditoAprobacion`**; **`fechaAprobacion`** (= **`pFecha`** MVC) y **`oficinaId`** obligatorios; **`oficinaId`** = **`vendix:oficina_id`**. **`usuarioId`** opcional; si se envía debe = **`vendix:usuario_id`**. Equivale a **`CreditoBL.ReporteCreditoAprobacion`** (sin nulls “todos” en oficina/usuario).
- **Créditos aprobados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-aprobacion-csv?oficinaId=&fechaAprobacion=` — mismos datos y reglas que **`rpt-credito-aprobacion`**; **`text/csv; charset=utf-8`**, **`credito-aprobacion.csv`** (UTF-8 con BOM).
- **Créditos aprobados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-aprobacion-csv?oficinaId=&fechaAprobacion=` — mismos datos y reglas que **`rpt-credito-aprobacion`**; **`text/csv; charset=utf-8`**, **`credito-aprobacion.csv`** (UTF-8 con BOM).
- **Créditos activos (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-activos?oficinaId=&fechaIni=&fechaFin=` — **`CREDITO.usp_RptCreditosActivos`**; **`usuarioId`** opcional con la misma regla que aprobación. Equivale a **`CreditoBL.ReporteCreditoActivo`** / **`ReporteController.ReporteCreditoActivo`**.
- **Créditos activos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-activos-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-creditos-activos`**; **`text/csv; charset=utf-8`**, **`creditos-activos.csv`** (UTF-8 con BOM).
- **Créditos activos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-activos-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-creditos-activos`**; **`text/csv; charset=utf-8`**, **`creditos-activos.csv`** (UTF-8 con BOM).
- **Créditos cierres (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-cierres?oficinaId=&fechaIni=&fechaFin=` — **`CREDITO.usp_RptCreditosCierres`**; misma auth que activos. Equivale a **`CreditoBL.ReporteCreditoCierre`** / **`ReporteController.ReporteCreditoCierre`**.
- **Créditos cierres CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-cierres-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-creditos-cierres`**; **`text/csv; charset=utf-8`**, **`creditos-cierres.csv`** (UTF-8 con BOM).
- **Créditos cierres CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-cierres-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-creditos-cierres`**; **`text/csv; charset=utf-8`**, **`creditos-cierres.csv`** (UTF-8 con BOM).
- **Créditos morosos pagados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-morosos-pagados?oficinaId=&fechaIni=&fechaFin=` — **`CREDITO.usp_RptCreditosMorososPagados`** (SQL: `UsuarioId`, `OficinaId`, `FechaInicio`, `FechaFin`); misma auth que cierres. Equivale a **`CreditoBL.ReporteCreditoMorosoPagado`** / **`ReporteController.ReporteCreditoMorosoPagado`**.
- **Créditos morosos pagados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-morosos-pagados-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-creditos-morosos-pagados`**; **`text/csv; charset=utf-8`**, **`creditos-morosos-pagados.csv`** (UTF-8 con BOM).
- **Créditos morosos pagados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-creditos-morosos-pagados-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-creditos-morosos-pagados`**; **`text/csv; charset=utf-8`**, **`creditos-morosos-pagados.csv`** (UTF-8 con BOM).
- **Clientes inactivos (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-inactivos?oficinaId=` — **`CREDITO.usp_RptClientesInactivos`**; **`fechaIni`** / **`fechaFin`** opcionales (juntas o ninguna; paridad `ReporteClientesInactivos` MVC sin fechas). **`usuarioId`** opcional: TODOS solo roles elevados; cajero sin `usuarioId` usa el del JWT.
- **Clientes inactivos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-clientes-inactivos-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-clientes-inactivos`**; **`text/csv; charset=utf-8`**, **`clientes-inactivos.csv`** (UTF-8 con BOM).
- **Reporte caja diario (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-caja-diario?oficinaId=&fechaIni=&fechaFin=` — **`CREDITO.usp_RptCajaDiario`** (SQL: `UsuarioId`, `OficinaId`, `FechaInicio`, `FechaFin`); lista resumen por caja diario como **`ReporteController.ReporteCajaDiario`**. Distinto de **`rpt-cajas-asignadas`** (otro proc).
- **Reporte caja diario CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-caja-diario-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-caja-diario`**; **`text/csv; charset=utf-8`**, **`caja-diario.csv`** (UTF-8 con BOM).
- **Créditos vencidos — listado proc (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-vencido?oficinaId=` — **`CREDITO.usp_RptCreditoVencido`**; query opcional `vencidoMenor60`, `vencidoMayor60`, `vencidoIrrecuperable` (`S`/`N`, como **`Boveda/Index`**). **`oficinaId`** debe coincidir con el token; el procedimiento **no** filtra por oficina (paridad con **`ReporteCreditoVencido`** del MVC).
- **Créditos vencidos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-vencido-csv?oficinaId=` — mismos datos y reglas que **`rpt-credito-vencido`**; **`text/csv; charset=utf-8`**, **`credito-vencido.csv`** (UTF-8 con BOM).
- **Comprobantes / movimientos caja anulados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-caja-anulado?oficinaId=&fechaIni=&fechaFin=` — **`CREDITO.usp_RptMovimientoCajaAnulado`**; mismo origen de datos que **`ReporteController.ReporteComprobantesCajaAnulados`**. **`oficinaId`** = token; el SP no incluye oficina.
- **Movimientos caja anulados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-caja-anulado-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-movimiento-caja-anulado`**; **`text/csv; charset=utf-8`**, **`movimiento-caja-anulado.csv`** (UTF-8 con BOM).
- **Reporte caja diario CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-caja-diario-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-caja-diario`**; **`text/csv; charset=utf-8`**, **`caja-diario.csv`** (UTF-8 con BOM).
- **Créditos vencidos — listado proc (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-vencido?oficinaId=` — **`CREDITO.usp_RptCreditoVencido`**; query opcional `vencidoMenor60`, `vencidoMayor60`, `vencidoIrrecuperable` (`S`/`N`, como **`Boveda/Index`**). **`oficinaId`** debe coincidir con el token; el procedimiento **no** filtra por oficina (paridad con **`ReporteCreditoVencido`** del MVC).
- **Créditos vencidos CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-credito-vencido-csv?oficinaId=` — mismos datos y reglas que **`rpt-credito-vencido`**; **`text/csv; charset=utf-8`**, **`credito-vencido.csv`** (UTF-8 con BOM).
- **Comprobantes / movimientos caja anulados (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-caja-anulado?oficinaId=&fechaIni=&fechaFin=` — **`CREDITO.usp_RptMovimientoCajaAnulado`**; mismo origen de datos que **`ReporteController.ReporteComprobantesCajaAnulados`**. **`oficinaId`** = token; el SP no incluye oficina.
- **Movimientos caja anulados CSV (JWT, política `CreditoUser`):** `GET /api/v1/credito/rpt-movimiento-caja-anulado-csv?oficinaId=&fechaIni=&fechaFin=` — mismos datos y reglas que **`rpt-movimiento-caja-anulado`**; **`text/csv; charset=utf-8`**, **`movimiento-caja-anulado.csv`** (UTF-8 con BOM).
- **Reporte de stock por oficina (JWT, política `CreditoUser`):** `GET /api/v1/almacen/reporte-stock?oficinaId=` — **`ALMACEN.usp_ReporteStock`**; `oficinaId` obligatorio y debe coincidir con **`vendix:oficina_id`** del token.
- **Reporte de stock CSV/PDF (JWT):** `GET /api/v1/almacen/reporte-stock-csv?oficinaId=`, `GET /api/v1/almacen/reporte-stock-pdf?oficinaId=` — mismos datos y reglas que JSON; PDF tabular (columnas = CSV).
- **Kardex (JWT, política `CreditoUser`):** `GET /api/v1/almacen/generar-kardex?oficinaId=&articuloId=&almacenId=` — **`ALMACEN.usp_GenerarKardex`**; el almacén debe pertenecer a la oficina del token. Equivale a **`KardexController.ListarKardex`**.
- **Kardex CSV/PDF (JWT):** `GET /api/v1/almacen/generar-kardex-csv?...`, `GET /api/v1/almacen/generar-kardex-pdf?...` — mismos datos y reglas que JSON.
- **Serie en línea kardex (JWT):** `GET /api/v1/almacen/serie-kardex?oficinaId=&movimientoDetalleId=` — **`ALMACEN.usp_ListarSerieKardex`**; `indStock` opcional (default false). Primera fila en `texto`.
- **Validar series (JWT):** `GET /api/v1/almacen/existe-serie-articulo?oficinaId=&listaSerie=` — **`ALMACEN.usp_ExisteSerieArticulo`**; `cantidad` e `indCorrelativo` opcionales. Como **`EntradaController.ValidarExisteSerie`**.
- **Reporte de stock CSV/PDF (JWT):** `GET /api/v1/almacen/reporte-stock-csv?oficinaId=`, `GET /api/v1/almacen/reporte-stock-pdf?oficinaId=` — mismos datos y reglas que JSON; PDF tabular (columnas = CSV).
- **Kardex (JWT, política `CreditoUser`):** `GET /api/v1/almacen/generar-kardex?oficinaId=&articuloId=&almacenId=` — **`ALMACEN.usp_GenerarKardex`**; el almacén debe pertenecer a la oficina del token. Equivale a **`KardexController.ListarKardex`**.
- **Kardex CSV/PDF (JWT):** `GET /api/v1/almacen/generar-kardex-csv?...`, `GET /api/v1/almacen/generar-kardex-pdf?...` — mismos datos y reglas que JSON.
- **Serie en línea kardex (JWT):** `GET /api/v1/almacen/serie-kardex?oficinaId=&movimientoDetalleId=` — **`ALMACEN.usp_ListarSerieKardex`**; `indStock` opcional (default false). Primera fila en `texto`.
- **Validar series (JWT):** `GET /api/v1/almacen/existe-serie-articulo?oficinaId=&listaSerie=` — **`ALMACEN.usp_ExisteSerieArticulo`**; `cantidad` e `indCorrelativo` opcionales. Como **`EntradaController.ValidarExisteSerie`**.

- **Correlación:** cabecera de respuesta `X-Correlation-ID` (se acepta la enviada en la petición o se genera una).

- **Forwarded headers:** `Hosting:ForwardedHeaders:Enabled` y `KnownProxies` (IPs del balanceador). Si `Enabled=true`, hace falta al menos una IP válida; ver `docs/migration/MODERN-E2E-LOGIN-MENU.md`.
- **CORS:** política `browser` desde `BrowserCors:AllowedOrigins`. En **Development** con lista vacía se permite cualquier origen; en el resto de entornos, sin orígenes configurados **no** hay origen permitido hasta que rellenes la lista (típico SPA en prod).
- **Rate limiting:** `POST /api/v1/auth/login` usa la política `auth-login` (por IP; `RateLimiting:LoginPermitLimit`, `LoginWindowSeconds`; `429` con cuerpo JSON). `POST /api/v1/auth/refresh` usa `auth-refresh` con **`RefreshPermitLimit`** y **`RefreshWindowSeconds`**. `RateLimiting:Disabled=true` solo para tests o diagnóstico.
- **Producción:** plantilla `appsettings.Production.json` (`Auth:RequerirClienteAcceso` en `true` por defecto). Secretos vía user-secrets o variables de entorno (`Jwt__*`, `CreditoDatabase__ConnectionString`).

### Guías de migración

- Patrón **strangler fig** y mapa de rebanadas: `docs/migration/STRANGLER-MIGRATION.md`.
- Cierre E2E login + menú + decisiones `MAESTRO.Acceso`: `docs/migration/MODERN-E2E-LOGIN-MENU.md`.
- Plan de dejar de usar clave en claro: `docs/migration/PASSWORD-STORAGE-ROADMAP.md`.
- Proxy `/api/v1` y backlog auth MVC: `docs/migration/PROXY-AND-MVC-AUTH-BACKLOG.md`.
- Proxy `/api/v1` y backlog auth MVC: `docs/migration/PROXY-AND-MVC-AUTH-BACKLOG.md`.

### Cadena de conexión

No commitear secretos. Desde la carpeta **`modern`** (donde está la solución), el proyecto se indica con la ruta al `.csproj`:

```powershell
cd D:\GitHub\Credito\modern
dotnet user-secrets set "CreditoDatabase:ConnectionString" "Server=.\MSSQLSERVER01;Database=CREDITO;User Id=sa;Password=AQUI_TU_CLAVE;" --project .\Credito.Modern.Api\Credito.Modern.Api.csproj
```

**O** entra en la carpeta del API (solo hay un `.csproj`) y omite `--project`:

```powershell
cd D:\GitHub\Credito\modern\Credito.Modern.Api
dotnet user-secrets set "CreditoDatabase:ConnectionString" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True"
```

No pegues en PowerShell el texto explicativo del chat (líneas que empiezan por “Sustituye…”, “Por qué…”, etc.): PowerShell intentará ejecutarlas como comandos.

O variables de entorno: `CreditoDatabase__ConnectionString`.

**JWT (HS256):** en `appsettings.Development.json` hay una clave solo para local; en **producción** define `Jwt__SigningKey` (≥ 32 bytes UTF-8), `Jwt__Issuer`, `Jwt__Audience` (access), `Jwt__RefreshAudience`, opcionalmente `Jwt__AccessTokenLifetimeHours` (1–168), `Jwt__RefreshTokenLifetimeDays` (1–90) y **`Jwt__RefreshTokenVersion`** (entero ≥ 1; súbelo para invalidar masivamente refresh tokens antiguos). Sin clave válida la aplicación no arranca (`ValidateOnStart`).

**Auth (login legado):** `Auth__RequerirClienteAcceso` (por defecto `true` en `appsettings.json`) replica la comprobación de `tk` contra `MAESTRO.Acceso`. En Development local está en `false` para poder probar sin fila de acceso. **`Auth__MigracionClavePerezosa`:** si es `true`, tras un login exitoso con clave en claro en `MAESTRO.Usuario.ClaveUsuario`, la API moderna la sustituye por hash PBKDF2 con prefijo `$pbk2$` (misma columna). Por defecto `false`. El MVC (`Web/HomeController.Autenticar`) acepta el mismo hash tras desplegar `Web` con `UsuarioPasswordHasherCompat`. **`Auth__MigracionClavePerezosa`:** si es `true`, tras un login exitoso con clave en claro en `MAESTRO.Usuario.ClaveUsuario`, la API moderna la sustituye por hash PBKDF2 con prefijo `$pbk2$` (misma columna). Por defecto `false`. El MVC (`Web/HomeController.Autenticar`) acepta el mismo hash tras desplegar `Web` con `UsuarioPasswordHasherCompat`.

**Menú / dev token:** `Menu__PermiteParametrosQuery` (por defecto `false` en plantilla base; `true` en Development). `Hosting__AllowDevToken` debe ser `true` para exponer `POST /api/v1/dev/token` en Development.

La sección en `appsettings` es `CreditoDatabase:ConnectionString` (ver `SqlDatabaseOptions.SectionName`).

## CI

Workflow: `.github/workflows/credito-modern-ci.yml` (disparo al cambiar `modern/`). Incluye `docker build` con contexto `modern/` y `Dockerfile` de esa carpeta. Si el remoto es un monorepo cuyo raíz no es esta carpeta, mueve el workflow a la raíz del repo y ajusta `defaults.run.working-directory` y `on.paths`.

## Contenedor

Desde `Credito/modern`:

```powershell
docker build -t credito-modern:local .
docker run --rm -p 8080:8080 -e CreditoDatabase__ConnectionString="..." -e Jwt__SigningKey="..." -e Jwt__Audience="..." -e Jwt__RefreshAudience="..." credito-modern:local
```

Variables de entorno típicas: `CreditoDatabase__ConnectionString`, `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__RefreshAudience` (y secretos según entorno).

### Proxy strangler (staging local)

Guía completa: [docs/migration/PHASE-3B-PROXY-E2E.md](../docs/migration/PHASE-3B-PROXY-E2E.md).

```powershell
cd Credito\modern
Copy-Item deploy\.env.example deploy\.env
# Editar deploy\.env (SQL + JWT_SIGNING_KEY)
.\deploy\scripts\start-strangler.ps1 -Build

# E2E local (MVC F5 + proxy + verify + smoke):
.\deploy\scripts\run-local-strangler-e2e.ps1

# O por pasos:
.\deploy\scripts\verify-strangler-proxy.ps1
.\deploy\scripts\smoke-strangler-proxy.ps1
# Código de artículo real (opcional): -CodigoArticulo "TU-CODIGO"
```

- **9080** — nginx: `/api/v1/*`, `/health`, `/swagger` → API; `/` → MVC (`host.docker.internal`, puerto configurable en `default.conf`; coincide con **DevelopmentServerPort** o **IISUrl** del proyecto Web).
- **5080** — API directa (depuración).
- Tras cambiar volúmenes en compose o ficheros nginx, usar **`docker compose ... up -d`** (no solo `restart`) para recrear el contenedor proxy si hace falta.
- Entorno **`Staging`**: `appsettings.Staging.json` (`AllowDevToken`, CORS `localhost:9080`).
- **Producción:** plantillas `deploy/iis-arr-web.config.example`, `deploy/nginx/production.conf.example`, `deploy/.env.production.example`; validar con `.\deploy\scripts\verify-production-config.ps1` (PHASE-3B iter. 66).
- Si rutas nuevas devuelven **404** tras pull: `docker compose ... up -d --build credito-modern-api`.

## Fase 5 — Interfaz (SPA)

Migración **backend** cerrada en repo — [docs/migration/MIGRATION-CLOSURE.md](../docs/migration/MIGRATION-CLOSURE.md). Siguiente trabajo: **UI** consumiendo esta API.

| Documento | Contenido |
|-----------|-----------|
| [PHASE-5-UI-START.md](../docs/migration/PHASE-5-UI-START.md) | Hub Fase 5 (subfases 5.0–5.6) |
| [PHASE-5-UI-STACK-OPTIONS.md](../docs/migration/PHASE-5-UI-STACK-OPTIONS.md) | React/Vue/Blazor, SPA vs BFF |
| [PHASE-5-UI-ARCHITECTURE.md](../docs/migration/PHASE-5-UI-ARCHITECTURE.md) | Auth, cliente HTTP, carpetas |
| [PHASE-5-UI-ROADMAP.md](../docs/migration/PHASE-5-UI-ROADMAP.md) | MVC → pantallas (29 controladores) |
| [PHASE-5-OPERATIONS-CUTOVER.md](../docs/migration/PHASE-5-OPERATIONS-CUTOVER.md) | Preprod/prod (paralelo) |

**Recomendación:** React + TypeScript + Vite en `Credito.Modern.Web`; `VITE_API_BASE_URL=http://localhost:9080/api/v1` en desarrollo.

## Operaciones (paralelo a UI)

- Desplegar proxy en preprod/prod: [PHASE-3B-PROXY-E2E.md](../docs/migration/PHASE-3B-PROXY-E2E.md), [PHASE-5-OPERATIONS-CUTOVER.md](../docs/migration/PHASE-5-OPERATIONS-CUTOVER.md).
- Fase 4 RDLC solo si negocio exige layout idéntico: [PHASE-4-START.md](../docs/migration/PHASE-4-START.md).
