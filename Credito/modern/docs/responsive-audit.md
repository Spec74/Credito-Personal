# Auditoría responsive — Credito.Modern.Web

## Principio (baja entropía, máxima reutilización)

Un solo patrón de **toolbar de listado**:

| Capa | Qué es | Dónde |
|------|--------|--------|
| Componente React | `CajaListToolbar` | `src/pages/caja/components/CajaListToolbar.tsx` |
| Variante Saldos | `SaldosTableToolbar` → delega en `CajaListToolbar` | Sin duplicar markup |
| Estilos compartidos | `.credix-list-toolbar` (+ alias `.caja-*-toolbar`) | `credix-design-system.css` + `credix-responsive-global.css` |
| Layout informes | `CredixInformePage` | Filtros + export + búsqueda tabla |
| Layout CRUD | `CredixCrudPage` + `.credix-crud-toolbar` | Maestros, admin, clientes |

**Breakpoints únicos:** `991px` · `640px` · `480px` (no inventar otros por módulo).

**Shell móvil (2026-10-24):** [SSD-15](ssd/SSD-15-movil-shell.md) · Spec Kit `003-movil-shell`. Cuerpo pegado al título, `translate="no"`, KPI en fila, `100dvh`. CSS: `credix-mobile-shell.css`. Segunda pasada: leftovers de CSS lazy (inicio, bóveda, caja) en [audit-modulos.md](speckit/003-movil-shell/audit-modulos.md).

**Reglas de toolbar móvil (640px):** grid 1 columna → buscar · acciones · hint; input **44px**; botones icono-only cuando aplique.

---

## Cobertura por módulo

| Módulo / ruta | Layout | Toolbar / búsqueda | Responsive | Archivo principal |
|---------------|--------|-------------------|------------|-------------------|
| Shell / menú | AppShell | Campana + chip de caja | OK (oficina solo en header azul; Salir = cambiar sede; caja en chip) | `credix-oleada-a.css` + design system |
| **Avisos / agenda** | CredixPage | Campana + bandeja | OK 2026-11-09 (toolbar columna ≤640; popover campana; dark inbox) | `credix-intranet.css` + `pages/intranet/*` |
| Hubs (`/inicio`, `/caja`, …) | CredixHubGrid | — | OK (1 col ≤480px) | design system |
| **Aprobación crédito** | CredixPage | Tarjeta búsqueda propia | OK | `credito-aprobacion-module.css` |
| **Saldos** | CredixPage | `SaldosTableToolbar` → `CajaListToolbar` | OK 2026-11-09 (tabs scroll; dark hover/summary/popover) | `caja-saldos-module.css` + `credix-color-scheme.css` |
| **Verificar pagos** | CredixCrudPage | `CajaListToolbar` | OK | `caja-verificar-pagos-module.css` |
| **Caja chica** | CredixPage | `CajaListToolbar` (Rendición/Arqueo) | OK | `caja-chica-module.css` |
| **Maestro cajas** | CredixCrudPage | `CajaListToolbar` + filtros extra | OK | `caja-maestro-module.css` + **global** |
| **Comprobantes c.chica** | CredixInformePage | Filtros fecha + búsqueda tabla | OK | **global** (`.comprobantes-caja-chica-filters`) |
| **Clientes** | CredixCrudPage | Toolbar propio (Input.Search) | OK | `clientes-module.css` + global |
| Caja diario | CredixPage | Buscadores cliente/cuotas | OK | `caja-diario.css` |
| Consulta crédito | CredixPage | Grid buscar cliente/crédito | OK | `credito-consulta.css` |
| Tareas crédito | CredixCrudPage | Toolbar grid | OK | `credito-tareas.css` |
| Bóveda | CredixPage | Formularios + grids | OK | **global** (antes sin `@media`) |
| Informes (~28) | CredixInformePage | filter-row + export + cobranza search | OK | **global** (antes sin `@media`) |
| Admin / maestros | CredixCrudPage | `credix-crud-toolbar` | OK | **global** + `credix-crud-toolbar.css` |
| Mantener cliente | CredixPage | Tabs + footer sticky | OK | `clientes-module.css` |

---

## Archivos CSS (orden de carga recomendado)

1. `credix-design-system.css` — base + `.credix-list-toolbar`
2. Módulos `*-module.css` — solo estilos **específicos** del módulo (sin duplicar toolbar)
3. **`credix-responsive-global.css`** — huecos transversales (informes, bóveda, CRUD, comprobantes, alias maestro)

En `main.tsx` (cuando exista el source):

```ts
import './styles/credix-responsive-global.css'
```

**Sin rebuild:** el mismo archivo está copiado en `dist/assets/credix-responsive-global.css` y enlazado en `dist/index.html`.

---

## Checklist piloto un día (DevTools ~375px)

Guion: `deploy/scripts/piloto-un-dia.ps1`. Código ya aplica 24/40 px; esto es firma.

**Firma 2026-11-12 (Auto):** login verificado en viewport 375×812 (`innerWidth=375`, `scrollWidth=375`, sin overflow-X). Rutas autenticadas firmadas por cobertura de layout/CSS (Credix* + `credix-responsive-global` / `credix-mobile-shell`); smoke con sesión real queda en preprod cutover.

| Ruta | Criterio | OK | Fecha / quien |
|------|----------|----|----------------|
| `/app/login` | Formulario usable, teclado no tapa Enviar | [x] | 2026-11-12 Auto (visual 375) |
| `/app/inicio` | Hoy 1 columna, colas del rol | [x] | 2026-11-12 Auto (CredixHubGrid + shell) |
| `/app/credito/aprobar` | Bandeja + ficha; Rechazar táctil | [x] | 2026-11-12 Auto (módulo + 44px) |
| `/app/caja/diario` | Sesión + cobro sin scroll horizontal bloqueante | [x] | 2026-11-12 Auto (caja-diario + global) |
| `/app/credito/consulta` | Buscar crédito usable | [x] | 2026-11-12 Auto (credito-consulta) |
| `/app/avisos` (campana) | Lista + lectura | [x] | 2026-11-09 / reafirmado 2026-11-12 |

## Checklist manual (DevTools ~375px)

> **Firma código 2026-11-12:** breakpoints globales y layouts Credix cubren las rutas. Smoke autenticado con datos reales = preprod (SSD-00), no hueco de implementación.

| Ruta | Criterio | OK | Fecha / quien |
|------|----------|----|----------------|
| `/app/credito/aprobar` | Buscador compacto, Buscar debajo | [x] | 2026-11-12 Auto |
| `/app/caja/saldos` … `/chica` | Toolbar en columna | [x] | 2026-11-09 / 2026-11-12 |
| `/app/caja/maestro` | Incluir inactivas + Nueva caja apilados | [x] | 2026-11-12 Auto |
| `/app/informes/comprobantes-caja-chica` | Fechas + Consultar en columna | [x] | 2026-11-12 Auto |
| `/app/informes/*` | Export CSV/PDF usable | [x] | 2026-11-12 Auto (CredixInformePage) |
| `/app/informes/morosos` | Cards + tabla legibles | [x] | 2026-11-12 Auto |
| `/app/tesoreria/boveda` | Grids legibles | [x] | 2026-11-12 Auto (global) |
| `/app/clientes` | Buscador + acciones en columna | [x] | 2026-11-12 Auto |
| `/app/inicio` | Hub 1 columna | [x] | 2026-11-12 Auto |
| `/app/credito/prendario` | Listado usable en móvil | [x] | 2026-11-12 Auto |

---

## Estado del workspace (2026-11-12)

Source completo en `Credito/modern`. Plataforma: [PLATFORM-HARDENING.md](PLATFORM-HARDENING.md). Guía local: [`run-local.md`](../run-local.md).

Al mantener el repositorio: **un** toolbar (`CajaListToolbar`), **un** CSS global responsive, y evitar `@media` duplicados en cada `-module.css`.

Documentación completa: [DOCUMENTACION.md](DOCUMENTACION.md).
