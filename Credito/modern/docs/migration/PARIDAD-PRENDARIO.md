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

- **Contrato prendario**: titular y cónyuge, detalle de bienes, importes en números y letras.
  El departamento/provincia se leen del catálogo, no se asumen Ayacucho.
- **Acta de entrega**: mismo encabezado y detalle de bienes, con lugar y fecha en texto.

`NumeroContrato` cae al `CreditoId` cuando `NumeroContratoPrendario` es nulo.

Las cláusulas fijas (`ClausulasPrendario.pdf`) aún no se anexan al PDF del contrato.

## Notificaciones

`ObtenerCreditosPrendariosPorVencer(diasAntes)` alimenta el aviso por WhatsApp de créditos
prendarios próximos a vencer.

## Acceso

Solo el rol `ANALISTA` (`RolId = 6`). En la base se concede con `MAESTRO.RolMenu` sobre los menús
`PRENDARIO - Listado` y `PRENDARIO - Nuevo`; en la SPA, `/credito/prendario` está en
`EXACT_MENU_ROUTES` para que el hub de crédito no alcance a habilitarla.

En el legacy, `PRENDARIO - Nuevo` (`Prendario/Create`) redirige a `Cliente/Mantener` con `id = 0`,
es decir, arranca creando el cliente.
