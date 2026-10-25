# Auditoría móvil — todos los módulos (003)

**Fecha:** 2026-10-24 · Segunda pasada (después del shell).  
**Método:** inventario de `*Page.tsx` + CSS con `min-height` / `100vh` / stats 1-col.  
**No se abre 004:** sigue siendo el mismo cambio de producto (cuerpo pegado al título + español intacto).

## Cómo se hereda

| Capa | Quién la usa | ¿Cubre el shell? |
|------|----------------|------------------|
| `CredixPage` | crédito, caja, tesorería, informes, intranet, dashboards, hubs, RoleHome, placeholder | Sí |
| `CredixCrudPage` | usuarios, roles, oficinas, maestros, clientes, tareas, aprobar, verificar, lista precios | Sí (envuelve Page) |
| `CredixInformePage` | ~28 informes + movimientos bóveda | Sí |
| `CredixWorkCanvas` | simulador, venta rápida, entrada almacén | Sí (hijo del body) |
| **Fuera del Page** | `LoginPage`, splash `#root`, portales Ant (`document.body`) | No hasta esta pasada |

Casi **95/97** páginas ya van por Page/Crud/Informe. El hueco no era “falta reescribir 95 archivos”. Era **CSS de módulo que se carga después del shell** y le gana.

## Hallazgos (hueco real vs no es hueco)

| SSD | Superficies | ¿Hueco título→cuerpo? | Qué se hizo / por qué no |
|-----|-------------|------------------------|---------------------------|
| **01 Auth / inicio** | Login | `100vh`, sin `translate` | `100dvh` + `translate="no"` en wrap y `body` |
| **01** | `/inicio` admin y analista | `dash-analista` 28rem + `dash-head` 9.5rem (CLS) | Reserva **solo escritorio**; móvil `min-height: 0` |
| **01** | RoleHome / Hoy | Page + stats default | Cubierto por shell |
| **01** | Hubs de módulo | `CredixModuleHubPage` | Cubierto; grid 1 col ≤480 es correcto |
| **02 Crédito** | Simulador, tareas | Evidencia original (capturas) | Shell 1ª pasada |
| **02** | Consulta, aprobar, condonar, parámetros | Page/Crud; `space-between` es toolbar, no columna de página | Sin reserva de alto |
| **03 Caja** | Diario | Stats 2-col + última celda a todo el ancho ≤720 | Compacto `auto-fit` ≤640; última celda ya no come fila |
| **03** | Chica, verificar | `@media 640` forzaba **1 columna** (carga *después* del shell) | `auto-fit` 88px |
| **03** | Saldos, maestro, asignar, cobro bloque | Page; `space-between` en cards | No empujan el body |
| **03** | Modal ruta cobrador | `100dvh` en el **modal** fullscreen | Intencional; no es la página |
| **04 Bóveda** | Estado | KPI `min-height: 200px` en móvil | Reserva solo ≥992 |
| **04** | Fondeo, red, movimientos | Page / Informe | Cubierto |
| **05 Clientes** | Listado | `clientes-page` stats 1-col ≤640 | `auto-fit` |
| **05** | Ficha / alta | Page; footer sticky (no hueco superior) | Sin cambio |
| **06 Prendario** | Nuevo, gestión, listado | Page; `space-between` en filas | Sin reserva de alto |
| **07 Informes** | Hub + ~28 + cierre + morosos | Informe/Page; stats 3→2→1 | 1-col revertido a `auto-fit` |
| **08 Admin** | Usuarios, roles, oficinas, wizard, comisiones | Crud/Page | Cubierto |
| **09 Maestros** | 5 CRUD | Crud | Cubierto |
| **10 Ventas** | Hub, rápida, orden, precios, canje | Page + WorkCanvas (1 col &lt;992) | Cubierto |
| **11 Almacén** | Entrada, salida, transfer, kardex, constancia | Page/Informe/Canvas | Cubierto |
| **12 Validación** | N/A layout | — | Fuera de alcance |
| **14 Intranet** | Avisos, admin avisos, agenda | Page; `space-between` en cards | Cubierto |
| **Chrome** | Header, drawer, footer | Footer ya oculto ≤640 | `body` + `html` `notranslate`; portales Ant heredan de `body` |
| **Visor RDLC** | `ReportViewerPage` | `100vh` a propósito (iframe) | No tocar |

## Qué no es hueco (no parchear)

- `justify-content: space-between` en **toolbars, cards, session-bar**: reparte izquierda/derecha, no empuja el body hacia abajo.
- `flex: 1 1 auto` en campos de formulario.
- `min-height: 100dvh` de **modales** fullscreen (mora, ruta cobrador).
- WorkCanvas: ya es 1 columna bajo 992 px.
- Reescribir 95 TSX, app nativa, `usp_*`, i18n.

## Causa raíz (2ª pasada)

`main.tsx` importa `credix-mobile-shell.css` **antes** del CSS lazy de cada página. Misma especificidad → gana el archivo que llega último. Por eso el shell no bastaba en Inicio, bóveda, caja chica y verificar.
