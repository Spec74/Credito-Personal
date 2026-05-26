# Modernización UI — estado y criterios

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
| **Inicio** | `/inicio` | Hub con accesos por área | — |
| **Crédito** | `/credito` | Consulta, simulador, aprobar, tareas, parámetros | `credito_migracion.md` |
| **Clientes** | `/clientes` | Listado + mantener (geocode, validaciones) | `clientes_migracion.md` |
| **Caja** | `/caja` | Diario, chica, saldos, asignar, verificar, maestro | `caja_*_migracion.md` |
| **Tesorería** | `/tesoreria` | Bóveda, movimiento bóveda | `boveda_migracion.md` |
| **Ventas** | `/ventas` | Venta rápida, orden, lista precios, canje puntos | — |
| **Almacén** | `/almacen` | Entrada, salida, transferencia, kardex, movimiento, constancia | — |
| **Maestros** | `/maestros` | Marcas, modelos, tipos, artículos, almacenes | — |
| **Admin** | `/admin` | Usuarios, roles, oficinas | — |
| **Informes** | `/informes` | ~30 informes JSON/CSV/PDF | API `catalogo-cobertura` |
| **Reportes** | `/reportes/*` | Índice crédito/almacén, cobranza, visor RDLC | — |

## Pendientes menores (no bloquean operación)

1. **Comisiones** (`/admin/comisiones`) — reservado; legacy sin lógica; pantalla informativa Credix.
2. **Informes `solo-mvc`** — ver matriz en `/informes/cobertura` o `GET /api/v1/reportes/catalogo-cobertura`.
3. **Reporte crédito índice** — algunos exports abren RDLC legacy (`ReporteCreditoIndexPage`); resto en SPA.
4. **PDF QuestPDF** — datos equivalentes; layout distinto al RDLC (aceptado).
5. **Caja chica** — rendiciones inline vs modales legacy (funcionalmente completo).
6. **Menú sin URL SPA** — `ModulePlaceholderPage` con atajos al hub del módulo.

## Mejoras aplicadas (mayo 2026)

- `main.tsx`: eliminación de imports CSS duplicados; carga de `credix-responsive-global.css` y `cliente-form.css`.
- `branding.ts`: colores alineados a `#114885` / Ant Design.
- Consulta crédito: métricas de vencimiento diferidas tras cargar plan (`planQuery.isSuccess`).
- Placeholder de menú: tarjetas Credix con sugerencias por módulo (`moduleHubSuggestions.ts`).
- Comisiones: panel reservado con iconografía de marca.

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
- Definir reglas de comisiones y reemplazar stub por CRUD + informes.
