# Caja chica y Verificar pagos — Estado y siguiente sprint

Spec SSD: [docs/ssd/SSD-03-caja.md](ssd/SSD-03-caja.md).

**Última revisión:** 2026-05-26  
Rutas: `/caja/chica`, `/caja/verificar-pagos`

**Estado:** UI moderna Credix aplicada (mayo 2026). Operativamente listas para go-live.

---

## Verificar pagos (`/caja/verificar-pagos`)

| Legacy (`VerificarPagos/Index.cshtml`) | Modern | Estado |
|----------------------------------------|--------|--------|
| jqGrid pagos no verificados | `CredixDataTable` + `fetchPagosNoVerificados` | ✅ Funcional |
| Doble clic → confirmar verificar | `onDoubleClick` + `Modal.confirm` | ✅ |
| `Verificar` POST | `verificarPagoTransferencia` | ✅ |
| — | Export CSV / PDF | ✅ Mejora |
| — | Botón Verificar por fila | ✅ Mejora |
| Búsqueda / debounce | `filterTableRows` + toolbar | ✅ |
| Estilos módulo Credix (`#114885`) | `caja-verificar-pagos-module.css` | ✅ |
| Stats / empty state | KPIs + `VerificarPagosTableEmpty` | ✅ |
| Paginación 45/60/75 | `pageSizeOptions` legacy | ✅ |
| Panel «VERIFICADOR DE PAGOS» | `CredixCrudPage` `panelTitle` | ✅ |

**API:** `GET pagos-no-verificados`, `POST verificar-pago-transferencia`, exportaciones en `cajaDiario` API.

**Archivos:** `VerificarPagosPage.tsx`, `components/VerificarPagosTableEmpty.tsx`, `components/CajaListToolbar.tsx`, `styles/caja-verificar-pagos-module.css`.

**Conclusión:** **Completo** (funcional + UX moderna).

---

## Caja chica (`/caja/chica`)

| Legacy (`CajaChica/Index.cshtml`) | Modern (`CajaChicaPage.tsx`) | Estado |
|-----------------------------------|------------------------------|--------|
| KPIs saldo inicial/entradas/salidas/neto | Stats + sesión query | ✅ |
| Tab GASTOS | Tab gastos — usuario, operación, E/S | ✅ |
| Tab RENDICION | Rendiciones pendientes + comprobantes | ✅ |
| Tab ARQUEO | Movimientos + tickets PDF | ✅ |
| Cerrar caja chica | Cierre sesión | ✅ |
| Transferir a bóveda | Drawer transferencia | ✅ |
| Enlace desde Saldos «Operar caja chica» | `SaldosPage` actions | ✅ |
| Cierre + transferir en tab ARQUEO (no tab extra) | Pestaña Arqueo | ✅ Paridad MVC |
| Confirmación Si/No gastos | `Modal.confirm` | ✅ |
| Búsqueda usuario debounce | `useDebouncedValue` 400 ms | ✅ |
| Filtro rendición / arqueo | `CajaListToolbar` | ✅ |
| Lazy tabs | `destroyInactiveTabPane` + queries por tab | ✅ |
| CSS módulo | `caja-chica-module.css` | ✅ |

**API:** `caja-chica-sesion`, `entrada-salida-caja-chica`, rendiciones CRUD, `transferir-saldos-caja-chica-boveda`, tickets PDF, etc. (`api/cajaChica.ts`).

**Archivos:** `CajaChicaPage.tsx`, `styles/caja-chica-module.css`.

**Conclusión:** **Completo** para operación diaria. Pendiente menor: modales legacy «Rendiciones pendientes» separados (en modern tabla inline — más directo).

---

## Maestro de cajas (`/caja/maestro`, `/mantenimiento/cajas`)

| Legacy (`Caja/Index.cshtml`) | Modern | Estado |
|------------------------------|--------|--------|
| Lista cajas + buscar | `CajaListToolbar` + debounce servidor | ✅ |
| Nueva / editar modal | Modal Ant Design | ✅ |
| Oficina, gestor, activo | Formulario completo | ✅ |
| Paginación 15/30/45 | `pageSizeOptions` | ✅ |
| Doble clic editar | `onDoubleClick` → modal | ✅ |
| Activar/desactivar | Botón + confirmación | ✅ |
| Columna abierta | `indAbierto` tag | ✅ Mejora |

**Archivos:** `CajaMaestroPage.tsx`, `styles/caja-maestro-module.css`.

---

## Comprobantes caja chica (`/informes/comprobantes-caja-chica`)

| Legacy (`ReporteComprobantesCajaChica`) | Modern | Estado |
|----------------------------------------|--------|--------|
| Rango fechas | `RangePicker` | ✅ |
| Tabla comprobantes | `CredixInformePage` + búsqueda prominente | ✅ |
| Export PDF RDLC | PDF tabular API + CSV | ✅ |
| KPI importe total | Stat «Importe total» | ✅ |

**Archivos:** `ComprobantesCajaChicaPage.tsx`, `styles/comprobantes-caja-chica-module.css`.

---

## Verificación local

```powershell
cd Credito\modern\Credito.Modern.Web
npm run build
```

Probar:

- `/caja/verificar-pagos`, `/caja/chica`, `/caja/maestro`
- `/informes/comprobantes-caja-chica`

---

## Referencias

- `CAJA_DIARIO_MIGRACION.md` — cobro diario (completo)
- `CAJA_SALDOS_MIGRACION.md` — saldos y cierres (completo mayo 2026)
- `BOVEDA_MIGRACION.md` — bóveda
