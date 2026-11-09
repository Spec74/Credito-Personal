# SSD-15 — Shell móvil (todas las pantallas)

**Estado:** as-built · 2026-10-24 · pasada avisos/saldos/cards 2026-11-09  
**Código:** `CredixPage`, `AppShell`, `styles/credix-mobile-shell.css`, `credix-responsive-global.css`, `credix-intranet.css`, `index.html`  
**Spec Kit:** [003-movil-shell](../speckit/003-movil-shell/spec.md) · auditoría viva: [responsive-audit.md](../responsive-audit.md)  
**No entra negocio:** ningún `usp_*`.

## 1. Propósito y actores

Quien opera en celular (cajero, analista, encargado, gerencia) debe ver el trabajo **pegado al título**, en español estable, con toques de 40 px.

## 2. Alcance

**Entra:** layout del shell, traductor, KPI compactos, padding/pie móvil, leftovers de CSS lazy (inicio, bóveda, caja, login).  
**No entra:** app nativa, i18n, reescritura de 95 TSX, reglas de crédito/caja.

## 3. Paridad MVC

El legado no tenía SPA móvil. Esto es producto Credix (no paridad de pantalla).

## 4. Contrato de datos

Ninguno. Solo CSS + `translate` + `lang="es"`.

## 5. API y SPA

No hay API. Superficie: `CredixPage` / Crud / Informe / WorkCanvas + Login + `body` (portales Ant). El CSS de módulo que se importa en la página **pisa** el shell: se parchea ahí. Matriz: [audit-modulos.md](../speckit/003-movil-shell/audit-modulos.md).

## 6. Seguridad

Sin cambio de JWT ni menú. `translate="no"` no relaja CSP.

## 7. Criterios de aceptación

- [x] Código: pila `flex-start`, `100dvh`, stats en fila compacta, sin margen doble, pie oculto ≤640 px.
- [x] Código: `html` / `body` / app / page / login sin traductor automático.
- [x] Código: Inicio sin reserva 28rem/9.5rem en móvil; bóveda sin KPI de 200 px; chica/verificar/diario/clientes/stats-3 en `auto-fit`.
- [x] Código 2026-11-09: avisos toolbar en columna ≤640; campana `credix-notify-popover` con max-width viewport; saldos tabs scroll horizontal; cards móviles con tokens (no `#f5f9ff` fijo); drawer extra táctil.
- [ ] Smoke 375 px en simulador, tareas, inicio, diario, bóveda, avisos, saldos, login (firma operativa).

## 8. Desviaciones

- **2026-10-24 — notranslate:** la UI ya está en es-PE; Chrome Translate doblaba grafías y partía el flex ([BITACORA](../migration/BITACORA-DESVIACIONES.md)).
- **2026-10-24 — leftovers de módulo:** el CSS lazy gana al shell (misma especificidad, llega después). Se parchea el archivo del módulo, no se usa `!important` global.

## 9. Pruebas

- `tsc -b` (Vercel).
- Checklist: [003/checklist.md](../speckit/003-movil-shell/checklist.md).

## 10. Go-live

Listo en Development al deployar `Eber-migracion`. No bloquea cutover SSD-00.
