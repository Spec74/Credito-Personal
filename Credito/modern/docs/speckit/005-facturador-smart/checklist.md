# Checklist — 005-facturador-smart

## Evidencia de la auditoría (2026-10-26)

- [x] Login demo proveedor → `/documents/create`
- [x] Menú completo mapeado (Ventas … Casa de empeño … CRM)
- [x] `/pawn` préstamos: 5 filas demo, chips de estado, Nuevo préstamo, Ver #5, menú Cobrar
- [x] `/pawn` prendas: sin préstamo / empeñada / devuelta + fotos + tasación
- [x] `/pawn` ajustes: interés % y días a remate
- [x] Renovar #5 (dialog): cobra S/ 12.50 y corre vencimiento un mes — **Cancelar**
- [x] Rematar #2 (dialog): pasa prenda a Productos a tasación S/ 1 200 y cierra el préstamo — **Cancelar**
- [x] `/documents` BLT1-1 Aceptado SUNAT + FTR1-1 crédito
- [x] `/dashboard` proyección de **ventas** (S/ 159.80), no de cartera
- [x] `/pos` escáner + PAGAR
- [x] `/cash` caja aperturada S/ 3 000
- [x] `/finances/unpaid` CxC de factura (FTR1-1), no de crédito
- [x] No se grabó préstamo ni remate en el demo

## Decisión gerencia (bloquea implementación)

- [ ] ¿Facturamos venta de prenda rematada? (sí → cotizar PSE, no clonar ERP)
- [ ] ¿Remate pasa el bien a stock SSD-10/11? (sí → USP + menú ventas)
- [ ] ¿Cobro prendario = interés del mes / abono / liberar? (sí → USP; no copiar 5 %)
- [ ] ¿Dónde viven las fotos? (blob / file share)

## US-1 (as-built 2026-10-26)

- [x] Observaciones visibles y editables en gestión
- [x] Foto si `FotoPath` tiene valor (`GET/POST .../prenda-foto`)
- [x] LTV y tasación siguen saliendo de la BD, no del C#

## Fuera de esta feature

- [ ] No hay pantalla SIRE/CPE en Credix
- [x] Cutover remate: RolMenu concede ventas/almacén existentes + `VENTA RAPIDA` si faltaba
