# Spec — Lecciones de Facturador Smart (sin reemplazar Credix)

**Feature:** `005-facturador-smart`  
**SSD:** [SSD-16](../../ssd/SSD-16-facturador-smart.md) · toca [SSD-06](../../ssd/SSD-06-prendario.md) · [SSD-10](../../ssd/SSD-10-ventas.md) · [SSD-11](../../ssd/SSD-11-almacen.md)  
**Tipo:** cambio de producto · Spec Kit `/speckit.specify`  
**Fecha:** 2026-10-26  
**Constitución:** `usp_*` no se inventa · JWT = oficina · tokens Credix · no copiar un ERP de facturación

## Problema

Gerencia recibió un demo comercial de **Facturador Smart** (`empeno.facturadorsmart.pe`, v 11.1026.0630, “Demo Casa de empeño”). El vendedor vende un **facturador electrónico** con un add-on de empeño. El riesgo es comprar o clonar ese producto y **perder** el motor de crédito, caja, bóveda y `usp_*` que ya tenemos.

Pregunta de gerencia: qué funcionalidades de ese demo **sí** nos sirven para llevar Credix Modern al límite, y cuáles no.

## Visión

Credix sigue siendo el sistema de la empresa. Del demo se **adoptan ideas de operación** (ficha de prenda, ciclo remate → venta, comprobante SUNAT como *integración*). No se sustituye CREDITO por un ERP de facturas.

## Tesis (una frase)

Facturador Smart es mejor **facturador**; Credix es mejor **casa de crédito**. El hueco real es **comprobante electrónico + venta de prenda rematada**, no préstamos ni caja.

## Alcance cerrado (2026-10-26, demo cae a las 18:00)

No se vuelve al demo. El contrato de UX de las 3 acciones de cobro + remate + ficha ya está en `research.md`.

| # | Qué | ¿Lo implementamos? | Cuándo | ¿Necesita el demo? |
|---|-----|--------------------|--------|--------------------|
| **US-1** | Ficha: cómo llegó + foto | **Sí** | **Ahora** (columnas ya en `CREDITO.Prenda`) | No |
| **US-2** | Rematar → stock vendible + cierra el crédito | **Sí** | Tras USP versionado (gerencia + DBA) | No — texto del diálogo ya es el contrato |
| **US-4** | Renovar / Abonar / Liberar (con caja) | **Sí** | Mismo USP / contrato de datos | No — los 3 modales ya están copiados |
| **US-5** | Encender menú ventas/almacén en la sede que venda remate | **Sí** | Junto con US-2, no antes | No |
| **US-3** | Boleta/factura SUNAT al vender el remate | **Sí, como PSE** | Cuando gerencia elija proveedor | No |

**No implementamos** (descartado aunque el demo lo tenga): SIRE, Buzón SUNAT, plan de cuentas, CRM, tienda virtual, planilla, voz, Smart Print, clonar el 5 %, catálogo “prenda sin préstamo”, POS nuevo.

## User stories (backlog, no un PR)

### US-1 — Ficha de prenda con evidencia (P0, **as-built 2026-10-26**)

Como analista, registro cómo llegó el bien y una foto. Columnas `Observaciones` / `FotoPath` ya existían; ahora las usa la SPA y `usp_PrendarioActualizarFicha`. Foto en `storage/{creditoId}/`.

**Aceptación**

- Gestión prendaria muestra y guarda observaciones (“cómo llegó”; en el demo: *Ej. Bueno, con rayones*).
- Si hay `FotoPath`, se ve la imagen. Subida: blob/ruta acordada con informática (no hardcodear disco local).
- Sin recálculo de tasación ni LTV.
- No se clona el catálogo “prenda sin préstamo” del demo: en Credix la prenda nace con el crédito.

### US-2 — Remate que cierra el ciclo (P0, **exige USP**)

Como encargado, un crédito `Para remate` tiene una acción **Rematar** con confirmación de importes. El bien pasa a stock vendible (SSD-10/11) o queda marcado `Rematado`. Hoy solo hay cola (`FechaRemate` = vencimiento + 30 d).

**Aceptación**

- No se pinta “Rematar” hasta existir `usp_*` / transacción versionada.
- Confirmación irreversible (constitución 11).
- No se inventa precio de remate en C#.

### US-3 — Comprobante SUNAT al vender la prenda (P1, **PSE externo**)

Como cajero, al vender una prenda rematada emito boleta/factura y la mando a SUNAT + WhatsApp/correo. Credix no reconstruye CPE/SIRE/Buzón.

**Aceptación**

- Integración con un PSE/OSE (puede ser el mismo proveedor u otro). Credix origina la venta; el PSE firma y envía.
- Fuera: clonar Facturador Smart, SIRE, plan de cuentas, 0710.

### US-4 — Cobros prendarios explícitos (P1, **exige USP**)

Como cajero, en un préstamo prendario veo: renovar interés del periodo / abonar capital / liberar (pago total). Hoy hay prórroga (`usp_ProrrogarCredito`) y cobro de cuotas del crédito personal, no este menú de empeño simple.

**Contrato de UX (demo, no fórmula Credix)**

| Acción | Qué cobra | Qué cambia | Irreversible |
|--------|-----------|------------|--------------|
| Renovar | Solo interés del periodo (demo: 5 % · S/ 12.50) | Vence + 1 mes | No (es cobro + prórroga) |
| Abonar | Monto libre ≤ capital pendiente | Baja capital; “capital que queda” en vivo; **no** cobra interés | No |
| Liberar | Capital + interés pendiente (demo #5: 250 + 12.50 = 262.50, no editable) | Préstamo liberado; *“la prenda vuelve a su dueño”* | Sí |

Todos: fecha, medio (efectivo/Yape/Plin/transfer/tarjeta) y n.° de operación. Sale a caja.

**Aceptación**

- Si gerencia quiere este menú, primero USP que cobre + mueva vencimiento/capital/estado. No copiar el 5 % ni el “1 mes” del demo.
- `usp_ProrrogarCredito` hoy solo suma días: no sustituye Renovar.
- No pintar los tres botones hasta existir el USP.

### US-5 — Activar ventas/almacén para remate (P2)

Como oficina que vende prendas, el menú vivo incluye SSD-10/11. Hoy el código está; oficina 1 CREDITO no las lista.

**Aceptación**

- Ítems de menú SQL solo en oficinas que operen mostrador. Sin reescribir venta rápida.

## Fuera de alcance

- Reemplazar Credix por Facturador Smart.
- Inventar TEA, mora, LTV, interés % en C#.
- Reconstruir: SIRE, Buzón SUNAT, libros electrónicos, planilla, CRM, tienda virtual, encuestas, influencers, cupones, ingredientes, rótulos de courier, link de pago, ecommerce.
- POS con escáner como producto nuevo (ya existe `/ventas/venta-rapida`).
- Offline Smart Print / SQL Express por sucursal.
- “Renovar” = crédito nuevo (contrato C: renovación = prórroga).

## Relación

Complementa SSD-06, SSD-13 oleada C (cola remate sin acta) y SSD-10/11 (código listo, menú apagado). No pisa 004 (alcance sede).
