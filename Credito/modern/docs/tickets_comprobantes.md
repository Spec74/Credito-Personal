# Tickets / comprobantes térmicos (QuestPDF)

**Estado:** completo · 2026-11-09 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md) · SSD-03 · SSD-04  
**Layout compartido:** `MovimientoCajaTicketPdfDocument` (ancho ~226 pt, paridad `TicketCaja` RDLC sin ReportViewer)

Hay **tres** tickets térmicos en el moderno. El resto de PDF (informes, saldos, constancias, prendario) son reportes de página completa, no tickets.

## Catálogo

| Dominio | Endpoint | Lectura | UI | Título PDF |
|---------|----------|---------|----|------------|
| Caja diario | `GET /api/v1/credito/movimiento-caja-ticket-pdf?oficinaId&movimientoCajaId` | `MovimientoCajaTicketReadService` | Arqueo, Movimientos, Cobro bloque (Enviados), Consulta crédito (cuota PAG) | `COMPROBANTE DE CAJA` |
| Bóveda | `GET /api/v1/credito/movimiento-boveda-ticket-pdf?movimientoBovedaId` | `MovimientoBovedaTicketReadService` | Historial bóveda, Movimiento bóveda | `COMPROBANTE DE BÓVEDA` |
| Caja chica | `GET /api/v1/credito/movimiento-caja-chica-ticket-pdf?movimientoCajaChicaId` | `MovimientoCajaChicaTicketReadService` | API viva; UI `/caja/chica` retirada | `COMPROBANTE DE CAJA CHICA` |

Política: `CreditoUser`. Caja exige `oficinaId` = JWT y scope del movimiento. Caja chica exige dueño de sesión chica abierta.

## Campos que debe mostrar (as-built 2026-11-08/09)

### Caja

| Campo | Origen |
|-------|--------|
| Oficina | `MAESTRO.Oficina` vía caja diario |
| Usuario | `UsuarioRegId` del movimiento |
| Medio | `TipoPagoId` → `ValorTabla` 13 + `DisplayLabel` (Yape/Interbank Gerente·René, BCP, Efectivo…) |
| Cliente / Persona | Persona del movimiento (si aplica) |
| Producto | Producto del crédito + `CREDITO {id}` en CUO/libre; `CAJA DIARIO · {Op}` en E/S |
| Concepto | PAGO CUOTA / LIBRE / INICIAL / CONTADO / ENTRADA / SALIDA |
| Desglose CUO | Saldo anterior, deuda, interés, mora, cuotas, capital… |
| Entrada / Salida | En layouts simples de E/S |

### Bóveda

| Campo | Origen |
|-------|--------|
| Oficina | Bóveda → oficina (no placeholder) |
| Usuario / agente | Persona de caja asignada o `UsuarioRegId` |
| Medio | `TipoPagoId` tipado (DisplayLabel) |
| Cliente | Etiqueta fija `BÓVEDA` (movimiento no es cobro de cliente) |
| Producto | `Operación {CodOperacion} (Activo\|Anulado)` |
| Concepto | Entrada / Salida |
| Glosa | `BovedaMov.Glosa` |
| Entrada / Salida + Importe | Según `IndEntrada` |

Ejemplo válido (operación):

```
COMPROBANTE DE BÓVEDA
Mov. N° 23
Oficina: OFICINA PRINCIPAL
Fecha: …
Usuario: …
Medio: BCP
Cliente: BÓVEDA
Producto: Operación TRE (Activo)
Concepto: Entrada
Glosa: …
Entrada / Importe: 350.00
```

### Caja chica

| Campo | Origen |
|-------|--------|
| Oficina | `UsuarioOficina` del dueño de sesión (fallback `PRINCIPAL` solo si no hay vínculo) |
| Usuario | `UsuarioRegId` del movimiento |
| Persona / Cliente | `MovimientoCajaChica.PersonaId` |
| Producto | `CAJA CHICA · {Operacion}` |
| Entrada / Salida | Según `IndEntrada` |
| Medio | N/A (tabla sin `TipoPagoId`) |

## Legacy corregido (ya no)

| Antes (legacy / early modern) | Ahora |
|-------------------------------|-------|
| Bóveda: oficina fija / usuario `SISTEMA` | Oficina y agente reales |
| Caja: sin medio tipado en PDF | `Medio:` con DisplayLabel |
| Caja chica: `Oficina = PRINCIPAL` siempre | Oficina desde `UsuarioOficina` |
| Columna Tipo pago 140 px → «Interbank Geren…» | Ancho ≥ 180–200 + `TipoPagoCell` |

## Pruebas

- `MovimientoCajaTicketPdfDocumentTests`
- `MovimientoCajaTicketEndpointTests` / `MovimientoCajaChicaTicketEndpointTests` (401)
- `reportpdfendpointsunauthorizedtests` (rutas ticket en lista)

## Smoke operativo

1. Bóveda → Historial → Ticket en TRE/TRS/OTE (medio BCP/Yape/Efectivo).
2. Caja diario → Arqueo o Movimientos → Ticket CUO e INI (medio visible).
3. Cobro bloque → Enviados → Imprimir.
4. Consulta crédito → cuota PAG → enlace ticket.
