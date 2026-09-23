import { getAccessToken } from '../auth/tokenStorage'
import { apiDownload, apiFetch, getApiBaseUrl } from './client'
import { ApiError, parseApiError } from './errors'
import type {
  CajaDiarioSesion,
  CajaDiarioVentaRapida,
  CreditoPorPersonaRow,
  CompletarImpagosValidacion,
  CuentaPorCobrarPendienteRow,
  CuotasPendientesRow,
  DesembolsoPendienteRow,
  PagosNoVerificadosRow,
  PagoCajaResult,
  RptSaldosCajaRow,
  TipoOperacionListItem,
  ValidarCierreCajaDiario,
  VerificarPagoTransferenciaResult,
} from '../types/api'

export function fetchCajaDiarioVentaRapida(
  oficinaId: number,
): Promise<CajaDiarioVentaRapida> {
  return apiFetch<CajaDiarioVentaRapida>(
    `/ventas/caja-diario-venta-rapida?oficinaId=${oficinaId}`,
  )
}

/** null = usuario sin caja abierta en la oficina (404 de negocio, paridad MVC). */
export async function fetchCajaDiarioSesion(
  oficinaId: number,
): Promise<CajaDiarioSesion | null> {
  const headers = new Headers({ Accept: 'application/json' })
  const token = getAccessToken()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const res = await fetch(
    `${getApiBaseUrl()}/credito/caja-diario-sesion?oficinaId=${oficinaId}`,
    { headers },
  )

  if (res.status === 404) {
    let raw: Record<string, unknown> | undefined
    try {
      raw = (await res.json()) as Record<string, unknown>
    } catch {
      return null
    }
    const detail = String(raw.detail ?? raw.Detail ?? '').toLowerCase()
    if (
      !detail ||
      detail.includes('caja') ||
      detail.includes('abierta') ||
      detail.includes('asignada')
    ) {
      return null
    }
    const msg =
      (typeof raw.detail === 'string' && raw.detail) ||
      (typeof raw.Detail === 'string' && raw.Detail) ||
      'No encontrado'
    throw new ApiError(msg, 404)
  }

  if (!res.ok) {
    throw await parseApiError(res)
  }

  return (await res.json()) as CajaDiarioSesion
}

export { buscarClientes } from './clientes'

export function fetchCreditosPorPersona(
  personaId: number,
  esCajaCentral: boolean,
): Promise<CreditoPorPersonaRow[]> {
  return apiFetch<CreditoPorPersonaRow[]>(
    `/credito/creditos-por-persona?personaId=${personaId}&esCajaCentral=${esCajaCentral}`,
  )
}

export function pagarCuotaImporteLibre(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
  importeRecibido: number
  tipoPagoId: number
  fechaPagoTransferencia?: string | null
}): Promise<PagoCajaResult> {
  return apiFetch<PagoCajaResult>('/credito/pagar-cuota-importe-libre', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function validarAnularMovimientoCaja(
  oficinaId: number,
  movimientoCajaId: number,
): Promise<{ bloqueadoPorPagosCuota: boolean }> {
  return apiFetch(
    `/credito/validar-anular-movimiento-caja?oficinaId=${oficinaId}&movimientoCajaId=${movimientoCajaId}`,
  )
}

export function reconciliarCajaDiario(body: {
  oficinaId: number
  cajaDiarioId: number
}): Promise<{ resultCode: number; cajaDiarioId: number | null }> {
  return apiFetch('/credito/reconciliar-caja-diario', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function validarDesembolso(
  oficinaId: number,
  cajaDiarioId: number,
  creditoId: number,
): Promise<{ puedeDesembolsar: boolean; mensaje: string | null }> {
  return apiFetch(
    `/credito/validar-desembolso?oficinaId=${oficinaId}&cajaDiarioId=${cajaDiarioId}&creditoId=${creditoId}`,
  )
}

export function fetchSaldoCuentaCajaDiario(
  cajaDiarioId: number,
  tipoPagoId = 1,
): Promise<{ saldo: number | null }> {
  return apiFetch<{ saldo: number | null }>(
    `/credito/obtener-saldo-cuenta-caja-diario?cajaDiarioId=${cajaDiarioId}&tipoPagoId=${tipoPagoId}`,
  )
}

export function fetchResumenIngresoCaja(
  cajaDiarioId: number,
  oficinaId: number,
): Promise<{ texto: string }> {
  return apiFetch<{ texto: string }>(
    `/credito/rpt-saldos-caja-resumen-ingreso?cajaDiarioId=${cajaDiarioId}&oficinaId=${oficinaId}`,
  )
}

export function fetchRptSaldosCaja(
  cajaDiarioId: number,
  indCajaChica = false,
  incluirAnulados = false,
): Promise<RptSaldosCajaRow[]> {
  const q = new URLSearchParams({
    cajaDiarioId: String(cajaDiarioId),
    indCajaChica: String(indCajaChica),
  })
  if (incluirAnulados) {
    q.set('incluirAnulados', 'true')
  }
  return apiFetch<RptSaldosCajaRow[]>(`/credito/rpt-saldos-caja?${q}`)
}

export function fetchMovimientoCajaDetalleOv(
  oficinaId: number,
  movimientoCajaId: number,
): Promise<{
  movimientoCajaId: number
  oficinaId: number
  operacion: string
  lineas: string[]
}> {
  return apiFetch(
    `/credito/movimiento-caja-detalle-ov?oficinaId=${oficinaId}&movimientoCajaId=${movimientoCajaId}`,
  )
}

export function downloadRptSaldosCajaCsv(
  cajaDiarioId: number,
  indCajaChica = false,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-saldos-caja-csv?cajaDiarioId=${cajaDiarioId}&indCajaChica=${indCajaChica}`,
    `arqueo-caja-${cajaDiarioId}.csv`,
  )
}

export function downloadRptSaldosCajaPdf(
  cajaDiarioId: number,
  indCajaChica = false,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-saldos-caja-pdf?cajaDiarioId=${cajaDiarioId}&indCajaChica=${indCajaChica}`,
    `arqueo-caja-${cajaDiarioId}.pdf`,
  )
}

export type MovimientoCajaAnularPreview = {
  movimientoCajaId: number
  cajaDiarioId: number
  oficinaId: number
  operacion: string
  descripcion: string | null
  persona: string | null
  fechaReg: string
  importePago: number
  puedeAnular: boolean
  bloqueo: string | null
}

export function fetchMovimientoCajaAnular(
  oficinaId: number,
  movimientoCajaId: number,
): Promise<MovimientoCajaAnularPreview> {
  return apiFetch(
    `/credito/movimiento-caja-anular?oficinaId=${oficinaId}&movimientoCajaId=${movimientoCajaId}`,
  )
}

export function downloadMovimientoCajaTicketPdf(
  oficinaId: number,
  movimientoCajaId: number,
): Promise<void> {
  return apiDownload(
    `/credito/movimiento-caja-ticket-pdf?oficinaId=${oficinaId}&movimientoCajaId=${movimientoCajaId}`,
    `ticket-movimiento-caja-${movimientoCajaId}.pdf`,
  )
}

export function fetchCuotasPendientes(
  creditoId: number,
  indCancelacion = false,
): Promise<CuotasPendientesRow[]> {
  const q = new URLSearchParams({
    creditoId: String(creditoId),
    indCancelacion: String(indCancelacion),
  })
  return apiFetch<CuotasPendientesRow[]>(`/credito/cuotas-pendientes?${q}`)
}

export function pagarCuotas(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
  listaPlanPagoId: string
  importeRecibido: number
  tipoPagoId?: number
  fechaPagoTransferencia?: string | null
  esUltimaCuota?: boolean
  aplicarMoraPostergada?: boolean
}): Promise<PagoCajaResult> {
  return apiFetch<PagoCajaResult>('/credito/pagar-cuotas', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      tipoPagoId: 1,
      aplicarMoraPostergada: true,
      ...body,
    }),
  })
}

export function pagarCuotasCancelacion(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
}): Promise<PagoCajaResult> {
  return apiFetch<PagoCajaResult>('/credito/pagar-cuotas-cancelacion', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchCuentasPorCobrarPendientes(
  oficinaId: number,
  cajaDiarioId: number,
  personaId = 0,
): Promise<CuentaPorCobrarPendienteRow[]> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    cajaDiarioId: String(cajaDiarioId),
    personaId: String(personaId),
  })
  return apiFetch<CuentaPorCobrarPendienteRow[]>(
    `/credito/cuentas-por-cobrar-pendientes?${q}`,
  )
}

export function pagarCuentaPorCobrar(body: {
  oficinaId: number
  cajaDiarioId: number
  ordenVentaId: number
  cuentaxCobrarId: number
}): Promise<PagoCajaResult> {
  return apiFetch<PagoCajaResult>('/credito/pagar-cuenta-por-cobrar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchDesembolsosPendientes(
  oficinaId: number,
  personaId = 0,
): Promise<DesembolsoPendienteRow[]> {
  return apiFetch<DesembolsoPendienteRow[]>(
    `/credito/desembolsos-pendientes?oficinaId=${oficinaId}&personaId=${personaId}`,
  )
}

export function realizarDesembolso(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
}): Promise<{ movimientoCajaId: number; yaRegistrado: boolean }> {
  return apiFetch('/credito/realizar-desembolso', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchTipoOperaciones(): Promise<TipoOperacionListItem[]> {
  return apiFetch<TipoOperacionListItem[]>('/tipo-operaciones')
}

export function entradaSalidaCajaDiario(body: {
  oficinaId: number
  cajaDiarioId: number
  personaId: number
  tipoOperacionId: number
  importe: number
  descripcion: string | null
  tipoPagoId: number
}): Promise<{ resultCode: number }> {
  return apiFetch('/credito/entrada-salida-caja-diario', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function anularMovimientoCaja(body: {
  oficinaId: number
  movimientoCajaId: number
  observacion?: string | null
}): Promise<{ resultCode: number; movimientoCajaId: number }> {
  return apiFetch('/credito/anular-movimiento-caja', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function validarCierreCajaDiario(
  oficinaId: number,
  cajaDiarioId: number,
): Promise<ValidarCierreCajaDiario> {
  return apiFetch<ValidarCierreCajaDiario>(
    `/credito/validar-cierre-caja-diario?oficinaId=${oficinaId}&cajaDiarioId=${cajaDiarioId}`,
  )
}

export function cerrarCajaDiario(body: {
  oficinaId: number
  cajaDiarioId: number
}): Promise<{ cajaDiarioId: number }> {
  return apiFetch('/credito/cerrar-caja-diario', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function completarImpagosValidacion(
  oficinaId: number,
  cajaDiarioId: number,
): Promise<CompletarImpagosValidacion> {
  return apiFetch<CompletarImpagosValidacion>(
    `/credito/completar-impagos-validacion?oficinaId=${oficinaId}&cajaDiarioId=${cajaDiarioId}`,
  )
}

export function completarImpagos(body: {
  oficinaId: number
  cajaDiarioId: number
}): Promise<{ success: boolean }> {
  return apiFetch('/credito/completar-impagos', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchPagosNoVerificados(
  oficinaId: number,
): Promise<PagosNoVerificadosRow[]> {
  return apiFetch<PagosNoVerificadosRow[]>(
    `/credito/pagos-no-verificados?oficinaId=${oficinaId}`,
  )
}

export function downloadPagosNoVerificadosCsv(oficinaId: number): Promise<void> {
  return apiDownload(
    `/credito/pagos-no-verificados-csv?oficinaId=${oficinaId}`,
    'pagos-no-verificados.csv',
  )
}

export function downloadPagosNoVerificadosPdf(oficinaId: number): Promise<void> {
  return apiDownload(
    `/credito/pagos-no-verificados-pdf?oficinaId=${oficinaId}`,
    'pagos-no-verificados.pdf',
  )
}

export function verificarPagoTransferencia(body: {
  oficinaId: number
  movimientoCajaId: number
}): Promise<VerificarPagoTransferenciaResult> {
  return apiFetch<VerificarPagoTransferenciaResult>(
    '/credito/verificar-pago-transferencia',
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    },
  )
}

export function recalcularCajaDiario(body: {
  oficinaId: number
  cajaDiarioId: number
}): Promise<{ resultCode: number; cajaDiarioId: number | null }> {
  return apiFetch('/credito/recalcular-caja-diario', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface CajaAbiertaTransferenciaRow {
  cajaId: number
  etiqueta: string
  usuarioAsignadoId: number
}

export function fetchCajasAbiertasTransferencia(
  oficinaId: number,
): Promise<CajaAbiertaTransferenciaRow[]> {
  return apiFetch<CajaAbiertaTransferenciaRow[]>(
    `/credito/cajas-abiertas-transferencia-boveda?oficinaId=${oficinaId}`,
  )
}

export function transferirSaldosCajaDiario(body: {
  oficinaId: number
  cajaDiarioId: number
  importe: number
  descripcion: string
  cajaIdDestino?: number | null
}): Promise<{ success: boolean }> {
  return apiFetch('/credito/transferir-saldos-caja-diario', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function confirmarClaveCajaDiario(
  clave: string,
): Promise<{ autorizado: boolean; mensaje: string | null }> {
  return apiFetch('/credito/confirmar-clave-caja-diario', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ clave }),
  })
}

export function fetchTieneCxcPendiente(creditoId: number): Promise<boolean> {
  return apiFetch<{ tienePendientes: boolean }>(
    `/credito/tiene-cxc-pendiente?creditoId=${creditoId}`,
  ).then((r) => r.tienePendientes)
}

export interface CreditoGestorPendienteRow {
  creditoId: number
  personaCodigo: string
  personaNombre: string
  montoCredito: number
  personaId: number
  /** Deuda pendiente real (capital + mora); paridad CreditoPendienteJGrid. */
  deudaPendiente: number
}

export function fetchCreditosGestorDesembolsados(): Promise<
  CreditoGestorPendienteRow[]
> {
  return apiFetch<CreditoGestorPendienteRow[]>(
    '/credito/creditos-gestor-desembolsados',
  )
}

export function cobrarPlanillaBloque(body: {
  oficinaId: number
  cajaDiarioId: number
  planilla: Array<{
    creditoId: number
    montoPagar: number
    tipoPagoId: number
    fechaHoraTrans?: string | null
  }>
}): Promise<{
  exito: boolean
  mensaje: string
  pagosProcesados: number
  impagosCompletados: number
}> {
  return apiFetch('/credito/cobrar-planilla-bloque', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface CreditoMoraRow {
  creditoMoraId: number
  creditoId: number
  movimientoCajaId: number | null
  fecha: string
  mora: number
  diasAtrazo: number
  saldoMora: number
  interesMora: number
}

export interface CreditoMoraResumen {
  indMoraProducto: boolean
  saldoPostergado: number
}

export function fetchCreditoMora(creditoId: number): Promise<CreditoMoraRow[]> {
  return apiFetch<CreditoMoraRow[]>(`/credito/credito-mora?creditoId=${creditoId}`)
}

export function fetchCreditoMoraResumen(
  creditoId: number,
): Promise<CreditoMoraResumen> {
  return apiFetch<CreditoMoraResumen>(
    `/credito/credito-mora-resumen?creditoId=${creditoId}`,
  )
}

export function pagarCuotaConMora(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
  importeRecibido: number
  esUltimaCuota: boolean
  tipoPagoId?: number
  fechaPagoTransferencia?: string | null
}): Promise<{ movimientoCajaId: number | null; mensaje: string; moraLiquidada: boolean }> {
  return apiFetch('/credito/pagar-cuota-con-mora', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tipoPagoId: 1, ...body }),
  })
}

