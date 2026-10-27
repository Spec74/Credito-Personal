# Plan técnico — 005-facturador-smart

**Spec Kit:** `/speckit.plan` · 2026-10-26

## Enfoque

Este cambio es **especificación y triage**. No se implementa SUNAT ni remate en este PR. El primer código permitido es US-1 (observaciones + foto) **después** de que informática elija dónde vive el archivo.

```
1. Spec + research + auditoría (esta carpeta)     ← este entregable
2. Gerencia: ¿PSE sí/no? ¿Remate vende stock?     ← decisión de negocio
3. US-1 ficha prenda (columnas existentes)
4. USP remate / cobros prendarios (si gerencia)
5. Adaptador PSE (si gerencia) + encender menú SSD-10/11
```

## Archivos (cuando se implemente US-1)

| Pieza | Archivo probable |
|-------|------------------|
| DTO / read | `creditogestiondtos.cs` ya trae `FotoPath` |
| SPA gestión | `CreditoPrendarioGestionPage.tsx` |
| Guardar prendas | endpoints existentes `guardar-prendas` — no inventar tabla |
| Storage | user-secrets / blob; **nunca** path absoluto en Git |

## Qué no se toca

- `usp_*` de cuota/mora/crédito.
- `usp_Credito_Del`, `usp_OrdenVenta_Del`.
- Web.config.
- Relajar JWT oficina.
- Tema visual / “asistente de voz” del demo.
- Menú oficina 1 CREDITO (ventas) hasta que una sede venda remate.

## Riesgos

- Subir fotos sin política de retención/PII (DNI en el mismo expediente).
- Pintar “Rematar” o “Renovar interés” sin USP: viola constitución.
- Contratar el demo completo “por si acaso”: dos sistemas, dos cajas, dos verdades.
