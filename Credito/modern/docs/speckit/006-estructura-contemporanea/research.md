# Research — 006-estructura-contemporanea

**Fecha:** 2026-10-26 · Spec Kit `/speckit.analyze`  
**Evidencia:** captura `/ventas/ordenes` (header `JVL… · OFL…` + `Caja CAJA CENTRÁ`); `AppShell.tsx`; `index.html`; [github/spec-kit](https://github.com/github/spec-kit) loop `specify → plan → implement`.

## Hallazgos

| Vacío | Causa | Qué no es |
|-------|--------|-----------|
| Usuario/oficina cortados | `.credix-header-user` es texto inline con `max-width: 28vw` + ellipsis. Copia el toolbar MVC (user · sede en una línea) | Falta de datos en JWT |
| `Caja CAJA CENTRÁ` | `CredixContextBar` pinta `Caja · ${denominacion}`; la denominación ya empieza por CAJA | Bug de caja diario |
| Barra llena en 375 px | Brand + user + office + caja + campana + salir en 52 px | Una pantalla de ventas mal hecha |
| Host “2018” | `viewport` sin `viewport-fit`; sin `color-scheme`; menú no es `nav` | Falta de lógica de venta |

## Decisiones

1. **Un cambio de host**, no 95 PRs. `AppShell` + CSS de chrome + `index.html`.
2. **Sesión = control**, no renglón. Popover/panel con nombres completos.
3. **Caja = chip de estado.** En móvil solo `Caja` / `Sin caja`.
4. **Landmarks nativos**, no un layout Ant `Layout.Header` nuevo.
5. **Propagar la plantilla**, no las páginas: `CredixPage` pasa a `article`/`header` + slot `toolbar`. Las ~90 Page heredan. El leftover real de ventas (`/ventas/ordenes` con `Space` MVC) se alinea a `CredixListToolbar`.

## Alternativas descartadas

- Meter el nombre completo a 11 px: ilegible (constitución p. 5).
- Segunda fila de header: come el pliegue (SSD-15).
- Reescribir Orden de venta: el círculo de la captura es el chrome, no la página.
- Dark mode / PWA: moda, no trabajo (constitución p. 4; 003 fuera de alcance).
- CLI `specify init` ahora: el repo ya mapea el loop en `docs/speckit/` ([COMO-APRENDER.md](../COMO-APRENDER.md)).
