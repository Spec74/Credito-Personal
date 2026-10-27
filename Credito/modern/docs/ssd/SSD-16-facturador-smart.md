# SSD-16 — Lecciones Facturador Smart

**Estado:** spec de **cambio** · 2026-10-26 · **sin código aún**  
**Spec Kit:** [005-facturador-smart](../speckit/005-facturador-smart/spec.md)  
**Auditoría:** [audit-modulos.md](../speckit/005-facturador-smart/audit-modulos.md)

## 1. Propósito y actores

Gerencia pidió revisar el demo que le ofrecieron y llevar Credix al límite **sin** cambiar de sistema.

| Actor | Uso de este SSD |
|-------|-----------------|
| Gerencia | Decidir: no migrar; cotizar PSE solo si se factura remate |
| Informática | Backlog US-1…5; no clonar CPE/SIRE |
| Analista / cajero | Más adelante: ficha de prenda y, si hay USP, remate |

## 2. Alcance

**Entra:** triage del demo, user stories, lo que se adopta / rechaza.  
**No entra:** implementar Facturador, inventar interés, segundo SQL.

## 3. Hallazgo

El demo es un facturador con un `/pawn` de interés mensual. Credix ya cubre crédito, caja, bóveda, prendario, informes y multi-sede. El único hueco de producto es **comprobante electrónico al vender**, más **cerrar el ciclo remate** (hoy solo hay cola).

## 4. Contrato de datos

Sin USP nuevo en esta spec. US-1 reutiliza `CREDITO.Prenda.Observaciones` y `FotoPath`. Remate / “renovar interés” esperan contrato C (como [001/contrato-datos-c](../speckit/001-experiencia-operativa/contrato-datos-c.md)).

## 5–10

API/SPA, seguridad, criterios, pruebas y go-live de **código** no aplican todavía. Criterios de la auditoría: checklist 005. Go-live de crédito/caja **no** depende de este SSD.
