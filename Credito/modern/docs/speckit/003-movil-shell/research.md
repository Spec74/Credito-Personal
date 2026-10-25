# Research — 003-movil-shell

**Fecha:** 2026-10-24 · Evidencia: capturas Vercel Android (Simulador, Mis tareas).

## Hallazgos

| Vacío | Causa | Qué no es |
|-------|--------|-----------|
| Hueco título → KPI | Traductor inserta nodos y parte el flex; `100vh` + pie + `margin` de `.app-shell-content` alargan el main | Un bug solo del simulador |
| Letras dobles | Chrome Translate sobre UI ya en español (`translate` no estaba en `html` / `CredixPage`) | Copy mal escrito |
| KPI apilados | `@media 480px` fuerza `ul.credix-stats` a 1 columna | Falta de data |
| Doble padding móvil | `main` tiene `credix-content` **y** `app-shell-content` (margen 12 + padding 16) | Diseño a propósito |

## Decisiones

1. **Una sola base de CSS de shell** (`credix-mobile-shell.css`), no parches por ruta.
2. **Prohibir traductor** en la app operativa (principio 13). Textos ya están en es-PE.
3. **Stats compactos en fila** en móvil. El trabajo (pasos, listado) es lo que debe verse primero después del título.
4. **Pie oculto ≤640 px.** El teléfono de soporte sigue en login / menú.

## Segunda pasada (todos los módulos)

El shell cubre `CredixPage` / Crud / Informe. **No cubría** lo que el CSS lazy pisa:

| Vacío | Causa | Archivo que gana |
|-------|--------|------------------|
| Hueco en `/inicio` | `min-height: 28rem` + head `9.5rem` (anti-CLS) | `dashboard-analista.css` (import en la página) |
| Hueco en bóveda | KPI `min-height: 200px` | `boveda-module.css` |
| KPI a 1 columna (chica, verificar, stats-3, clientes) | `@media 640/480 { 1fr }` | CSS de módulo *después* de `credix-mobile-shell.css` |
| Login / splash | Fuera de Page; `100vh`; `body` sin `translate` | `LoginPage`, `index.html` |
| Portales Ant | `getPopupContainer → document.body` | Se resuelve con `body translate="no"` |

`space-between` en toolbars/cards **no** es el hueco del título. Los `100dvh` de modales fullscreen tampoco.

## Alternativas descartadas

- `position: sticky` en stats: empeora el hueco.
- Quitar KPI en móvil: gerencia los usa en campo.
- Traducir a propósito (i18n): no hay demanda; el daño es el traductor automático.
- Subir `!important` en el shell para pisar 95 módulos: frágil. Se parchea el archivo que gana (el lazy) y se sube especificidad solo donde el shell carga antes.
- Feature `004`: mismo producto, no un cambio nuevo.
