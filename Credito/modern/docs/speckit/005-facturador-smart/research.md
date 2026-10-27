# Research — 005-facturador-smart

**Fecha:** 2026-10-26  
**Fuente:** demo `https://empeno.facturadorsmart.pe` (login demo del proveedor, v 11.1026.0630).  
**Método:** sesión real, menú completo + ejercicio de Casa de empeño, dashboard, POS, cajas, CxC y alta de comprobante. No se registró préstamo ni se remató (el dinero sale de la caja abierta del demo).

## Qué es el producto (no lo que vende el comercial)

Facturador Smart es un **ERP de facturación electrónica peruana** (CPE, notas, guías, SIRE, buzón, POS, inventario, compras, contabilidad) con un módulo vertical **Casa de empeño** (`/pawn`) de **una sola pantalla** (pestañas Préstamos / Prendas / Ajustes).

El comercial lista 12 bullets. Once son del facturador. El duodécimo (“préstamos, prendas, intereses, ventas de prendas vencidas”) es el add-on `/pawn`.

## Motor de empeño del demo (probado)

| Hecho | Evidencia |
|-------|-----------|
| Estados | Vigente, Vencido, Para remate, Liberado, Rematado, Anulado |
| Demo vivo | #5 vigente S/ 250 + int. 12.50; #4 vigente 600; #3 vencido 400; #2 para remate 700; #1 liberado |
| Interés | Mensual configurable (default 5 %). #5: 5 % · S/ 12.50 al mes |
| Remate desde | Vencimiento + ~31 d (#5 vence 2026-11-10 → remate 2026-12-11). Parecido a Credix (`FechaRemate` = venc. + 30) |
| Alta | Prenda ya tasada → monto → % interés → fecha → medio (efectivo/Yape/Plin/transfer/tarjeta). “El dinero sale de tu caja abierta.” |
| Cobrar | Renovar (interés del mes) · Abonar al capital · Liberar la prenda (pago total) |
| Ficha | Cómo llegó, tasación, fotos, constancia, anular |
| Ajustes | Interés mensual por defecto + días de espera antes del remate |
| Prenda suelta | Existe “sin préstamo” (Pulsera Patricia) y “devuelta” re-prestable |

No hay cuotas, simulador, TEA, analista/aprobador, bóveda, fondeo, PLA/UIF, ni cartera personal.

## Flujos cobrados en la segunda pasada (2026-10-26, sin grabar)

| Flujo | Qué hace de verdad | ¿Ventaja para Credix? |
|-------|--------------------|------------------------|
| **Renovar** préstamo #5 | Cobra S/ 12.50 (interés 5 %), nuevo vencimiento 2026-12-10, medio + n.° Yape/Plin. No se confirmó. | **Sí.** En Credix la prórroga solo suma días; no cobra el mes. Eso es caja que hoy no se opera en un clic. |
| **Rematar** préstamo #2 | Texto del sistema: *“La prenda pasa a Productos con 1 unidad, a S/ 1,200.00 (su tasación), para venderla en el punto de venta. El préstamo #2 queda cerrado como rematado.”* No se confirmó. | **Sí, la más fuerte.** Cierra el ciclo: custodia → stock → venta. Hoy Credix solo tiene cola `FechaRemate`. |
| Listado ventas | BLT1-1 boleta **Aceptado** (SUNAT) S/ 79.90; FTR1-1 factura crédito mismo monto, saldo 79.90. IGV 18 %. | **Sí al vender remate.** No para el préstamo. Integrar PSE, no clonar. |

Esas tres son las únicas del demo que agregan valor competitivo a una casa de crédito/empeño. El resto (SIRE, CRM, tienda, voz) es ruido de facturador.

## Tercera pasada — diálogos que faltaban (2026-10-26, sin grabar)

Se abrieron **Abonar**, **Liberar** y **Registrar prenda** y se canceló. Contrato de UX listo; no hace falta volver al demo.

### Abonar al capital · préstamo #5

```
Anillo de oro con piedra roja
Luz Marina Gutiérrez Salas · vence 2026-11-10
Fecha del pago
Monto a abonar (S/) *
Capital pendiente     S/ 250.00
Capital que queda     S/ 250.00   ← baja al tipear el monto
A cobrar              S/ 0.00
Medio de pago *       Efectivo | Yape | Plin | Transferencia | Tarjeta débito | Tarjeta crédito
N.° de operación      (si Yape/Plin/transferencia)
Cancelar / Cobrar S/ …
```

No cobra interés. Solo reduce capital. El botón queda deshabilitado en S/ 0.00.

### Liberar la prenda · préstamo #5

```
Fecha del pago
Capital                       S/ 250.00
Interés pendiente (1 mes)     S/ 12.50
A cobrar                      S/ 262.50
Medio de pago * + N.° de operación
“Al confirmar, el préstamo queda liberado y la prenda vuelve a su dueño.”
Cancelar / Cobrar S/ 262.50
```

Monto no es editable: capital + interés del periodo. Irreversible.

### Nuevo préstamo (sobre prenda ya tasada)

```
Prenda en garantía *   (busca prenda o cliente; solo “sin préstamo” / “devuelta”)
Monto a prestar (S/) *
Interés mensual (%) *  default 5.00
Fecha del préstamo *
Cómo entregas el dinero *  mismos medios
Interés de cada mes / Vence (+30 d) / Para recoger la prenda al vencer
“El dinero sale de tu caja abierta.”
```

### Registrar prenda (catálogo, antes del préstamo)

```
Cliente * [+ Nuevo]
Descripción *
Marca
Serie o IMEI
Cómo llega            (placeholder: “Ej. Bueno, con rayones”)
Valor de tasación (S/) *
“El préstamo no puede pasar de este monto.”
Fotos de la prenda    Elegir fotos / Ninguna foto
```

Catálogo separado del préstamo: una prenda puede estar “sin préstamo”, “empeñada” o “devuelta” y se vuelve a prestar. En Credix la prenda nace con el crédito; no hay que clonar el catálogo, solo usar `Observaciones` + `FotoPath`.

## Alternativas descartadas

| Opción | Por qué no |
|--------|------------|
| Migrar la operación a Facturador Smart | Pierde `usp_*`, crédito personal, bóveda, cierre gerencial, morosos, alcance sede, PLA. El empeño del demo es un préstamo mensual, no el producto CREDITO. |
| Clonar el ERP (CPE + SIRE + contabilidad) en C# | Años de trabajo; no es el rubro. SUNAT se **integra** (PSE). |
| Copiar el 5 % / “renovar = pagar un mes” en Credix | Constitución: no inventar interés. Contrato C: renovación = `usp_ProrrogarCredito`. |
| Encender POS/escáner como módulo nuevo | SSD-10 venta rápida ya existe. |

## Alternativa recomendada

1. Seguir en Credix.  
2. Adoptar UX de ficha (fotos + observaciones) con columnas ya en `CREDITO.Prenda`.  
3. Versionar remate + cobros prendarios **si** gerencia lo pide, con USP.  
4. Facturación electrónica = **PSE**, no un segundo sistema de préstamos.
