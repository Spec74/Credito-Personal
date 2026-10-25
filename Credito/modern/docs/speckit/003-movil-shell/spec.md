# Spec — Shell móvil (todas las pantallas)

**Feature:** `003-movil-shell`  
**SSD:** [SSD-15](../../ssd/SSD-15-movil-shell.md)  
**Tipo:** cambio de producto · Spec Kit `/speckit.specify`  
**Fecha:** 2026-10-24  
**Constitución:** [CONSTITUTION-UX.md](../CONSTITUTION-UX.md) (principio 13: español operativo, sin traductor)

## Problema

En el celular (Vercel, Chrome) el **cuerpo queda abajo del encabezado** con un hueco blanco (Simulador, Mis tareas y el resto de `CredixPage`). Además Chrome Translate **dobla letras** (`Simuladorador`, `Montoto`, `Cuotastas`, `Oficina` → `Oficinaa`).

No es un bug de una pantalla. El shell (`CredixPage` + stats + `100vh` + margen doble) y el traductor rompen **todas** las rutas.

## Visión

El mostrador y el campo usan el mismo Credix en un teléfono ~360 px: el trabajo empieza **bajo el título**, sin hueco; el español no se traduce; toques de 40 px; sin scroll horizontal bloqueante.

## User stories

### US-1 — El cuerpo pega al título (P0)

Como cajero o analista en el celular, veo KPI y formulario **justo debajo** del H1, no al fondo de la pantalla.

**Aceptación**

- `.credix-page` es una pila `flex-start`. Ningún hijo (`head`, stats, body) hace `flex: 1` ni `margin-top: auto`.
- `main.credix-content` no lleva `margin` extra de `.app-shell-content` en ≤991 px.
- Alto de app = `100dvh` (no `100vh` del chrome de Android).
- Pie de soporte no roba el pliegue en ≤640 px.

### US-2 — Español intacto (P0)

Como usuario, leo «Simulador», «Monto», «Cuotas», «Oficina», no versiones dobladas.

**Aceptación**

- `html lang="es" translate="no"` + `meta google=notranslate`.
- `.credix-app` y `CredixPage` (default) `translate="no"`.
- El traductor del navegador no reordena el flex.

### US-3 — KPI no se comen el pliegue (P0)

Como analista, los 3 stats del simulador/tareas caben en **una fila** compacta. El paso «Cliente» / el listado se ve sin bajar media pantalla.

**Aceptación**

- ≤640 px: stats en `auto-fit` ≥96 px (no 1 columna de tarjetas altas).
- Padding de li reducido. Tipo del valor ≤ 1.05 rem.

### US-4 — Táctil y usable (P1)

**Aceptación**

- Controles primarios ≥40 px (`--credix-touch-min`).
- Sin overflow-x en `CredixPage` / tablas operativas (scroll interno de tabla OK).
- Breadcrumb en una línea (ellipsis). Footer de app no tapa el último campo.

### US-5 — Módulos que cargan CSS *después* del shell (P0)

Como cajero o gestor, Inicio / Diario / Bóveda / Chica / Verificar / Login tampoco tienen hueco ni KPI de una columna.

**Aceptación**

- `/inicio` (admin y analista): `dash-analista` y `dash-head` sin `min-height` reservado en ≤991 px.
- `/tesoreria/boveda`: KPI sin reserva de 200 px en móvil.
- Caja chica, verificar, diario, clientes, `credix-page--stats-3`, informes: stats `auto-fit` ≥88 px en ≤640 px (no `1fr`).
- `/login` y `body`: `translate="no"` + alto `100dvh`. Portales Ant heredan de `body`.
- Evidencia: [audit-modulos.md](audit-modulos.md) (SSD-01…14). No se reescriben 95 TSX.

## Fuera de alcance

- App nativa / PWA store.
- Reescribir 95 páginas una a una (el shell cubre `CredixPage` / Crud / Informe / WorkCanvas).
- Inventar `usp_*`, TEA, ROSEL.
- Densidad automática ya existe (SSD-13); no se reabre.

## Relación

Complementa SSD-13 (experiencia) y SSD-14 (chrome sede móvil). No sustituye RoleHome.
