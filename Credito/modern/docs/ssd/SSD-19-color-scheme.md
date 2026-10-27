# SSD-19 — Esquema de color (claro / oscuro)

**Estado:** as-built · 2026-10-27  
**Código:** `theme/credixColorScheme.ts`, `theme/credixDarkTheme.ts`, `theme/CredixColorSchemeProvider.tsx`, `styles/credix-color-scheme.css`, tokens en `credix-design-system.css`  
**Complementa:** [SSD-13](SSD-13-experiencia-operativa.md) (anatomía), [SSD-17](SSD-17-estructura-contemporanea.md) (host), [CONSTITUTION-UX](../speckit/CONSTITUTION-UX.md) p. 4, 6, 14

El operador elige claro u oscuro. El oscuro **no** es invertir la pantalla.

## 1. Propósito y actores

Quien opera de noche o con Windows en oscuro debe leer importes, menú e informes sin halo, sin fotos invertidas y sin bloques blancos sueltos.

| Rol | Superficie |
|-----|------------|
| Todos | Toggle sol/luna en cabecera; login también |
| Informática | Tokens `--credix-*` + `data-theme` en `html` |

## 2. Alcance

**Entra**

- Preferencia `light` / `dark` / `system` (`localStorage` `credix-color-scheme`)
- Primera visita: sigue `prefers-color-scheme` del SO
- Tokens WCAG 2.2 AA (texto 4.5:1, UI 3:1, foco visible, target ≥24 px)
- Tema Ant Design `darkAlgorithm` + menú `theme="dark"`
- Menú inline, flyout colapsado, accesos rápidos, drawer, tarjetas de informe

**No entra**

- Invertir fotos de prenda, logo o capturas
- Oscuro obligatorio (el claro sigue siendo válido)
- Recalcular `usp_*` / cambiar marca navy de cabecera
- Chrome Translate (sigue `translate="no"`; si el usuario lo fuerza, dobla letras)

## 3. Paridad MVC

El legado no tenía esquema de color. Desviación de producto (bitácora).

## 4. Contrato de datos

Ninguno. No hay columna ni `usp_*` de tema.

## 5. API y SPA

Sin API. El FOUC se evita con script en `index.html` que pone `data-theme` antes del bundle.

| Pieza | Dónde |
|-------|--------|
| Resolución | `resolveCredixColorScheme` |
| Tokens oscuros | `:root[data-theme='dark']` |
| Ant | `credixDarkTheme` (`colorPrimary` `#2e69ae`, enlaces `#8cb6e8`) |
| Toggle | `CredixColorSchemeToggle` (cabecera + login) |

## 6. Seguridad

Sin cambio de JWT. `color-scheme` solo informa al UA (scrollbars, inputs nativos).

## 7. Criterios de aceptación (WCAG 2.2 AA)

- [x] Lienzo ≠ `#000` (halo). Texto de cuerpo ≠ `#fff` puro.
- [x] Texto secundario ≥ 4.5:1 sobre canvas y cards (`--credix-text-muted`).
- [x] Enlaces y selección en oscuro usan `--credix-ink-brand` (`#8cb6e8`), no navy `#114885` (falla contraste sobre fondo oscuro).
- [x] Cabecera y botón primario conservan navy + blanco (≥ 8:1). Acción en canvas oscuro: `--credix-action` `#2e69ae`.
- [x] Submenús, flyouts y accesos rápidos usan superficie oscura (no degradé blanco).
- [x] Pies de tarjeta de informe, selectores y date pickers no quedan blancos.
- [x] PDF/XLS en oscuro: `--credix-pdf` / `--credix-xls` (rojo/verde claros ≥ 4.5:1).
- [x] `img` / video / foto de prenda: `filter: none`.
- [x] Foco visible (2.4.13) y toque ≥ 40 px en el toggle (2.5.8 / 2.5.5).
- [x] Tests de resolución de preferencia (`credixColorScheme.test.ts`).
- [x] Superficies de módulo (`#f8fafc`, degradés a `#fff`, thead/summary Ant, scroll de tabla) usan tokens (`--credix-surface`, `--credix-surface-muted`, `--credix-table-head`, `--credix-scroll-*`).
- [x] Bóveda (tarjetas medio), caja diario (Crédito activo + leyenda), caja saldos (thead + TOTAL), crédito tareas (subtarea) y aprobar no dejan bloques blancos en oscuro.
- [x] Semáforo (morosos, SBS): en oscuro tinte + texto cromático (`--credix-chip-*`), no relleno neón. Buscador de créditos usa `--credix-surface`.
- [x] Cierre gerencial: `SIN CONFIGURAR`, PRODUCTIVA/ESPECIAL, barra de cierre oficial y `ant-tag-*` usan chips/tokens (no `#eef2f5` ni navy `#0f4c81` como texto).

## 8. Desviaciones

- **2026-10-27 — esquema de color moderno:** [BITACORA](../migration/BITACORA-DESVIACIONES.md).

## 9. Pruebas

`vitest` tema + `tsc -b`. Smoke: `/tesoreria/boveda`, `/caja/diario`, `/caja/saldos`, `/credito/tareas`, `/credito/aprobar`, `/reportes/credito`, `/informes/cierre-gerencial`. Alternar sol/luna.

## 10. Go-live

Listo en Development. No bloquea cutover SSD-00. No requiere script SQL.
