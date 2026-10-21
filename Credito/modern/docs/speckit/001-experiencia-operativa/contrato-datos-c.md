# Contrato de datos — Oleada C

**Spec Kit:** T-C0 · 2026-10-20  
**Regla:** la BD es el contrato. La UI no inventa tope LTV, renovación, TEA/TCEA ni PLA.

| Necesidad | ¿Existe hoy? | Qué puede hacer la UI | Qué falta (gerencia + `usp_*`) |
|-----------|--------------|------------------------|--------------------------------|
| **LTV** | Sí, derivado: `MontoCredito / MontoTasacion`. Tope vigente = **100%** (`monto ≤ tasación`). No hay % en Producto/Oficina. | Mostrar LTV observado. Validar tope 100% (ya en `validarMontoVsTasacion`). | Si gerencia pide 80% u otro tope: parámetro en producto/oficina + USP. **No hardcodear 80.** |
| **Renovación prendaria** | **No hay renovación.** Existe **prórroga** `CREDITO.usp_ProrrogarCredito` (suma días a vencimiento, solo `DES`). | Prorrogar en consulta y en gestión prendaria. No se pinta “renovar” como crédito nuevo. | Si gerencia pide crédito nuevo, otro USP. |
| **Devolución de bien** | `CREDITO.usp_DevolverPrenda` (2026-10-20). `EN CUSTODIA` → `DEVUELTO` con `FechaMod`/`UsuarioModId`. Bloquea si el crédito está `DES`. | Devolver + comprobante PDF en gestión. | — |
| **Cola remate** | `Credito.FechaRemate` (vencimiento + 30 d). Categoría `REMATADO` / `VENCIDO` / `PORVENCER` ya en listado y WhatsApp 3 d. | Filtrar la misma lista (sin nuevo cálculo). | Acta de remate / pipeline de venta si gerencia lo pide. |
| **Hoja resumen** | Contrato y acta QuestPDF (`contrato-pdf`, `acta-entrega-pdf`). Interés del crédito. **No hay TEA/TCEA** en USP. | Reimprimir contrato/acta. No inventar TEA. | Campos TEA/TCEA en `usp` si SBS/gerencia lo exige. |
| **Cliente otra sede** | `MAESTRO.Persona` es **global**. Búsqueda no filtra oficina. Créditos y edición operativa van con `vendix:oficina_id`. | Buscar y ver ficha. Operar cartera solo en la sede del login. | Consulta de cartera ajena (solo lectura) si gerencia la autoriza. |
| **PLA umbral** | Piso legal: D.S. 020-2017-JUS art. 24.4. RO individual **US$ 10 000**; múltiples mes **US$ 50 000**. ROS **sin** monto. Detalle: [contrato-pla-uif.md](contrato-pla-uif.md). | Cola RO + bitácora inusual. TC SBS del mes (parámetro). Gerencia solo puede **bajar** el piso. | Oficial de cumplimiento + envío ROSEL (fuera del sistema). |

## T-Q1 — decisiones 2026-10-20

1. Tope LTV vigente = **100%** (no se hardcodea 80).  
2. Renovación = **prórroga** (`usp_ProrrogarCredito`), no crédito nuevo.  
3. Ventas/almacén/maestros en oficina 1: **no** (sin ítem de menú; 403 correcto).  
4. Wizard sucursal queda en **SPA**.
