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

## Alternativas descartadas

- `position: sticky` en stats: empeora el hueco.
- Quitar KPI en móvil: gerencia los usa en campo.
- Traducir a propósito (i18n): no hay demanda; el daño es el traductor automático.
