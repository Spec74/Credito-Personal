# Saldos y cierres — Paridad Legacy MVC ↔ Modern

Spec SSD: [docs/ssd/SSD-03-caja.md](ssd/SSD-03-caja.md).

Documento de verificación de migración (`Credito/Web/Views/Saldos/Index.cshtml` → `Credito.Modern.Web` `/caja/saldos`).

**Última revisión:** 2026-05-26  
**Ruta SPA:** `/caja/saldos`  
**Asignación caja (modal legacy):** `/caja/asignar`  
**API base:** `/api/v1/credito/*` (saldos, cierre, bóveda)

---

## Resumen ejecutivo

| Categoría | Estado |
|-----------|--------|
| Grilla cajas asignadas + footer totales | ✅ Completo |
| Saldos caja diario / caja chica (consulta) | ✅ Completo |
| Saldos por bóveda abierta | ✅ Completo (pestaña dedicada; legacy filtraba en misma grilla) |
| Asignar caja + monto bóveda | ✅ Completo (modal en `/caja/saldos`; ruta `/caja/asignar` reutiliza el mismo formulario) |
| Validar cierre + conteo billetes + cerrar cajas | ✅ Completo |
| Transferir cierre caja chica | ✅ Completo |
| Post-cierre bóveda (cartera/calificación) | ✅ Completo |
| Reportes RDLC cajas / bóveda / saldo por fila | ✅ Completo (QuestPDF paridad RDLC; bóveda con efectivo / digitales en cabecera) |
| Rol `LECTURA_SALDO` (solo lectura) | ✅ Completo |
| UI Credix moderna, búsqueda, lazy tabs | ✅ Completo |
| Panel «Anular movimiento» en misma pantalla | ⚪ Fuera de alcance (ver Caja diario) |

**Conclusión:** **Saldos y cierres** está **completo para go-live operativo**. Las diferencias restantes son de **ubicación UX** (anular movimiento) o **mejoras** respecto al MVC.

---

## Mapa legacy → modern

| Legacy (`Saldos/Index.cshtml`) | Modern | Notas |
|--------------------------------|--------|-------|
| `grdCajadiario` — cajas asignadas | Tab **Cajas asignadas** | Mismas columnas; footer `TOTAL CAJAS` |
| `btnNuevo` — asignar caja | Botón **Asignar caja** abre modal (misma pantalla) | Tras guardar se refrescan las grillas; no navega a diario |
| `btnReporteCajas` | `downloadCajasAsignadasPdf` | QuestPDF paridad `rptCajasAsignadas.rdlc` |
| `btnReporteBoveda` | `downloadRptMovimientoBovedaPdf` | QuestPDF paridad `rptMovimientoBoveda.rdlc` |
| `btnTransferir` — CERRAR CAJAS | Tab **Cierre masivo** + `ConteoBilletesModal` | Validación previa + sobrante |
| `dialog_ConteoBillete` (11 denominaciones) | `ConteoBilletesModal.tsx` | Regla conteo ≥ importe cierre |
| `Transferir` + `ActualizarDatosPostCierreBoveda` | `cerrarCajasDiarios` + post-cierre auto/manual (saldo + calificar + `usp_IntentarGenerarCierreGerencialMensual`) | Paridad secuencia MVC |
| `grdBuscar` — saldos caja diario | Tab **Saldos caja diario** | Lazy query al activar tab |
| `grdCajaChica` — saldos chica | Tab **Saldos caja chica** | Lazy query |
| `btnCerrarCajaChica` | Tab **Cierre masivo** → `transferirCierreCajaChica` | Tras `validar-cierre-caja-chica` |
| `ImprimirSaldo` por fila | `downloadRptSaldosCajaPdf` | QuestPDF paridad `rptSaldoCaja.rdlc` (cabecera + INGRESOS/EGRESOS) |
| `ViewBag.EsLectura` / ENCARGADO vs PRINCIPAL | `esLecturaSaldoCaja` / tag vista | Sin asignar ni cerrar; sí puede ver grillas e imprimir PDF |
| Panel anular movimiento (admin) | — | **Caja diario → Arqueo** (`anular-movimiento-caja`) |
| Búsqueda/paginación jqGrid servidor | Historial: paginación y búsqueda en servidor (`page`, `pageSize`, `buscar`); cajas asignadas: filtro cliente instantáneo | Paridad `Skip/Take`; el conjunto del día es acotado y se filtra en memoria |

---

## Endpoints API (modern)

| Endpoint | Paridad |
|----------|---------|
| `GET cajas-asignadas` | `ListarCajasAsignadas` |
| `GET saldos-caja-diario` | `ListarSaldoCajaDiario` (paginado: `page`, `pageSize`, `buscar`) |
| `GET saldos-caja-chica-diario` | `ListarSaldoCajaChicaDiario` (paginado; sin filtro de oficina, igual que el legado) |
| `GET saldos-caja-diario-boveda` | Listado con `BovedaId` (paginado) |
| `GET validar-cierre-saldos` | `ValidarCierre` |
| `GET validar-cierre-caja-chica` | `ValidarCierreCajaChica` |
| `POST cerrar-cajas-diarios` | `Transferir` (sobrante) |
| `POST transferir-cierre-caja-chica` | `TransferirCierreCajaChica` |
| `POST actualizar-datos-post-cierre-boveda` | `ActualizarDatosPostCierreBoveda` (+ `usp_IntentarGenerarCierreGerencialMensual`) |
| `GET rpt-saldos-caja-pdf` | `ReporteSaldoCaja` (`rptSaldoCaja.rdlc`: cabecera + INGRESOS/EGRESOS) |
| `GET rpt-cajas-asignadas-pdf` | `ReporteCajasAsignadas` |
| `GET rpt-movimiento-boveda-pdf` | `ReporteMovimientoBoveda` (+ resumen efectivo / medios digitales en cabecera) |
| `GET boveda-abierta` | Bóveda activa oficina |

Asignación: `GET cajas-para-asignar` (incluye cajero), `GET monto-boveda-asignacion`, `POST asignar-caja` (política `CreditoNoLecturaSaldo`). Modal en Saldos; `/caja/asignar` es el mismo formulario.

---

## UI moderna (mayo 2026)

| Criterio | Implementación |
|----------|----------------|
| Colores Credix | `caja-saldos-module.css` — `#114885`, `--credix-brand-light` |
| Rendimiento | React Query lazy por tab; historial paginado en servidor con `keepPreviousData`; columnas memoizadas |
| Búsqueda | Historial: `buscar` al servidor con debounce 350 ms; cajas asignadas: `filterTableRows` sin round-trip |
| Responsive | Toolbar wrap, tablas `mode="operacion"`, columnas secundarias por breakpoint (`responsive`), modal a ancho de viewport |
| Tabla ancha | Rueda del mouse → scroll horizontal; `Saldo final` anclado a la derecha; degradado cuando hay más columnas |
| Usabilidad | Stats KPI, badges rol, header sticky, orden por columna en cajas asignadas, estados vacíos explicativos |

**Archivos:** `SaldosPage.tsx`, `components/SaldosSesionTable.tsx`, `components/ConteoBilletesModal.tsx`, `components/SaldosTableToolbar.tsx`, `utils/cajaSaldosPermisos.ts`, `utils/tableClientFilter.ts`, `utils/tableSorters.ts`, `styles/caja-saldos-module.css`.

---

## Diferencias aceptadas (no bloquean)

1. **Anular movimiento:** en legacy convive en `Saldos/Index` para admin; en modern está en **Caja diario → Arqueo** (misma API, mejor contexto operativo).
2. **Asignar caja:** el modal vive en Saldos (paridad MVC). `/caja/asignar` sigue existiendo para el menú y reutiliza el mismo formulario; al guardar vuelve a Saldos.
   - **Cajero, no combo de usuario:** en el MVC el `cboUsuario` está comentado y el POST manda `pUsuarioAsignadoId: 0`; `CajaDiarioBL.AsignarUsuarioCaja` usa `Caja.CajeroId`. La regla no cambia: la caja siempre se abre a nombre de `Caja.CajeroId`.
   - **Modernización (mejora sobre el MVC):** el modal muestra el cajero de la caja elegida. El **administrador** puede corregirlo ahí mismo: se guarda en el maestro (`POST /api/v1/cajas/guardar`, política `CreditoRolAdministrador`) y recién después se asigna. Los demás perfiles lo ven como solo lectura y las cajas sin cajero quedan deshabilitadas, en vez de fallar con «No tiene adignado un Cajero.» después de guardar como en el legado.
3. **Doble clic:** no aplica en saldos (legacy tampoco en estas grillas de saldo).
4. **Importe cierre:** legacy `toFixed(1)` en footer; modern suma precisión completa de `saldoFinal` (más exacto).
5. **Tab bóveda:** pestaña explícita vs filtro `BovedaId` en postData jqGrid.
6. **Totales del historial:** el jqGrid no los mostraba; modern los calcula en SQL sobre todo el filtro (no solo la página). Ver bitácora de desviaciones.
7. **Orden de columnas:** el historial se sirve por fecha descendente desde el servidor (paridad `sortname` por defecto); el orden interactivo se ofrece solo en cajas asignadas, que está completa en memoria.
8. **`LECTURA_SALDO` e informes:** el MVC ocultaba «Reporte cajas» y «Reporte bóveda» junto con los botones de escritura (`ViewBag.EsLectura`). En modern esos PDF son solo lectura y siguen visibles: el rol no puede asignar ni cerrar, pero sí consultar e imprimir.

---

## Verificación local

```powershell
cd Credito\modern\Credito.Modern.Web
npm run build

cd Credito\modern
dotnet build Credito.Modern.Api/Credito.Modern.Api.csproj
```

**Pruebas manuales sugeridas**

1. Usuario **PRINCIPAL:** asignadas → reportes → cierre con conteo ≥ saldo → post-cierre.
2. Usuario **`LECTURA_SALDO`:** solo grillas y reportes permitidos; sin cierre ni asignar.
3. Imprimir saldo en fila (PDF QuestPDF con cabecera de sesión).
4. Cerrar caja chica desde tab cierre cuando validación OK.

---

## Pantallas siguientes (módulo caja)

| Ruta | Legacy | Estado migración |
|------|--------|------------------|
| `/caja/chica` | `CajaChica/Index` | API + UI operativa; **siguiente:** pulir UX Credix (paridad visual) |
| `/caja/verificar-pagos` | `VerificarPagos/Index` | Funcional completo; **siguiente:** toolbar/búsqueda/debounce estilo Saldos |

Ver `CAJA_CHICA_VERIFICAR_MIGRACION.md` para checklist detallado del siguiente sprint.
