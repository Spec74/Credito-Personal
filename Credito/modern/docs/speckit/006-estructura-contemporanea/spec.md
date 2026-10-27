# Spec — Estructura contemporánea (documento + chrome)

**Feature:** `006-estructura-contemporanea`  
**SSD:** [SSD-17](../../ssd/SSD-17-estructura-contemporanea.md)  
**Tipo:** cambio de producto · Spec Kit `/speckit.specify`  
**Fecha:** 2026-10-26  
**Constitución:** [CONSTITUTION-UX.md](../CONSTITUTION-UX.md) (p. 2, 4, 7, 13) + negocio `usp_*` en [ssd/README.md](../../ssd/README.md)

## Problema

La lógica de crédito, caja, prendario, ventas y almacén ya corre en el moderno. Lo que **sigue viéndose legado** no es un `usp_*`: es la **estructura del documento y del chrome**.

En el teléfono (p. ej. `/ventas/ordenes`) la barra superior copia el toolbar MVC: `Usuario · Oficina` + `Caja CAJA CENTRAL` en una sola línea. El resultado es `JVL… · OFL…` y `Caja CAJA CENTRÁ`. El cajero no lee la sede; lee puntos suspensivos.

Eso no se arregla reescribiendo 95 TSX. Se arregla el **host**: `index.html` + `AppShell` + tokens. Las páginas `CredixPage` / Crud / Informe / WorkCanvas heredan.

## Visión

Un host de aplicación 2026–2028, no un facsímil del MVC:

- Landmarks HTML (`banner`, `nav`, `main`, `contentinfo`).
- La sucursal y la caja son **contexto**, no un renglón truncado.
- El nombre completo de usuario/oficina/roles se abre en un panel, no se aprieta en 52 px.
- `viewport-fit=cover` y `color-scheme` para notch y UA contemporáneo.
- Sin app nativa, sin i18n, sin inventar TEA/LTV/`usp_*`.

## User stories

### US-1 — Documento con landmarks (P0)

Como operador (teclado o lector), salto al trabajo y reconozco menú / contenido / pie.

**Aceptación**

- `header.credix-header` es el banner.
- El menú (sidebar o drawer) vive en `nav` con `aria-label="Módulos"`.
- `#main-content` sigue siendo el `main`.
- El pie es `contentinfo` (oculto ≤640 px, SSD-15).
- Skip link intacto.

### US-2 — Sesión legible, no sopa de ellipsis (P0)

Como cajero en ~375 px, veo **quién** y **dónde** sin adivinar `JVL…`.

**Aceptación**

- El header **no** pinta `Usuario · Oficina` como texto suelto.
- Un control de sesión muestra la oficina (o un chip corto). Al tocarlo: usuario, oficina completa, roles, “Salir para cambiar de sede”.
- `CREDICONFIABLE` en la barra no pelea el pliegue en ≤991 px (sigue en el sider/drawer).

### US-3 — Caja es estado, no un segundo título (P0)

Como cajero, el chip dice si hay caja y lleva a diario.

**Aceptación**

- ≤991 px: etiqueta `Caja` o `Sin caja` (nombre largo en `title` / tooltip).
- ≥992 px: puede mostrar la denominación.
- Toque ≥40 px. No se concatena `Caja · CAJA CENTRAL`.

### US-4 — Host HTML contemporáneo (P1)

**Aceptación**

- `viewport` con `viewport-fit=cover`.
- `color-scheme: light` (tokens `--credix-*`; sin dark fashion).
- `lang="es"` + `translate="no"` se mantienen (SSD-15).

## Fuera de alcance

- Reescribir 95 páginas, PWA store, i18n, glass/neón.
- Inventar `usp_*`, TEA, LTV, SIRE/PSE.
- Cambiar el menú vivo o el JWT.

## Relación

Complementa SSD-13 (anatomía) y SSD-15 (cuerpo pegado al título). No reabre 003 ni 005.
