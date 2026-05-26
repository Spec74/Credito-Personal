# Caja Diario — Paridad Legacy MVC ↔ Modern

Documento de verificación de migración (`Credito/Web/Views/Credito/CajaDiario.cshtml` → `Credito.Modern.Web` `/caja/diario`).

**Última revisión:** 2026-05-18  
**Ruta SPA:** `/caja/diario`  
**API base:** `/api/v1/credito/*`, `/api/v1/ventas/caja-diario-venta-rapida`

---

## Resumen ejecutivo

| Categoría | Estado |
|-----------|--------|
| Operaciones de cobro (cuotas, libre, CxC, cancelación) | ✅ Completo |
| Desembolsos y validaciones | ✅ Completo |
| Entrada/salida + clave egreso | ✅ Completo |
| Arqueo, anular, transferir, conciliar | ✅ Completo |
| Cierre + PDF saldo | ✅ Completo |
| Informes laterales (cobros día, ruta QR, movimientos) | ✅ Completo |
| Modales / confirmaciones Si-No | ✅ Completo |
| Tickets PDF automáticos | ✅ Completo (cuando el SP devuelve `MovimientoCajaId`) |

**Conclusión:** La migración de **Caja Diario** está **funcionalmente completa** para uso operativo por gestores y administradores. Las diferencias restantes son de **UX/layout** (tabs vs pestañas jQuery) o **mejoras** respecto al legacy.

---

## Mapa botón a botón (Legacy → Modern)

| Legacy (MVC) | Modern | Notas |
|--------------|--------|-------|
| Sesión / saldos en pantalla | Sidebar + KPIs `CajaDiarioPage` | + recalcular, enlaces informes |
| `btnCxcPendiente` GAD | Cobranzas → GAD / tab CxC | |
| `btnCuotaPendiente` | Cobranzas → Cuotas pendientes | `GET creditos-gestor-desembolsados` (DES gestor) |
| `btnCompletarCoutaPendienteImpago` | Completar impagos + confirmación | |
| Buscar cliente + chips crédito | `ClienteBuscarAutoComplete` + chips | + PDF movimientos crédito |
| `btnPagar` cuotas | Pagar cuota + TieneCxc + confirm + ticket | |
| `btnPagoLibre` | Pago libre + mismas reglas | |
| `btnCancelarCredito*` | `CancelacionCreditoPanel` | |
| `btnDesembolso` / pendientes | Tab Desembolsos (APR) | Grid vs formulario único legacy |
| `btnPagarSalida` E/S | Tab Egreso/Ingreso | Confirm + clave egreso |
| `grdEntradas` / `grdSalidas` | Tab Arqueo | Dblclick → detalle movimiento |
| Anular movimiento | Arqueo → Anular | |
| `btnTransFerirSaldos` | `TransferirSaldosDrawer` | Bóveda / otra caja |
| `btnConciliar` | Arqueo (solo caja central) | |
| `btnPrevSaldoCaja` | Arqueo PDF + auto al cerrar | |
| `btnCerrar` | Tab Cierre | |
| Cobros del día PDF | Ops bar | `rpt-cobro-diario-pdf` |
| Ruta QR | `RutaCobranzaDrawer` + `RutaQrModal` | QR visual + enlace |
| `btnMovimientosCaja` | `MovimientosCajaModal` | `rpt-saldos-caja` + ticket/fila |
| Verificar pagos | `/caja/verificar-pagos` | Página dedicada |
| `TieneCxcPendiente` | `GET tiene-cxc-pendiente` | Por `creditoId` |
| `ImprimirMovCaja` | `movimiento-caja-ticket-pdf` | Auto tras pagos |

---

## Endpoints API (modern) usados por la pantalla

| Endpoint | Paridad BL / Controller |
|----------|-------------------------|
| `GET caja-diario-sesion` | `ObtenerCajaDiario` |
| `GET cuotas-pendientes` | `ListarCuotasPendientesJGrid` |
| `POST pagar-cuotas` | `PagarCuotas` |
| `POST pagar-cuota-importe-libre` | `PagarCuotasImporteLibre` |
| `POST pagar-cuotas-cancelacion` | `PagarCuotasCancelacion` |
| `GET tiene-cxc-pendiente` | `TieneCxcPendiente` |
| `GET creditos-gestor-desembolsados` | `LstCreditoPendienteJGrid` |
| `GET cuentas-por-cobrar-pendientes` | `LstCuentasxCobrarJGrid` |
| `POST pagar-cuenta-por-cobrar` | `RealizarPagarCuentaxCobrar` |
| `GET desembolsos-pendientes` | Créditos APR (desembolso) |
| `POST realizar-desembolso` | `RealizarDesembolso` |
| `POST entrada-salida-caja-diario` | `RealizarEntradaSalidaCajaDiario` |
| `POST confirmar-clave-caja-diario` | `ConfirmarClave` |
| `GET rpt-saldos-caja` | Movimientos arqueo |
| `POST anular-movimiento-caja` | Anulación |
| `POST transferir-saldos-caja-diario` | `TransferirSaldos` |
| `POST reconciliar-caja-diario` | `ConciliarCajaDiario` |
| `POST cerrar-caja-diario` | `CerrarCajaDiario` |
| `GET rpt-movimiento-credito-pdf` | `ReporteCreditoMovimiento` |
| `GET credito-mora` | `ListarCreditoMoraGrd` |
| `GET credito-mora-resumen` | `CreditoBL.ObtenerMoraPostergadaAcumulada` + `IndMora` |
| `POST pagar-cuota-con-mora` | `ProcesarPagoCuotaConMora` |
| `POST pagar-cuotas` (+ flags) | `ProcesarPagoCompleto` / registrar + liquidar mora |

---

## Mora postergada (`CreditoMora`)

Flujo legacy replicado sin cambiar reglas de BD:

1. **Registro:** al pagar cuotas con atraso/mora, si el producto tiene `IndMora = 1`, se ejecuta `CREDITO.usp_CreditoMora_Registrar`; la mora queda en `CreditoMora` con `MovimientoCajaId = NULL`.
2. **Liquidación:** al cobrar la **última cuota pendiente**, `CREDITO.usp_CreditoMora_Liquidar` crea el movimiento `MOR` y recalcula caja.
3. **Exclusión:** CREDI PRENDARIO (`ProductoId = 2`, `IndMora = 0`) no aplica mora postergada.

| Pantalla | UI modern |
|----------|-----------|
| Caja Diario → Cobranzas | Alerta mora acumulada, botón «Crédito mora», `esUltimaCuota` en pago |
| Consulta Crédito | KPI mora total/postergada, botón historial, modal `CreditoMoraModal` |

Paridad consulta pendiente: mora vigente (`usp_CalcularMoraPendiente`) + mora postergada (`SUM(SaldoMora)` donde `MovimientoCajaId IS NULL`).

---

## Diferencias aceptadas (no bloquean migración)

1. **Movimientos modal:** Legacy `ListarMovimientoCajaGrd` solo lista si hay `PersonaId`; modern muestra todos los movimientos de la caja (`rpt-saldos-caja`) con filtro cliente — **más útil en operación diaria**.
2. **Desembolsos:** Legacy formulario de un crédito; modern grilla de pendientes APR — misma lógica de negocio, mejor descubrimiento.
3. **Layout:** Tabs Ant Design vs pestañas jQuery UI; colores cuotas alineados a `Creditos.cshtml`.
4. **Tickets auto-impresión:** En legacy muchos `ImprimirMovCaja` estaban comentados; modern los activa cuando el SP devuelve ID de movimiento.
5. **Cierre:** Legacy recarga toda la página; modern navega a inicio tras cerrar (sesión ya no tiene caja).

---

## Pantallas relacionadas (fuera de `/caja/diario` pero mismo módulo)

| Pantalla modern | Legacy aproximado |
|-----------------|-------------------|
| `/caja/asignar` | Asignación caja |
| `/caja/saldos` | Saldos / cierres masivos — **✅ completo** (ver `CAJA_SALDOS_MIGRACION.md`) |
| `/caja/verificar-pagos` | Pagos transferencia — **✅ completo** (ver `CAJA_CHICA_VERIFICAR_MIGRACION.md`) |
| `/caja/chica` | Caja chica — **✅ completo** (UI mayo 2026) |
| `/caja/maestro` | Maestro cajas — **✅ completo** (UI mayo 2026) |
| `/informes/comprobantes-caja-chica` | Comprobantes rendidos — **✅ completo** |
| Informes `cobro-diario`, `saldo-cartera-caja-diario` | Reportes MVC |

---

## Verificación local

```powershell
cd Credito\modern\Credito.Modern.Web
npm run build

cd Credito\modern
dotnet build Credito.Modern.Api/Credito.Modern.Api.csproj
```

Probar con API en marcha y usuario con caja abierta: `/caja/diario`.
