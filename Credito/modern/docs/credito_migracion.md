# Módulo Crédito — Legacy → Modern

Paridad con `Web/Views/Credito/Creditos.cshtml`, `CreditoController`, `CreditoBL` y menú **Reportes → Crédito**.

## Mapa rápido

| Área legacy | Pantalla moderna | API (prefijo `/api/v1/credito`) | Estado |
|-------------|------------------|----------------------------------|--------|
| Buscar cliente + grilla créditos | `/credito/consulta` (barra cliente) | `creditos-grilla-persona`, `creditos-por-persona` | **Hecho** (UI unificada mayo 2026) |
| Ficha crédito (plan, mov., gestión) | `/credito/consulta?creditoId=` | `estado-plan-pago`, `rpt-movimiento-credito`, `credito-contexto` | **Hecho** |
| Crear solicitud / generar | `/credito/simulador` | `crear-solicitud-credito`, `crear-credito`, `simulador-credito` | **Hecho** |
| Aprobar 1ª / 2ª | `/credito/aprobar` | `creditos-por-aprobar`, `aprobar-credito`, `rechazar-credito` | **Hecho** (UI moderna mayo 2026) |
| Simulador / parámetros | `/credito/simulador`, `/credito/parametros-simulador` | `simulador-credito`, `parametros-simulador` | **Hecho** |
| Tareas | `/credito/tareas` | `tareas/*` | **Hecho** (UI moderna mayo 2026) |
| Caja diario (cobro) | `/caja/diario` | `pagar-cuotas`, `cuotas-pendientes`, mora | **Hecho** |
| Impresos plan / estado / mov. | Botones en consulta + informes | `rpt-plan-pagos-pdf`, `rpt-estado-credito-pdf`, `rpt-movimiento-credito-pdf` | **Hecho** (QuestPDF, no RDLC) |
| Informes cartera | `/informes/*`, `/reportes/credito` | `rpt-credito-*` | **Hecho** |
| Evidencias | Panel gestión + `/credito/evidencias` (ruta legacy) | `evidencias-credito`, `subir-evidencia-credito` | **Hecho** |
| Condonar, cargos, aval, tope | `CreditoConsultaGestionPanel` | `condonar-credito`, `guardar-cargo`, … | **Hecho** |
| Mora postergada | `CreditoMoraModal` | `credito-mora`, `credito-mora-resumen` | **Hecho** |
| `ListarCreditoMoraGrd` (MVC sin action) | Modal mora + SQL directo | `GET credito-mora` | **Hecho** |

## Flujo operativo recomendado (modern)

1. **Hub** → `/credito`
2. **Consulta** → buscar cliente → elegir crédito en grilla → plan / gestión / movimientos
3. **Cobro** → enlace «Cobrar en caja diario» → `/caja/diario?creditoId=`
4. **Alta** → simulador → solicitud → aprobar → desembolso en caja

## Fases pendientes (prioridad)

| Fase | Contenido | Estado |
|------|-----------|--------|
| **1** | Consulta unificada por cliente + grilla créditos | **Hecho** |
| **2** | Cabecera cliente (SBS, calificación, KPIs, crear solicitud → simulador) | **Hecho** — `GET persona-credito-ficha`, `CreditoPersonaCabecera` |
| **3** | Impresos agrupados + depurar en cabecera + rol LECTURA | **Hecho** |
| **4** | Permisos finos (APROBADOR 1 solo bandeja, ENCARGADO) | **Hecho** |
| **5** | UI familiar + PDF tabla / puente RDLC | **Hecho** |

### Fase 2 — mapeo legacy

| Legacy (`Creditos.cshtml` / `CreditoController`) | Modern |
|--------------------------------------------------|--------|
| `pPersonaId` + cabecera persona/cliente | `GET /credito/persona-credito-ficha` |
| `EstadoCliente` ACTIVO/INACTIVO | `estadoCliente` |
| `Calificacion` A/B/C → BUENO/REGULAR/MALO | `calificacionLabel` |
| `ClasificacionRiesgoSBS` (ItemId−1) → badge SBS | `clasificacionRiesgoSbsCodigo` + CSS `.credito-sbs-riesgo--*` |
| `TotalCreditos`, créditos pendientes | `totalCreditos`, `creditosPendientes` |
| `ViewBag.Depurado` | `depuradoDescripcion` + alerta |
| `btnCrearSolicitud` → `CrearSolicitudCredito` | Botón → `POST crear-solicitud-credito` → `/credito/simulador?personaId&solicitudCreditoId=` |
| Solicitud CRE existente | Tag + «Continuar solicitud» |
| `btnSimulador` / producto | Enlace «Simulador» |
| Ficha cliente PDF | `ReporteCliente` / `rpt-cliente-pdf` |

### Fase 3 — mapeo legacy

| Legacy | Modern |
|--------|--------|
| `btncImpEstadoCuenta` | `CreditoConsultaImpresosBar` → `rpt-estado-credito-pdf` |
| `btncImpPlanPago` | `rpt-plan-pagos-pdf` |
| `btncImpMovimiento` | `rpt-movimiento-credito-pdf` + CSV |
| `btnDepurarCliente` + `DepurarCredito` | Botón en cabecera → `POST depurar-persona-credito` |
| `Model.lectura` / rol LECTURA | `tieneCreditoModoLectura` — sin crear solicitud ni gestión |
| `btnCrearSolicitud` si `lectura==false` | `puedeCrearSolicitudCreditoUi` |

### Fase 4 — permisos finos

| Legacy / rol | Modern (UI) | Modern (API) |
|--------------|-------------|--------------|
| `ViewBag.Aprobador1` — anular, prorrogar, tope, analista | `puedeAnularCreditoUi`, `puedeProrrogarCreditoUi`, `puedeEditarTopeCreditoUi`, `puedeCambiarAnalistaCreditoUi` | `CreditoRolAprobador1OAdministrador` en aprobar/anular/prorrogar/tope/analista |
| `ViewBag.Administrador` — reprogramar, condonar | `puedeReprogramarCreditoUi`, `puedeCondonarCreditoUi` | `CreditoRolSoloAdministrador` |
| APROBADOR 1 sin gestor/encargado/admin | `esCreditoPerfilSoloBandeja` → hub + rutas solo `/credito/aprobar` | — |
| Rol LECTURA | `tieneCreditoModoLectura` | `CreditoRolOperador` (403 si solo LECTURA) |
| ENCARGADO — bóveda temporal | `puedeGestionarBovedaEncargadoUi` | `CreditoRolEncargadoOAdministrador` en `asignar-boveda-temporal` |

**Archivos:** `creditoOperacionPermisos.ts`, `creditoHubFilter.ts`, `CreditoOperacionRoute.tsx`, `CreditoAuthorizationPolicies.cs`.

### Fase 5 — UI y exportaciones (sin motor RDLC en API)

| Legacy | Modern |
|--------|--------|
| Cabecera `grid_12 profile` + avatar + `ul.info` KPIs | `CreditoPersonaCabecera` — franja código / totales / calificación |
| Tabla «Aval y Avalados» | `CreditoPersonaAvalesPanel` → `GET rpt-aval` |
| `.actions` impresos + ciclo | `CreditoConsultaImpresosBar` + `CreditoConsultaAccionesCredito` |
| Campos `txtc*` del crédito | `CreditoConsultaResumenCredito` → `GET rpt-estado-credito` |
| PDF RDLC plan / estado / mov. | Botones **(RDLC)** → `creditoLegacyReports.ts` (`VITE_LEGACY_ORIGIN`) |
| PDF tabla mismos datos | QuestPDF + `CreditoPdfBranding` en plan/estado ficha |

**Nota:** La API no embebe ReportViewer; paridad de **datos** en PDF moderno y diseño **legacy** opcional con sesión MVC.

## Archivos clave

- **Web:** `src/pages/credito/`, `src/config/creditoOperacionesSections.ts`
- **API:** `Credito.Modern.Api/Program.cs`, `Credito/CreditoInformesLegacyEndpoints.cs`
- **BL moderno:** `Credito.Modern.Infrastructure/CreditoPlanes/`
- **Legacy:** `Web/Views/Credito/Creditos.cshtml`, `ITB.VENDIX.BL/Creditos/CreditoBL.cs`

## SPs principales

`usp_EstadoPlanPago`, `usp_Credito_Ins/Upd/Del`, `usp_SimuladorCredito`, `usp_ReprogramarCredito`, `usp_ProrrogarCredito`, `usp_CreditoMora_Registrar/Liquidar`, `usp_RptMovimientoCredito`, `usp_RptCredito*`.

## Auditoría integral (mayo 2026)

| Criterio | Estado | Notas |
|----------|--------|-------|
| **Paridad funcional MVC** | Cumplido | Fases 1–5 documentadas; operaciones críticas en API + UI; RDLC legacy vía `VITE_LEGACY_ORIGIN` (sin motor RDLC en API). |
| **Interfaz moderna** | Cumplido | `CreditoPersonaCabecera`, paneles consulta, `credito-consulta.css`, hub filtrado por rol. |
| **Permisos / roles** | Cumplido | Fase 4: políticas API + `creditoOperacionPermisos.ts` + rutas restringidas. |
| **Rendimiento / carga** | Mejorado | `creditoStaleTime`, queries lazy por pestaña (`mov`, `gestion` con `activo`), `destroyInactiveTabPane`, clave grilla unificada, `placeholderData` en listados paginados. |
| **Tablas autoajustables** | Cumplido | `CredixDataTable` `mode="operacion"`: `tableLayout="auto"`, `scroll.x: max-content` (consulta plan/mov, grilla cliente, persona). |
| **Buenas prácticas frontend** | Cumplido | React Query con stale times, invalidaciones por prefijo, URL `creditoId` vía `useEffect`, componentes por dominio. |

### Tareas — paridad MVC (`Views/Tareas/Index.cshtml`)

| Legacy | Modern |
|--------|--------|
| Filtros PEN / COM | `Segmented` + API `?estado=` |
| Buscar tareas (cliente) | Campo búsqueda en toolbar (cliente) |
| Filtro analista (`cboFiltroAnalista`) | `Select` analista (cliente, sobre listado cargado) |
| Nueva tarea / Guardar / Eliminar / Completar | Modal + API `guardar`, `eliminar`, `completar` |
| Autocomplete crédito desembolsado | `TareasBuscarCredito` (debounce + botón + tabla) |
| Subtareas predeterminadas (combo) | `TAREAS_SUBTAREAS_PREDETERMINADAS` en modal |
| Exportar PDF (header) | Botón → `downloadRptCreditoTareaPdf` (PDF agrupado por cliente) |
| Informe RDLC / tabular | `/informes/credito-tarea` + `TareaReportePdfDocument` (grupos por nombre) + CSV ordenado |
| Búsqueda listado | Multi-palabra (`tareaListSearch.ts`) |
| Búsqueda crédito modal | `TareaBusquedaTerminos` + DNI/nombre/N° crédito/código `[…]` |
| Filas completadas (verde / tachado) | Clase `.credito-tareas-row--completada` |
| Solo admin/aprobador crea | `GET tareas/puede-editar` |

**Archivos:** `TareasPage.tsx`, `TareaEditorModal.tsx`, `TareasBuscarCredito.tsx`, `credito-tareas.css`.

**Menú:** en legacy el ítem **Tareas** abre `/Tareas/Index` directo (no hub). En modern, `legacyRoutes` + `resolveSpaPathFromMenuItem` envían **Tareas** y **Cliente** a `/credito/tareas` y `/clientes`; el hub `/credito` queda para entradas genéricas o acceso desde Inicio.

**Pendientes menores (no bloquean go-live):**

- PDF QuestPDF ≠ pixel-perfect RDLC (diseño distinto; datos agrupados por cliente en orden alfabético).
- `GET metricas-vencimiento-credito` sigue cargando al seleccionar crédito (podría diferirse a pestaña KPI).
- `TareasPage`: listado sin paginación servidor (igual que legacy; filtros búsqueda/analista en cliente).
- Avales: tabla Ant `Table` (no `CredixDataTable`); columnas con anchos fijos razonables.

**Utilidades añadidas:** `src/utils/creditoQueryOptions.ts`, `src/utils/creditoGrillaQueryKey.ts`, `CredixDataTable` modo `operacion`.

### Bandeja Aprobar — paridad MVC (`Views/CreditoAprobar/Index.cshtml`)

**Última auditoría:** 2026-05-26 — **completo para go-live.**

| Legacy | Modern | Estado |
|--------|--------|--------|
| Título «CREDITOS POR APROBAR» | `panelTitle` + `CreditoAprobarPage` | ✅ |
| Buscar por nombre (Enter / botón) | Búsqueda multi-término: cliente, DNI, código, crédito, gestor | ✅ Potenciado (mayo 2026) |
| jqGrid columnas código/cliente/monto/interés/gestor | `CredixDataTable` + orden servidor | ✅ |
| Paginación 15/30/45 | `pageSizeOptions` 15/30/45 | ✅ |
| Orden default gestor ASC | `sortField: agente`, `sortOrder: asc` | ✅ |
| Doble clic → `Creditos?pPersonaId` | Doble clic → `/credito/consulta?creditoId=` | ✅ Mejora UX |
| Imprimir ficha (`ReporteCliente`) | Botón «Ficha» → `/informes/reporte-cliente?personaId=` | ✅ |
| Aprobar 1ª/2ª (otra pantalla MVC) | Botones 1.ª / 2.ª en bandeja + confirmación | ✅ Mejora |
| — | Rechazar solicitud | ✅ API `rechazar-credito` |
| — | Consulta crédito en fila | ✅ |
| Rol solo bandeja APROBADOR 1 | `CreditoOperacionRoute` + hub filtrado | ✅ |

**UI:** `credito-aprobacion-module.css`, stats KPI, empty `AprobarTableEmpty`, `placeholderData` en listado.

**Archivos:** `CreditoAprobarPage.tsx`, `components/AprobarSearchToolbar.tsx`, `components/AprobarTableEmpty.tsx`, `utils/creditoAprobarSearch.ts`, `CreditosPorAprobarReadService.cs`, `CreditoBandejaBusquedaSql.cs`, `styles/credito-aprobacion-module.css`.

**Búsqueda API:** cada palabra (máx. 8) debe coincidir en nombre, DNI, código, `CreditoId` o gestor; columnas `Documento` en grilla.

**Diferencias aceptadas:** doble clic por crédito (no por persona); aprobar/rechazar en bandeja (legacy solo listaba + imprimía).

## Auditoría integral (mayo 2026)

| Criterio | Estado | Notas |
|----------|--------|-------|
| **Paridad funcional MVC** | Cumplido | Fases 1–5 documentadas; operaciones críticas en API + UI; RDLC legacy vía `VITE_LEGACY_ORIGIN` (sin motor RDLC en API). |
| **Interfaz moderna** | Cumplido | `CreditoPersonaCabecera`, paneles consulta, `credito-consulta.css`, hub filtrado por rol. |
| **Permisos / roles** | Cumplido | Fase 4: políticas API + `creditoOperacionPermisos.ts` + rutas restringidas. |
| **Rendimiento / carga** | Mejorado | `creditoStaleTime`, queries lazy por pestaña (`mov`, `gestion` con `activo`), `destroyInactiveTabPane`, clave grilla unificada, `placeholderData` en listados paginados. |
| **Tablas autoajustables** | Cumplido | `CredixDataTable` `mode="operacion"`: `tableLayout="auto"`, `scroll.x: max-content` (consulta plan/mov, grilla cliente, persona). |
| **Buenas prácticas frontend** | Cumplido | React Query con stale times, invalidaciones por prefijo, URL `creditoId` vía `useEffect`, componentes por dominio. |

### Tareas — paridad MVC (`Views/Tareas/Index.cshtml`)

| Legacy | Modern |
|--------|--------|
| Filtros PEN / COM | `Segmented` + API `?estado=` |
| Buscar tareas (cliente) | Campo búsqueda en toolbar (cliente) |
| Filtro analista (`cboFiltroAnalista`) | `Select` analista (cliente, sobre listado cargado) |
| Nueva tarea / Guardar / Eliminar / Completar | Modal + API `guardar`, `eliminar`, `completar` |
| Autocomplete crédito desembolsado | `TareasBuscarCredito` (debounce + botón + tabla) |
| Subtareas predeterminadas (combo) | `TAREAS_SUBTAREAS_PREDETERMINADAS` en modal |
| Exportar PDF (header) | Botón → `downloadRptCreditoTareaPdf` (PDF agrupado por cliente) |
| Informe RDLC / tabular | `/informes/credito-tarea` + `TareaReportePdfDocument` (grupos por nombre) + CSV ordenado |
| Búsqueda listado | Multi-palabra (`tareaListSearch.ts`) |
| Búsqueda crédito modal | `TareaBusquedaTerminos` + DNI/nombre/N° crédito/código `[…]` |
| Filas completadas (verde / tachado) | Clase `.credito-tareas-row--completada` |
| Solo admin/aprobador crea | `GET tareas/puede-editar` |

**Archivos:** `TareasPage.tsx`, `TareaEditorModal.tsx`, `TareasBuscarCredito.tsx`, `credito-tareas.css`.

**Menú:** en legacy el ítem **Tareas** abre `/Tareas/Index` directo (no hub). En modern, `legacyRoutes` + `resolveSpaPathFromMenuItem` envían **Tareas** y **Cliente** a `/credito/tareas` y `/clientes`; el hub `/credito` queda para entradas genéricas o acceso desde Inicio.

**Pendientes menores (no bloquean go-live):**

- PDF QuestPDF ≠ pixel-perfect RDLC (diseño distinto; datos agrupados por cliente en orden alfabético).
- `GET metricas-vencimiento-credito` sigue cargando al seleccionar crédito (podría diferirse a pestaña KPI).
- `TareasPage`: listado sin paginación servidor (igual que legacy; filtros búsqueda/analista en cliente).
- Avales: tabla Ant `Table` (no `CredixDataTable`); columnas con anchos fijos razonables.

**Utilidades añadidas:** `src/utils/creditoQueryOptions.ts`, `src/utils/creditoGrillaQueryKey.ts`, `CredixDataTable` modo `operacion`.

### Bandeja Aprobar — paridad MVC (`Views/CreditoAprobar/Index.cshtml`)

**Última auditoría:** 2026-05-26 — **completo para go-live.**

| Legacy | Modern | Estado |
|--------|--------|--------|
| Título «CREDITOS POR APROBAR» | `panelTitle` + `CreditoAprobarPage` | ✅ |
| Buscar por nombre (Enter / botón) | Búsqueda multi-término: cliente, DNI, código, crédito, gestor | ✅ Potenciado (mayo 2026) |
| jqGrid columnas código/cliente/monto/interés/gestor | `CredixDataTable` + orden servidor | ✅ |
| Paginación 15/30/45 | `pageSizeOptions` 15/30/45 | ✅ |
| Orden default gestor ASC | `sortField: agente`, `sortOrder: asc` | ✅ |
| Doble clic → `Creditos?pPersonaId` | Doble clic → `/credito/consulta?creditoId=` | ✅ Mejora UX |
| Imprimir ficha (`ReporteCliente`) | Botón «Ficha» → `/informes/reporte-cliente?personaId=` | ✅ |
| Aprobar 1ª/2ª (otra pantalla MVC) | Botones 1.ª / 2.ª en bandeja + confirmación | ✅ Mejora |
| — | Rechazar solicitud | ✅ API `rechazar-credito` |
| — | Consulta crédito en fila | ✅ |
| Rol solo bandeja APROBADOR 1 | `CreditoOperacionRoute` + hub filtrado | ✅ |

**UI:** `credito-aprobacion-module.css`, stats KPI, empty `AprobarTableEmpty`, `placeholderData` en listado.

**Archivos:** `CreditoAprobarPage.tsx`, `components/AprobarSearchToolbar.tsx`, `components/AprobarTableEmpty.tsx`, `utils/creditoAprobarSearch.ts`, `CreditosPorAprobarReadService.cs`, `CreditoBandejaBusquedaSql.cs`, `styles/credito-aprobacion-module.css`.

**Búsqueda API:** cada palabra (máx. 8) debe coincidir en nombre, DNI, código, `CreditoId` o gestor; columnas `Documento` en grilla.

**Diferencias aceptadas:** doble clic por crédito (no por persona); aprobar/rechazar en bandeja (legacy solo listaba + imprimía).
