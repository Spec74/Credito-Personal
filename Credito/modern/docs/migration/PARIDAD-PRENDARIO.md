# Paridad funcional: módulo de crédito prendario

Reglas de negocio extraídas del legacy para que el sistema moderno se comporte igual.

**Fuente:** repositorio `https://github.com/RichardZC/Credito`, rama `Prendario-Eber`
(3 commits sobre `master`). El módulo no existe en `master` ni en las demás ramas.

Archivos de referencia:

| Archivo | Contenido |
| --- | --- |
| `Web/Controllers/Prendario/PrendarioController.cs` | Listado, resumen, gestión, solicitud, generación |
| `ITB.VENDIX.BL/Creditos/CreditoBL.cs` | `CrearSolicitudCreditoPrendario`, `GuardarPrendario`, datos de contrato y acta |
| `ITB.VENDIX.BL/PrendaBL.cs` | `PrendaDto`, listado por crédito |
| `Web/Views/Prendario/Index.cshtml` | Listado con tarjetas de resumen |
| `Web/Views/Prendario/Gestionar.cshtml` | Página única de gestión (439 líneas) |
| `Web/Reporte/rptContratoPrendario.rdlc` | Contrato |
| `Web/Reporte/rptActaEntregaPrendario.rdlc` | Acta de entrega |
| `Web/Reporte/ClausulasPrendario.pdf` | Cláusulas fijas anexadas al contrato |

## Identificación de un crédito prendario

`ProductoId = 2` (CREDI PRENDARIO) más el indicador `Credito.EsPrendario = 1`. El listado acepta
cualquiera de los dos (`EsPrendario = 1 OR ProductoId = 2`) porque hay créditos antiguos con el
producto pero sin el indicador.

## Alta de solicitud

`CrearSolicitudCreditoPrendario(personaId)` crea el crédito en estado `CRE` con estos valores
fijos, que el analista ajusta después en el simulador:

| Campo | Valor |
| --- | --- |
| `ProductoId` | 2 |
| `EsPrendario` | 1 |
| `Descripcion` | `CREDITO PRENDARIO` |
| `TipoCuota` | `F` |
| `FormaPago` | `M` |
| `MontoCredito` | 500 |
| `MontoProducto`, `MontoInicial` | 0 |
| `TipoGastoAdm` | `CAP` |
| `MontoGastosAdm` | `CalcularGastosAdm(500, true)` |
| `NumeroCuotas` | 1 |
| `Interes` | 8 |
| `Calificacion` | `A` |
| `FechaPrimerPago`, `FechaVencimiento` | fecha del servidor + 1 mes |
| `Estado` | `CRE` |

El aval se hereda: se toma el `PersonaAvalId` del crédito más reciente de esa persona cuyo estado
no sea `CRE` ni `ANU`.

La fecha es siempre la del servidor (`VendixGlobal.GetFecha()`, que resuelve a `dbo.ufnFecha`),
nunca la del cliente.

## Generación del crédito

`GenerarCredito` pasa el crédito de `CRE` a `PEN` reutilizando `CreditoBL.CrearCredito` con
`ProductoId = 2`, `TipoCuota = "F"` y `MontoInicial = 0`. El resto (monto, interés, modalidad,
cuotas, gastos administrativos, fecha de primer pago, central de riesgo) lo captura el analista.

## Guardado de bienes

`GuardarPrendario(creditoId, prendas, fechaRemate?)`, en una sola transacción:

1. Borra todas las filas de `Prenda` del crédito y reinserta. El formulario envía siempre el
   detalle completo, así que la semántica es de reemplazo, no de acumulación.
2. Descarta las prendas con `Descripcion` vacía.
3. Normaliza a mayúsculas y sin espacios sobrantes `Descripcion`, `Marca`, `Modelo`, `Color`.
   `Serie` vacía se guarda como `N/T`.
4. `Estado = 'EN CUSTODIA'`, `FechaRegistro` = fecha del servidor.
5. Marca `Credito.EsPrendario = 1`.
6. `Credito.MontoTasacion` = suma de las tasaciones.
7. Si `NumeroContratoPrendario` está vacío, lo fija al `CreditoId` en texto.
8. `Credito.FechaRemate` = la recibida, o `FechaVencimiento + 30 días` si no se envía.

**Defecto del legacy a no replicar:** el paso 6 suma `pPrendas` completo, sin excluir las
prendas descartadas en el paso 2. Un renglón con tasación pero sin descripción infla
`MontoTasacion` por encima de la suma de los bienes realmente guardados. En el moderno la suma
debe calcularse sobre las prendas efectivamente insertadas. Registrado en
`BITACORA-DESVIACIONES.md`.

## Listado

Búsqueda por nombre completo, número de documento o número de contrato. Orden por `CreditoId`
descendente. Columnas: crédito, contrato (`(pendiente)` si es nulo), documento, cliente,
tasación, monto del crédito, vencimiento, remate, estado, categoría, días al vencimiento y
celular.

Categoría calculada por fila, en este orden:

| Condición | Categoría |
| --- | --- |
| `EsPrendario = 0` | `SINBIENES` |
| `Estado <> 'DES'` | `OTRO` |
| `FechaRemate < hoy` | `REMATADO` |
| `FechaVencimiento < hoy` | `VENCIDO` |
| `FechaVencimiento <= hoy + 3 días` | `PORVENCER` |
| resto | `VIGENTE` |

`días` = `FechaVencimiento - hoy`, puede ser negativo.

En la columna Situación, `OTRO` no se muestra como «En trámite»: se usa el estado del
ciclo (`CRE` solicitud, `PEN` pendiente, `APR` aprobado, etc.). Vigente / vencido /
rematado solo aplican a `Estado = 'DES'`. Aprobado no es vigente hasta el desembolso en Caja.

## Tarjetas de resumen

Todas sobre `EsPrendario = 1 AND Estado = 'DES'`:

| Tarjeta | Definición |
| --- | --- |
| Total | todos |
| Por vencer | `FechaVencimiento` entre hoy y hoy + 3 días |
| Vencidos | `FechaVencimiento < hoy` y (`FechaRemate` nula o `>= hoy`) |
| Rematados | `FechaRemate` no nula y `< hoy` |

El resumen se restringe a `Estado = 'DES'` mientras el listado no lo hace; es deliberado, las
tarjetas miden cartera desembolsada.

## Reportes

Ambos exigen al menos un bien guardado; si no hay prendas responden 409 pidiendo guardar
primero. Formato A4 vertical con márgenes de 0.25 pulgadas.

- **Contrato prendario**: Anexo A (cliente, prendas, observaciones) y Anexo B (tasas,
  R.G.ADM 1 %, IGV 18 %, comisión de venta 5 %, montos en números y letras). El
  departamento/provincia se leen del catálogo, no se asumen Ayacucho.
- **Acta de entrega**: texto legal del RDLC (entrega voluntaria, declaración jurada,
  aceptación con día/mes/año y recuadro de huella).

`NumeroContrato` cae al `CreditoId` cuando `NumeroContratoPrendario` es nulo.

Las cláusulas oficiales (`ClausulasPrendario.pdf`) se anexan sin reescribirse. En la
última página se estampa `Ayacucho, {día} de {mes} de {año}`, el nombre y el DNI sobre
los blancos del PDF legal (`ReporteController.MergePdf` + sello). El archivo vive como
recurso embebido `ReportAssets/ClausulasPrendario.pdf`.

## Notificaciones

`GET /api/v1/prendario/avisos-vencimiento` equivale a `ObtenerCreditosPrendariosPorVencer`:
créditos `EsPrendario = 1`, `Estado = 'DES'`, que vencen exactamente en N días (por defecto 3)
y que todavía no tienen `FechaNotifWhatsapp3d` de hoy. El moderno acota además a la oficina
de la sesión.

El botón **WhatsApp** de gestión abre `wa.me` para un recordatorio puntual. El aviso
automático a 3 días usa WhatsApp Cloud API y la plantilla `aviso_vencimiento_prendario`
(`{{1}}` nombre, `{{2}}` fecha, `{{3}}` importe). Corre a las 08:00 hora de Lima y, en
desarrollo, una pasada al arrancar. `POST /avisos-vencimiento/enviar` permite dispararlo
a mano. El token no se guarda en Git (user-secrets / variables de entorno).

## Acceso

Solo el rol `ANALISTA` (`RolId = 6`). En la base se concede con `MAESTRO.RolMenu` sobre los menús
`PRENDARIO - Listado` y `PRENDARIO - Nuevo`; en la SPA, `/credito/prendario` está en
`EXACT_MENU_ROUTES` para que el hub de crédito no alcance a habilitarla.

En el legacy, `PRENDARIO - Nuevo` (`Prendario/Create`) redirige a `Cliente/Mantener` con `id = 0`.
En modern no hay alta paralela: se busca un cliente existente o se abre `/clientes/nuevo` (ApiPerú
y `guardarCliente`). Al guardar, vuelve a `/credito/prendario/nuevo?personaId=`.
