# Plan técnico — 003-movil-shell

**Spec Kit:** `/speckit.plan` · 2026-10-24  
**Spec:** [spec.md](spec.md) · **Research:** [research.md](research.md)

## Enfoque

Un cambio de **shell**, no 95 PRs. `CredixPage` / Crud / Informe / WorkCanvas heredan.

```
1. Documento (SSD-15 + este kit)
2. html + CredixPage + AppShell: translate="no"
3. credix-mobile-shell.css: pila, dvh, stats, padding, pie
4. Smoke 375×812: simulador, tareas, diario, bóveda, inicio
```

## Stack

Sin cambio de plataforma. Solo SPA. Sin USP.

## Archivos

| Pieza | Archivo |
|-------|---------|
| Spec SSD | `docs/ssd/SSD-15-movil-shell.md` |
| Kit | `docs/speckit/003-movil-shell/*` |
| HTML | `Credito.Modern.Web/index.html` |
| Page | `components/credix/CredixPage.tsx` |
| Shell | `components/layout/AppShell.tsx` |
| CSS | `styles/credix-mobile-shell.css` (nuevo) · import en `main.tsx` |
| Auditoría | `docs/responsive-audit.md` |

## Riesgos

- `translate="no"` impide traducir a quechua/inglés en el navegador. Aceptable: la UI es es-PE.
- Ocultar footer: el teléfono queda en login. OK.
