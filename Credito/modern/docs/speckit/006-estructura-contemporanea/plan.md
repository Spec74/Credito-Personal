# Plan técnico — 006-estructura-contemporanea

**Spec Kit:** `/speckit.plan` · 2026-10-26  
**Spec:** [spec.md](spec.md) · **Research:** [research.md](research.md)

## Enfoque

Un cambio de **host**. Las ~95 páginas no se tocan.

```
1. Documento (SSD-17 + este kit)
2. index.html: viewport-fit=cover + color-scheme
3. AppShell: landmarks + sesión como control + nav
4. CredixContextBar: etiqueta corta en ≤991 px
5. CSS chrome (design-system + leftover shell)
6. Smoke: /ventas/ordenes, /credito/prendario, /caja/diario @ 375 y escritorio
```

## Stack

Sin cambio de plataforma. SPA. Sin USP. Ant `Popover` para el detalle de sesión (ya está en el árbol).

## Archivos

| Pieza | Archivo |
|-------|---------|
| Spec SSD | `docs/ssd/SSD-17-estructura-contemporanea.md` |
| Kit | `docs/speckit/006-estructura-contemporanea/*` |
| HTML | `Credito.Modern.Web/index.html` |
| Shell | `components/layout/AppShell.tsx` |
| Sesión | `components/layout/CredixSessionControl.tsx` (nuevo) |
| Caja | `components/credix/CredixContextBar.tsx` |
| CSS | `styles/credix-design-system.css`, `styles/credix-oleada-a.css`, `styles/credix-mobile-shell.css` |
| Índice | `docs/speckit/README.md`, `docs/ssd/README.md`, `docs/DOCUMENTACION.md` |
| Bitácora | `docs/migration/BITACORA-DESVIACIONES.md` |

## No se toca

`usp_*`, JWT, menú `MAESTRO`, 95 `*Page.tsx`, color de marca `#114885`.
