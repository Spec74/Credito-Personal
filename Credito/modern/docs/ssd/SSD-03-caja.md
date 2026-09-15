# SSD-03 — Caja (diario, chica, saldos, verificar)

**Estado:** as-built · 2026-09-11  
**Código:** `/caja/*` · `/api/v1/credito/*` (caja) + venta rápida de mostrador  
**Fuente legacy:** `CajaDiarioController`, `CajaChicaController`, `SaldosController`, `VerificarPagosController`, `CajaController` (maestro), vista `Credito/CajaDiario.cshtml`  
**Doc de ingeniería:** [caja_diario_migracion.md](../caja_diario_migracion.md), [caja_saldos_migracion.md](../caja_saldos_migracion.md), [caja_chica_verificar_migracion.md](../caja_chica_verificar_migracion.md)

## 1. Propósito y actores

Operación de efectivo del día: cobrar, desembolsar, arqueo, cierre, caja chica y verificación de transferencias.

| Rol | Uso |
|-----|-----|
| Gestor / cajero | Caja diario de su sesión (cobros, desembolso APR, E/S, arqueo) |
| Encargado | Saldos, asignar, cierre masivo, conciliar (caja central) |
| Administrador | Maestro de cajas, anular movimiento, mismos cierres |
| `LECTURA_SALDO` | Solo consulta de saldos; sin asignar ni cerrar |

## 2. Alcance

**Entra**

- `/caja/diario`, `/caja/chica`, `/caja/saldos`, `/caja/asignar`, `/caja/verificar-pagos`, `/caja/maestro` (`/mantenimiento/cajas`)
- Mora postergada al cobrar; solicitar condonación desde diario (bandeja en SSD-02)
- Tickets PDF de movimiento cuando el SP devuelve `MovimientoCajaId`

**No entra**

- Tesorería / bóveda / transferencia entre bancos → SSD-04
- Informes cobro diario, cajas asignadas, caja diario → SSD-07
- Venta rápida de mostrador como módulo → SSD-10 (el diario puede enlazar el endpoint de venta)

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/Credito/CajaDiario` | `/caja/diario` | Tabs vs pestañas jQuery; mismos `usp_*` |
| `/CajaChica` | `/caja/chica` | Gastos, rendición, arqueo, transferir bóveda |
| `/Saldos` | `/caja/saldos` | Asignadas, saldos, cierre masivo, conteo billetes |
| Asignar (modal Saldos) | Modal en `/caja/saldos` + ruta `/caja/asignar` | Mismo formulario; menú exacto sigue en `/caja/asignar` |
| `/VerificarPagos` | `/caja/verificar-pagos` | Exact menu |
| `/Caja` (mantenimiento) | `/mantenimiento/cajas` | Ítem CAJA módulo MANTENIMIENTO |
| Impresos RDLC cajas / saldo / bóveda desde Saldos | PDF API (`downloadCajasAsignadasPdf`, `downloadRptSaldosCajaPdf`, movimiento bóveda) | Ya no se abre MVC |

Menú vivo: CAJA DIARIO `Credito/CajaDiario`, CAJACHICA `CajaChica`, SALDOS CAJA `Saldos`, VERIFICAR PAGOS `VerificarPagos`, CAJA `Caja` (mantenimiento).

## 4. Contrato de datos

| Procedimiento / tabla | Uso |
|-----------------------|-----|
| `usp_PagarCuotas`, `usp_PagarCuotaPagoLibre`, `usp_PagarCuotasCancelacion` | Cobro |
| `usp_CompletarImpagos` / `usp_CompletarImpagosValidacion` | Impagos 0 del día |
| `usp_CreditoMora_Registrar` / `_Liquidar` | Mora al pagar / última cuota |
| Desembolso, E/S, anular movimiento | Mismos `usp_*` que `CajaDiarioController` |
| `usp_ReconciliarCajaDiario`, `usp_RecalcularCajaDiario`, `usp_CerrarCajaDiario` | Arqueo y cierre |
| `usp_SolicitarCondonacion` | Pedido desde diario; cierre bloquea si hay pendientes de esa caja |
| `usp_PagosNoVerificados` | Verificar Yape/Plin/transferencia |
| Caja chica: E/S, rendición, cierre, transferir bóveda | `usp_*` de `CajaChica` |
| Validar cierre + conteo billetes + cerrar cajas | Secuencia Saldos MVC |

El C# no recalcula `TotalPago` ni saldos de cuota.

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `GET caja-diario-sesion` | `CreditoUser` | `/caja/diario` |
| `POST pagar-cuotas`, `pagar-cuota-importe-libre`, `pagar-cuotas-cancelacion` | Operador | Cobranzas |
| `POST completar-impagos`, `GET completar-impagos-validacion` | Operador | Cobranzas |
| `POST realizar-desembolso` | Operador | Desembolsos |
| `POST entrada-salida-caja-diario`, `POST confirmar-clave-caja-diario` | Operador | Egreso (clave) |
| `POST anular-movimiento-caja` | `CreditoRolAnularMovimientoCaja` | Arqueo |
| `POST transferir-saldos-caja-diario`, `POST reconciliar-caja-diario`, `POST cerrar-caja-diario` | Operador | Arqueo / cierre |
| `GET pagos-no-verificados`, `POST verificar-pago-transferencia` | Operador | `/caja/verificar-pagos` |
| `GET saldos-caja-diario*`, cierre masivo / asignar | Según rol / `LECTURA_SALDO` | `/caja/saldos`, `/caja/asignar` |
| Caja chica E/S, rendición, `cerrar-caja-chica-diario`, transferir bóveda | Operador | `/caja/chica` |
| `POST solicitar-condonacion` | Operador | Diario → bandeja SSD-02 |

## 6. Seguridad

- Sesión de caja y `oficinaId` del JWT; no se opera la caja de otra oficina.
- `EXACT_MENU_ROUTES`: `/caja/asignar`, `/caja/saldos`, `/caja/verificar-pagos`, `/caja/maestro`, `/mantenimiento/cajas`. El hub `/caja` no las habilita.
- Egreso pide confirmación de clave (SPA + `UsuarioPasswordHasherCompat` en MVC).
- Anular movimiento: rol anulación o administrador.

## 7. Criterios de aceptación

- [ ] Ítem CAJA DIARIO abre sesión del día; cobro de cuota llama `usp_PagarCuotas` y emite ticket si hay `MovimientoCajaId`.
- [ ] Completar impagos confirma y registra CUO 0; el cierre usa la validación GET, no bloquea el POST.
- [ ] Última cuota con mora liquida `usp_CreditoMora_Liquidar` (producto con `IndMora`).
- [ ] Cierre de caja falla con mensaje de condonaciones pendientes de esa caja.
- [ ] Verificar pagos confirma transferencia y actualiza el movimiento.
- [ ] `LECTURA_SALDO` ve `/caja/saldos` sin asignar ni cierre masivo.
- [ ] PDF de cajas asignadas / saldo caja / movimiento bóveda desde Saldos usa JWT, no RDLC MVC.
- [ ] Hub `/caja` no abre asignar / saldos / verificar.

## 8. Desviaciones

- 2026-09-14 — cierre auditoría: Extension en cuotas digitales; mora última cuota server-side; arqueo anulados; detalle OV; fecha transferencia legacy ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- 2026-09-14 — anular INI+PAG: bloqueo real (no “confirmación”); observación obligatoria; E/S usa ValorTabla 13; cierre valida antes de confirmar ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- 2026-09-10 — condonación: listado/alta por oficina JWT; bloqueo de cierre como `ValidarCierreCajaDiario` ([BITACORA](../migration/BITACORA-DESVIACIONES.md))
- 2026-09-09 — mora postergada y `SCOPE_IDENTITY` en liquidación

Aceptadas de producto: grilla de desembolsos APR (el MVC era un formulario); modal de movimientos de toda la caja; tickets auto cuando el SP devuelve id; rendiciones de caja chica en línea (no pixel-perfect de modales legacy); Saldos Anular aplica bloqueo INI+PAG aunque el MVC lo tenía comentado; cobro de cuota admite tipo pago digital (legacy solo efectivo) **con** Extension/Verificar.

## 9. Pruebas y evidencia

- API: `pagarcuotasendpointtests`, `pagarcuotascancelacionendpointtests`, `completarimpagosvalidacionendpointtests`, `cerrarcajadiarioendpointtests`, `validarcierrecajadiarioendpointtests`, `reconciliarcajadiarioendpointtests`, `transferirsaldoscajadiarioendpointtests`, `saldoscierreendpointtests`, `asignarcajaendpointtests`, `cajachicaoperacionendpointtests`, `creditocondonacionendpointtests`
- SPA: `menuRouteAccess.test.ts` (hub caja vs asignar/saldos/verificar)
- Smoke: abrir diario, cobrar cuota efectivo + Yape (aparece en Verificar), verificar pago, intentar cierre con pendiente digital (bloquea), anular, cierre limpio; encargado en saldos

## 10. Go-live

**Caja Diario listo** en Development (auditoría 2026-09-14). Desplegar `CREDITO.usp_PagarCuotas.sql` en BD (API ya tiene safety-net Extension). Cutover: humo cajero + encargado (asignar, cierre masivo, caja chica) en preprod.
