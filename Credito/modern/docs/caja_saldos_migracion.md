# Saldos y cierres — Paridad Legacy MVC ↔ Modern

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
| Asignar caja + monto bóveda | ✅ Completo (`/caja/asignar`) |
| Validar cierre + conteo billetes + cerrar cajas | ✅ Completo |
| Transferir cierre caja chica | ✅ Completo |
| Post-cierre bóveda (cartera/calificación) | ✅ Completo |
| Reportes RDLC cajas / bóveda / saldo por fila | ✅ Completo (PDF API + fallback legacy) |
| Rol `LECTURA_SALDO` (solo lectura) | ✅ Completo |
| UI Credix moderna, búsqueda, lazy tabs | ✅ Completo |
| Panel «Anular movimiento» en misma pantalla | ⚪ Fuera de alcance (ver Caja diario) |

**Conclusión:** **Saldos y cierres** está **completo para go-live operativo**. Las diferencias restantes son de **ubicación UX** (anular movimiento) o **mejoras** respecto al MVC.

---

## Mapa legacy → modern

| Legacy (`Saldos/Index.cshtml`) | Modern | Notas |
|--------------------------------|--------|-------|
| `grdCajadiario` — cajas asignadas | Tab **Cajas asignadas** | Mismas columnas; footer `TOTAL CAJAS` |
| `btnNuevo` — asignar caja | Botón → `/caja/asignar` | Modal MVC → página dedicada |
| `btnReporteCajas` | `openLegacyCajasAsignadas()` | RDLC vía `VITE_LEGACY_ORIGIN` |
| `btnReporteBoveda` | `openLegacyMovimientoBoveda(bovedaId)` | Prefetch bóveda en tab asignadas |
| `btnTransferir` — CERRAR CAJAS | Tab **Cierre masivo** + `ConteoBilletesModal` | Validación previa + sobrante |
| `dialog_ConteoBillete` (11 denominaciones) | `ConteoBilletesModal.tsx` | Regla conteo ≥ importe cierre |
| `Transferir` + `ActualizarDatosPostCierreBoveda` | `cerrarCajasDiarios` + post-cierre auto/manual | Paridad secuencia MVC |
| `grdBuscar` — saldos caja diario | Tab **Saldos caja diario** | Lazy query al activar tab |
| `grdCajaChica` — saldos chica | Tab **Saldos caja chica** | Lazy query |
| `btnCerrarCajaChica` | Tab **Cierre masivo** → `transferirCierreCajaChica` | Tras `validar-cierre-caja-chica` |
| `ImprimirSaldo` por fila | `downloadRptSaldosCajaPdf` + fallback `openLegacyReporteSaldoCaja` | QuestPDF + RDLC |
| `ViewBag.EsLectura` / ENCARGADO vs PRINCIPAL | `esLecturaSaldoCaja` / tag vista | Sin botones cierre/asignar |
| Panel anular movimiento (admin) | — | **Caja diario → Arqueo** (`anular-movimiento-caja`) |
| Búsqueda jqGrid servidor | Filtro cliente instantáneo (`tableClientFilter`) | Sobre datos ya cargados |

---

## Endpoints API (modern)

| Endpoint | Paridad |
|----------|---------|
| `GET cajas-asignadas` | `ListarCajasAsignadas` |
| `GET saldos-caja-diario` | `ListarSaldoCajaDiario` |
| `GET saldos-caja-chica-diario` | `ListarSaldoCajaChicaDiario` |
| `GET saldos-caja-diario-boveda` | Listado con `BovedaId` |
| `GET validar-cierre-saldos` | `ValidarCierre` |
| `GET validar-cierre-caja-chica` | `ValidarCierreCajaChica` |
| `POST cerrar-cajas-diarios` | `Transferir` (sobrante) |
| `POST transferir-cierre-caja-chica` | `TransferirCierreCajaChica` |
| `POST actualizar-datos-post-cierre-boveda` | `ActualizarDatosPostCierreBoveda` |
| `GET rpt-saldos-caja-pdf` | `ReporteSaldoCaja` |
| `GET boveda-abierta` | Bóveda activa oficina |

Asignación: `GET cajas-para-asignar`, `GET monto-boveda-asignacion`, `POST asignar-caja` (pantalla `/caja/asignar`).

---

## UI moderna (mayo 2026)

| Criterio | Implementación |
|----------|----------------|
| Colores Credix | `caja-saldos-module.css` — `#114885`, `--credix-brand-light` |
| Rendimiento | React Query lazy por tab; prefetch bóveda en asignadas/cierre |
| Búsqueda | `SaldosTableToolbar` + `filterTableRows` (sin round-trip) |
| Responsive | Toolbar wrap, tablas `mode="operacion"` |
| Usabilidad | Stats KPI, badges rol, flujo «Ir a cierre masivo» |

**Archivos:** `SaldosPage.tsx`, `components/ConteoBilletesModal.tsx`, `components/SaldosTableToolbar.tsx`, `utils/cajaSaldosPermisos.ts`, `utils/tableClientFilter.ts`, `styles/caja-saldos-module.css`.

---

## Diferencias aceptadas (no bloquean)

1. **Anular movimiento:** en legacy convive en `Saldos/Index` para admin; en modern está en **Caja diario → Arqueo** (misma API, mejor contexto operativo).
2. **Asignar caja:** página `/caja/asignar` en lugar de modal en la misma vista (misma lógica `AsignarCaja`).
3. **Doble clic:** no aplica en saldos (legacy tampoco en estas grillas de saldo).
4. **Importe cierre:** legacy `toFixed(1)` en footer; modern suma precisión completa de `saldoFinal` (más exacto).
5. **Tab bóveda:** pestaña explícita vs filtro `BovedaId` en postData jqGrid.

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
3. Imprimir saldo en fila (PDF moderno o ventana legacy).
4. Cerrar caja chica desde tab cierre cuando validación OK.

---

## Pantallas siguientes (módulo caja)

| Ruta | Legacy | Estado migración |
|------|--------|------------------|
| `/caja/chica` | `CajaChica/Index` | API + UI operativa; **siguiente:** pulir UX Credix (paridad visual) |
| `/caja/verificar-pagos` | `VerificarPagos/Index` | Funcional completo; **siguiente:** toolbar/búsqueda/debounce estilo Saldos |

Ver `CAJA_CHICA_VERIFICAR_MIGRACION.md` para checklist detallado del siguiente sprint.
