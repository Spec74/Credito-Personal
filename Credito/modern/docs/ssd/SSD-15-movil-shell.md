# SSD-15 — Shell móvil (todas las pantallas)

**Estado:** as-built · 2026-10-24  
**Código:** `CredixPage`, `AppShell`, `styles/credix-mobile-shell.css`, `index.html`  
**Spec Kit:** [003-movil-shell](../speckit/003-movil-shell/spec.md)  
**No entra negocio:** ningún `usp_*`.

## 1. Propósito y actores

Quien opera en celular (cajero, analista, encargado, gerencia) debe ver el trabajo **pegado al título**, en español estable, con toques de 40 px.

## 2. Alcance

**Entra:** layout del shell, traductor, KPI compactos, padding/pie móvil.  
**No entra:** app nativa, i18n, reescritura por módulo, reglas de crédito/caja.

## 3. Paridad MVC

El legado no tenía SPA móvil. Esto es producto Credix (no paridad de pantalla).

## 4. Contrato de datos

Ninguno. Solo CSS + `translate` + `lang="es"`.

## 5. API y SPA

No hay API. Superficie: cualquier ruta que use `CredixPage` / Crud / Informe / WorkCanvas.

## 6. Seguridad

Sin cambio de JWT ni menú. `translate="no"` no relaja CSP.

## 7. Criterios de aceptación

- [x] Código: pila `flex-start`, `100dvh`, stats en fila compacta, sin margen doble, pie oculto ≤640 px.
- [x] Código: `html` / app / page sin traductor automático.
- [ ] Smoke 375 px en simulador y tareas (firma operativa).

## 8. Desviaciones

- **2026-10-24 — notranslate:** la UI ya está en es-PE; Chrome Translate doblaba grafías y partía el flex ([BITACORA](../migration/BITACORA-DESVIACIONES.md)).

## 9. Pruebas

- `tsc -b` (Vercel).
- Checklist: [003/checklist.md](../speckit/003-movil-shell/checklist.md).

## 10. Go-live

Listo en Development al deployar `Eber-migracion`. No bloquea cutover SSD-00.
