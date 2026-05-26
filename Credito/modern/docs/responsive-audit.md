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

**Reglas de toolbar móvil (640px):** grid 1 columna → buscar · acciones · hint; input **44px**; botones icono-only cuando aplique.

---

## Cobertura por módulo

| Módulo / ruta | Layout | Toolbar / búsqueda | Responsive | Archivo principal |
|---------------|--------|-------------------|------------|-------------------|
| Shell / menú | AppShell | — | OK | `credix-design-system.css` |
| Hubs (`/inicio`, `/caja`, …) | CredixHubGrid | — | OK (1 col ≤480px) | design system |
| **Aprobación crédito** | CredixPage | Tarjeta búsqueda propia | OK | `credito-aprobacion-module.css` |
| **Saldos** | CredixPage | `SaldosTableToolbar` → `CajaListToolbar` | OK | `caja-saldos-module.css` + list-toolbar |
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

## Checklist manual (DevTools ~375px)

- [ ] `/app/credito/aprobar` — buscador compacto, botón Buscar debajo
- [ ] `/app/caja/saldos`, `/app/caja/verificar-pagos`, `/app/caja/chica` — toolbar en columna
- [ ] `/app/caja/maestro` — Incluir inactivas + Nueva caja apilados
- [ ] `/app/informes/comprobantes-caja-chica` — fechas + Consultar en columna
- [ ] `/app/informes/*` — export CSV/PDF usable
- [ ] `/app/tesoreria/boveda` — grids legibles
- [ ] `/app/clientes` — buscador + acciones en columna
- [ ] `/app/inicio` — hub 1 columna

---

## Estado del workspace (mayo 2026)

El **source completo** (`src/`, `.csproj`) puede estar ausente; el build en **`dist/`** y **`bin/`** conserva la app. Ver [`RUN-LOCAL.md`](../RUN-LOCAL.md) para ejecutar sin `dotnet run` / `npm run dev`.

Al restaurar el repositorio: mantener **un** toolbar (`CajaListToolbar`), **un** CSS global responsive, y evitar `@media` duplicados en cada `-module.css`.
