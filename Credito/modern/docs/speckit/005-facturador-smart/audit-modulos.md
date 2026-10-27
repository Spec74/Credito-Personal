# Auditoría módulo a módulo — Facturador Smart vs Credix

**Demo:** `empeno.facturadorsmart.pe` · 2026-10-26 · v 11.1026.0630  
**Credix:** SSD-00…15, rama `Eber-migracion`.

Leyenda de **Decisión:** **Ya lo tenemos** · **Adoptar/adaptar** · **Rechazar** · **Integrar (no construir)**.  
**Evidencia:** *probado* = pantalla ejercida; *menú* = ítem visto, no se ejecutó un alta.

## Matriz

| # | Módulo FS | Ruta / evidencia | Credix hoy | Decisión | Nota |
|---|-----------|------------------|------------|----------|------|
| 1 | Casa de empeño — préstamos | `/pawn` *probado* | SSD-06 listado + categorías + `FechaRemate` | **Adaptar UX** | Ellos: interés mensual + Ver/Cobrar/Rematar. Nosotros: crédito con `usp_*`, prórroga, no “Rematar” que venda. |
| 2 | Casa de empeño — prendas | misma *probado* | `CREDITO.Prenda` (marca, serie, `Observaciones`, `FotoPath`) | **Adoptar** | Fotos y “cómo llegó” no están en la SPA. Columnas sí. |
| 3 | Casa de empeño — ajustes | % interés default, días a remate *probado* | Tope LTV 100 %; remate = venc. + 30 | **Rechazar copiar %** | Parámetro solo si gerencia + USP/producto. |
| 4 | Nuevo comprobante | `/documents/create` *probado* | No hay CPE | **Integrar PSE** | Factura/boleta/NC/ND. No clonar el formulario. |
| 5 | Listado ventas / notas / cotizaciones / pedidos | menú Ventas | SSD-10 orden + venta rápida (menú apagado of. 1) | **Ya (interno)** | Cotización/pedido FS es comercial; nosotros pedido+caja. |
| 6 | CPE no enviados / rectificar / contingencia / resúmenes / anulaciones | menú | No | **Integrar PSE** | Cola SUNAT vive en el facturador, no en Credix. |
| 7 | POS + venta rápida + cajas | `/pos` `/cash` *probado* | SSD-03 caja diario; SSD-10 venta rápida | **Ya** | FS caja: apertura/cierre/saldo. Credix es más tesorería (chica, verificar, bóveda). |
| 8 | Guías de remisión | menú | No | **Rechazar** | No es el flujo diario de crédito. |
| 9 | Compras / proveedores / OC | menú | No (crédito no compra mercadería de giro) | **Rechazar** salvo almacén de remates |
| 10 | Productos, marcas, series, lotes | menú | SSD-09 + SSD-11 | **Ya** | Activar menú si se vende remate. |
| 11 | Clientes | menú | SSD-05 Persona red + DNI | **Ya** | Ellos: portal/zonas. Nosotros: ficha + PLA. |
| 12 | Finanzas CxC | `/finances/unpaid` *probado* | Cobranza, morosos, `usp_PagarCuentaxCobrar` | **Ya (más fuerte)** | FS CxC = factura FTR1-1 S/ 79.90. Credix = cartera de cuotas. |
| 13 | Dashboard | `/dashboard` *probado* | `/inicio` admin + sede Central | **Adaptar idea** | Proyección EOM y “salud” son de **ventas**. No copiar: nuestro KPI es cobrado/desembolso/cartera/mora. |
| 14 | Inventario / kardex | menú | SSD-11 `usp_GenerarKardex` | **Ya** | Kardex 13.1 valorizado FS es contable SUNAT. |
| 15 | Usuarios y locales | menú | SSD-08 + 004 alcance sede | **Ya (más fuerte)** | Ellos: establecimientos. Nosotros: JWT oficina, Central vs sucursal. |
| 16 | WhatsApp masivo | menú | SSD-06 aviso 3 d Cloud API + `wa.me` | **Adaptar** | Enviar PDF de contrato/constancia/comprobante; no CRM masivo. |
| 17 | Buzón SUNAT / SIRE / sistema contable | menú | No | **Rechazar construir** | Contador + PSE. |
| 18 | Reportes ventas | menú | SSD-07 + cierre gerencial | **Ya** | Ellos: ingresos de factura. Nosotros: cartera y caja. |
| 19 | CRM / planilla / encuestas / tienda / links / rótulos | menú | No | **Rechazar** | Ruido para una casa de crédito. |
| 20 | Ayuda por pantalla + buscar menú + voz | chrome *visto* | Constitución: cero ruido | **Rechazar voz/IA** | Buscar menú: opcional P3. No párrafos permanentes. |
| 21 | Offline / Smart Print | menú | Cobro bloque offline (foto de cartera) | **Rechazar clonar** | No SQL por sucursal. |

## Pitch del vendedor vs realidad

| Bullet comercial | ¿Qué es? | ¿Credix lo necesita? |
|------------------|----------|----------------------|
| Facturas, boletas, NC/ND | CPE | Sí, **al vender remate / mostrador**. Integrar. |
| Enviar a SUNAT + WhatsApp/correo | PSE + canal | Sí el envío del **PDF ya nuestro** + CPE del PSE. |
| Cotizaciones y pedidos | Ventas | No para crédito diario. SSD-10 cubre pedido interno. |
| Inventario y kardex | Almacén | Ya. Encender si hay remate. |
| Compras y proveedores | Compras | No. |
| Cobros pendientes | CxC de factura | Ya: cobranza/morosos. Distinto objeto. |
| Guías de remisión | Logística | No. |
| Varios locales y permisos | Multi-sucursal | Ya cerrado (004). |
| POS | Mostrador | Ya (venta rápida). |
| Reportes de ventas | Ingresos | Ya (cierre / rentabilidad si hay menú). |
| Contable, SIRE, Buzón | Contabilidad | No construir. |

## Preguntas que el vendedor le hizo al gerente

Respuesta recomendada (no enviar credenciales ni arquitectura):

1. **¿Cuántos locales?** Varios (Huamanga Central, Pichari, Huanta, Andahuaylas). Ya operan en un solo SQL.  
2. **¿Primer sistema?** No. Sistema propio de crédito (Credix) en producción strangler.  
3. **¿Qué controlar?** Préstamos, prendas, intereses y venta de vencidas **ya**. El único hueco comercial es **facturación electrónica de la venta**, no el préstamo.

## Conclusión para gerencia

No comprar Facturador Smart **como sistema de la casa**. Si el contador necesita CPE, se cotiza **solo el plan de facturación** (o cualquier PSE) y se engancha a la venta de remate. El “módulo casa de empeño” del demo es más simple que SSD-06 y **no** reemplaza crédito personal ni tesorería.
