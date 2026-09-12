# Modernización UI — estado y criterios

Specs SSD: [SSD-08 admin](ssd/SSD-08-admin.md), [SSD-09 maestros](ssd/SSD-09-maestros.md), [SSD-10 ventas](ssd/SSD-10-ventas.md), [SSD-11 almacén](ssd/SSD-11-almacen.md). Este archivo sigue siendo el índice visual Credix.

Auditoría de la SPA **Credito.Modern.Web** (mayo 2026): paridad funcional con MVC + diseño Credix moderno.

## Criterios de calidad

| Criterio | Implementación |
|----------|----------------|
| Marca | Color `#114885`, logo `/brand/credix.png`, tipografía system-ui |
| Componentes | `CredixPage`, `CredixDataTable`, `CredixInformePage`, `CredixCrudPage` |
| Iconos | Ant Design + mapa `hubLinkIcons.tsx` + menú legacy `menuIcons.tsx` |
| Responsive | Breakpoints 991 / 640 / 480 px; `credix-responsive-global.css` |
| Rendimiento | Lazy routes, React Query (`staleTime`), CSS sin duplicados en `main.tsx` |
| Paridad MVC | Docs `*_migracion.md` por módulo; RDLC opcional vía `VITE_LEGACY_ORIGIN` |

## Módulos operativos (UI completa)

| Módulo | Rutas hub | Pantallas | Doc migración |
|--------|-----------|-----------|---------------|
| **Inicio** | `/inicio` | Tablero analista / gerencial (oficina JWT); mapa `?vista=modulos` | — |
| **Crédito** | `/credito` | Consulta, simulador, aprobar, tareas, parámetros | `credito_migracion.md` |
| **Clientes** | `/clientes` | Listado + mantener (geocode, validaciones) | `clientes_migracion.md` |
| **Caja** | `/caja` | Diario, chica, saldos, asignar, verificar, maestro | `caja_*_migracion.md` |
| **Tesorería** | `/tesoreria` | Bóveda, movimiento bóveda | `boveda_migracion.md` |
| **Ventas** | `/ventas` | Venta rápida, orden, lista precios, canje puntos | [SSD-10](ssd/SSD-10-ventas.md) |
| **Almacén** | `/almacen` | Entrada, salida, transferencia, kardex, movimiento, constancia | [SSD-11](ssd/SSD-11-almacen.md) |
| **Maestros** | `/maestros` | Marcas, modelos, tipos, artículos, almacenes | — |
| **Admin** | `/admin` | Usuarios, roles, oficinas | — |
| **Informes** | `/informes` | ~30 informes JSON/CSV/PDF | API `catalogo-cobertura` |
| **Reportes** | `/reportes/*` | Índice crédito/almacén/venta, cobranza, visor RDLC | — |

## Pendientes menores (no bloquean operación)

1. **Comisiones** (`/admin/comisiones`) — **Hecho** (paridad del vacío: el MVC no calcula).
2. **Informes `solo-mvc`** — ver matriz en `/informes/cobertura` o `GET /api/v1/reportes/catalogo-cobertura`.
3. **Reporte crédito/venta índice** — PDF/Excel del índice usan la API moderna (JWT). El puente RDLC queda en `legacyReportUrls.ts` / `creditolegacyreports.ts` por si el cutover lo necesita (las páginas ya no lo llaman).
4. **PDF QuestPDF** — datos del `usp_*`; layout tabular Credix (catálogo de columnas RDLC). No es copia píxel a píxel.
5. **Caja chica** — rendiciones inline vs modales legacy (funcionalmente completo).
6. **Menú sin URL SPA** — **Hecho.** El menú vivo de CREDITO (oficina 1) resuelve a pantalla dedicada, incluido `Dashboard/Admin` → `/inicio`. Los padres (`CREDITO`, `SEGURIDAD`, …) no se mapean a hubs: `usp_MenuLst` los une y eso ampliaría permisos. El placeholder queda para ítems realmente desconocidos y ahora reintenta con `legacyUrl`.

## Mejoras aplicadas (mayo 2026)

- `main.tsx`: eliminación de imports CSS duplicados; carga de `credix-responsive-global.css` y `cliente-form.css`.
- `branding.ts`: colores alineados a `#114885` / Ant Design.
- Consulta crédito: métricas de vencimiento diferidas tras cargar plan (`planQuery.isSuccess`).
- Placeholder de menú: tarjetas Credix con sugerencias por módulo (`moduleHubSuggestions.ts`).
- Reportes de venta: ruta `/reportes/venta` para cubrir `Reporte/Venta` sin caer al hub genérico.
- Comisiones: pantalla de paridad (el MVC no tenía cálculo).

## Checklist de verificación manual

Responsive (~375 px): inicio, consulta crédito, caja diario/saldos/chica, clientes, bóveda, informes export, aprobar crédito.

Build:

```powershell
cd modern
dotnet build Credito.Modern.sln
cd Credito.Modern.Web
npm run build
dotnet test ../Credito.Modern.sln
```

## Siguiente iteración (opcional)

- Sustituir exports legacy restantes en `ReporteCreditoIndexPage` por rutas SPA cuando exista PDF tabular.
- Pulir modales de rendición caja chica si el negocio pide paridad pixel-perfect.
- Si el negocio define reglas de comisiones, versionar primero el SQL y luego el CRUD.
