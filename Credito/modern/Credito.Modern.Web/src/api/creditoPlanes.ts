import { apiDownload, apiFetch, apiOpenInTab } from './client'
import type {
  CalcularTemResult,
  ClientesInactivosParams,
  ClientesNuevosMesParams,
  CobroDiarioParams,
  GestorInformeParams,
  CreditoCondonadoParams,
  CreditoMorosidadParams,
  CreditoVencidoFranja,
  CreditoVencidoParams,
  EstadoPlanPagoCuota,
  RptClientesBloqueadosRow,
  RptClientesInactivosRow,
  RptClientesTopeCreditoRow,
  RptCobroDiarioRow,
  RptCreditoCondonadoRow,
  RptCreditoMorosidadRow,
  RptCreditoObservadoRow,
  RptCreditoRentabilidadRow,
  RptCreditoVencidoRow,
  RptCajaDiarioRow,
  RptCajasAsignadasRow,
  RptMovimientoCreditoRow,
  CajaDiarioInformeParams,
  CreditoRentabilidadParams,
  CreditoAprobacionInformeParams,
  InformeRangoGestorParams,
  ComprobantesCajaChicaParams,
  CreditoTareaReportParams,
  MovimientoCajaAnuladoParams,
  RptComprobantesCajaChicaRow,
  RptCreditoTareaRow,
  RptEstadoCreditoInforme,
  RptClienteInforme,
  RptPlanPagosRow,
  RptCreditoAprobacionRow,
  RptCreditosActivosRow,
  RptCreditosCierresRow,
  RptCreditosMorososPagadosRow,
  RptMovimientoCajaAnuladoRow,
  ReporteCreditoParams,
  RptCreditoRow,
  SaldoCarteraCajaDiarioParams,
  RptSaldoCarteraCajaDiarioRow,
  RptCobroDiarioDetalleRow,
  RptAvalRow,
  CajaDiarioOperacionResponse,
  CrearSolicitudCreditoResponse,
  CreditoCicloOperacionResponse,
  CrearCreditoResponse,
  SimuladorCreditoCuota,
  SimuladorCreditoRequest,
  ValidarAnularCreditoResponse,
} from '../types/api'

export function simularCredito(
  body: SimuladorCreditoRequest,
): Promise<SimuladorCreditoCuota[]> {
  return apiFetch<SimuladorCreditoCuota[]>('/credito/simulador-credito', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function calcularTem(tea: number, formaPago: string): Promise<CalcularTemResult> {
  return apiFetch<CalcularTemResult>('/credito/calcular-tem', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tea, formaPago }),
  })
}

export function fetchEstadoPlanPago(creditoId: number): Promise<EstadoPlanPagoCuota[]> {
  return apiFetch<EstadoPlanPagoCuota[]>(
    `/credito/estado-plan-pago?creditoId=${creditoId}`,
  )
}

export function fetchMovimientosCredito(
  creditoId: number,
): Promise<RptMovimientoCreditoRow[]> {
  return apiFetch<RptMovimientoCreditoRow[]>(
    `/credito/rpt-movimiento-credito?creditoId=${creditoId}`,
  )
}

export function fetchMoraPendiente(
  creditoId: number,
): Promise<{ moraPendiente: number | null }> {
  return apiFetch<{ moraPendiente: number | null }>(
    `/credito/calcular-mora-pendiente?creditoId=${creditoId}`,
  )
}

export function downloadMovimientosCreditoCsv(creditoId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-movimiento-credito-csv?creditoId=${creditoId}`,
    `movimiento-credito-${creditoId}.csv`,
  )
}

export function downloadMovimientosCreditoPdf(creditoId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-movimiento-credito-pdf?creditoId=${creditoId}`,
    `movimiento-credito-${creditoId}.pdf`,
  )
}

export function openMovimientosCreditoPdfInTab(creditoId: number): Promise<void> {
  return apiOpenInTab(`/credito/rpt-movimiento-credito-pdf?creditoId=${creditoId}`)
}

function queryGestorInforme(p: GestorInformeParams): string {
  const q = new URLSearchParams()
  if (p.oficinaId != null && p.oficinaId > 0) {
    q.set('oficinaId', String(p.oficinaId))
  }
  if (p.usuarioId != null && p.usuarioId > 0) {
    q.set('usuarioId', String(p.usuarioId))
  }
  return q.toString()
}

export type CobroDiarioQueryParams = GestorInformeParams & { soloMora?: boolean }

function queryCobroDiario(p: CobroDiarioQueryParams): string {
  const q = new URLSearchParams()
  if (p.usuarioId != null && p.usuarioId > 0) {
    q.set('usuarioId', String(p.usuarioId))
  }
  if (p.oficinaId != null && p.oficinaId > 0) {
    q.set('oficinaId', String(p.oficinaId))
  }
  if (p.soloMora) {
    q.set('soloMora', 'true')
  }
  return q.toString()
}

function queryClientesNuevosMesOpcional(
  params: Pick<ClientesNuevosMesParams, 'oficinaId' | 'usuarioId'> &
    Partial<Pick<ClientesNuevosMesParams, 'fechaIni' | 'fechaFin'>>,
): string {
  const q = new URLSearchParams({ oficinaId: String(params.oficinaId) })
  if (params.usuarioId != null && params.usuarioId > 0) {
    q.set('usuarioId', String(params.usuarioId))
  }
  if (params.fechaIni) {
    q.set('fechaIni', params.fechaIni)
  }
  if (params.fechaFin) {
    q.set('fechaFin', params.fechaFin)
  }
  return q.toString()
}

export function fetchCobroDiario(
  params: CobroDiarioQueryParams,
): Promise<RptCobroDiarioRow[]> {
  return apiFetch<RptCobroDiarioRow[]>(
    `/credito/rpt-cobro-diario?${queryCobroDiario(params)}`,
  )
}

export function fetchCajaPorCajero(usuarioId: number): Promise<{ denominacion: string }> {
  return apiFetch<{ denominacion: string }>(
    `/credito/caja-por-cajero?usuarioId=${usuarioId}`,
  )
}

export function downloadCobroDiarioCsv(params: CobroDiarioQueryParams): Promise<void> {
  const name = params.soloMora ? 'morosidad-gestor.csv' : 'cobro-diario.csv'
  return apiDownload(`/credito/rpt-cobro-diario-csv?${queryCobroDiario(params)}`, name)
}

export function downloadCobroDiarioPdf(params: CobroDiarioQueryParams): Promise<void> {
  const name = params.soloMora ? 'morosidad-gestor.pdf' : 'cobro-diario.pdf'
  return apiDownload(`/credito/rpt-cobro-diario-pdf?${queryCobroDiario(params)}`, name)
}

export function downloadMorosidadGestorCsv(params: CobroDiarioQueryParams): Promise<void> {
  return apiDownload(
    `/credito/rpt-morosidad-gestor-csv?${queryCobroDiario({ ...params, soloMora: true })}`,
    'morosidad-gestor.csv',
  )
}

export function downloadMorosidadGestorPdf(params: CobroDiarioQueryParams): Promise<void> {
  return apiDownload(
    `/credito/rpt-morosidad-gestor-pdf?${queryCobroDiario({ ...params, soloMora: true })}`,
    'morosidad-gestor.pdf',
  )
}

export interface GenerarRutaCobrosResponse {
  exito: boolean
  urlCortita: string | null
  mensaje: string | null
}

export function generarRutaCobros(
  creditoIds: number[],
): Promise<GenerarRutaCobrosResponse> {
  return apiFetch<GenerarRutaCobrosResponse>('/credito/generar-ruta-cobros', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ creditoIds }),
  })
}

function queryClientesInactivos(
  params: Pick<ClientesInactivosParams, 'oficinaId' | 'usuarioId'> &
    Partial<Pick<ClientesInactivosParams, 'fechaIni' | 'fechaFin'>>,
): string {
  const q = new URLSearchParams({ oficinaId: String(params.oficinaId) })
  if (params.usuarioId != null && params.usuarioId > 0) {
    q.set('usuarioId', String(params.usuarioId))
  }
  if (params.fechaIni) {
    q.set('fechaIni', params.fechaIni)
  }
  if (params.fechaFin) {
    q.set('fechaFin', params.fechaFin)
  }
  return q.toString()
}

export function fetchClientesInactivos(
  params: ClientesInactivosParams,
): Promise<RptClientesInactivosRow[]> {
  return apiFetch<RptClientesInactivosRow[]>(
    `/credito/rpt-clientes-inactivos?${queryClientesInactivos(params)}`,
  )
}

export function downloadClientesInactivosCsv(
  params: ClientesInactivosParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-inactivos-csv?${queryClientesInactivos(params)}`,
    'clientes-inactivos.csv',
  )
}

export function downloadClientesInactivosPdf(
  params: ClientesInactivosParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-inactivos-pdf?${queryClientesInactivos(params)}`,
    'clientes-inactivos.pdf',
  )
}

/** Paridad caja gestor legacy: sin rango de fechas (null en el SP). */
export function downloadClientesInactivosPdfGestor(params: {
  oficinaId: number
  usuarioId?: number
}): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-inactivos-pdf?${queryClientesInactivos(params)}`,
    'clientes-inactivos.pdf',
  )
}

function franjaToQueryFlags(franja: CreditoVencidoFranja): {
  vencidoMenor60?: string
  vencidoMayor60?: string
  vencidoIrrecuperable?: string
} {
  switch (franja) {
    case 'menor60':
      return { vencidoMenor60: 'S' }
    case 'mayor60':
      return { vencidoMayor60: 'S' }
    case 'irrecuperable':
      return { vencidoIrrecuperable: 'S' }
    default:
      return {}
  }
}

function queryCreditoVencido(params: CreditoVencidoParams): string {
  const q = new URLSearchParams({ oficinaId: String(params.oficinaId) })
  const flags = franjaToQueryFlags(params.franja)
  if (flags.vencidoMenor60) {
    q.set('vencidoMenor60', flags.vencidoMenor60)
  }
  if (flags.vencidoMayor60) {
    q.set('vencidoMayor60', flags.vencidoMayor60)
  }
  if (flags.vencidoIrrecuperable) {
    q.set('vencidoIrrecuperable', flags.vencidoIrrecuperable)
  }
  return q.toString()
}

export function fetchCreditoVencido(
  params: CreditoVencidoParams,
): Promise<RptCreditoVencidoRow[]> {
  return apiFetch<RptCreditoVencidoRow[]>(
    `/credito/rpt-credito-vencido?${queryCreditoVencido(params)}`,
  )
}

export function downloadCreditoVencidoCsv(
  params: CreditoVencidoParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-vencido-csv?${queryCreditoVencido(params)}`,
    'credito-vencido.csv',
  )
}

export function downloadCreditoVencidoPdf(
  params: CreditoVencidoParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-vencido-pdf?${queryCreditoVencido(params)}`,
    'credito-vencido.pdf',
  )
}

export function fetchClientesBloqueados(
  params: GestorInformeParams,
): Promise<RptClientesBloqueadosRow[]> {
  return apiFetch<RptClientesBloqueadosRow[]>(
    `/credito/rpt-clientes-bloqueados?${queryGestorInforme(params)}`,
  )
}

export function downloadClientesBloqueadosCsv(
  params: GestorInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-bloqueados-csv?${queryGestorInforme(params)}`,
    'clientes-bloqueados.csv',
  )
}

export function downloadClientesBloqueadosPdf(
  params: GestorInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-bloqueados-pdf?${queryGestorInforme(params)}`,
    'clientes-bloqueados.pdf',
  )
}

export function fetchClientesTopeCredito(
  params: GestorInformeParams,
): Promise<RptClientesTopeCreditoRow[]> {
  return apiFetch<RptClientesTopeCreditoRow[]>(
    `/credito/rpt-clientes-tope-credito?${queryGestorInforme(params)}`,
  )
}

export function downloadClientesTopeCreditoCsv(
  params: GestorInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-tope-credito-csv?${queryGestorInforme(params)}`,
    'clientes-tope-credito.csv',
  )
}

export function downloadClientesTopeCreditoPdf(
  params: GestorInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-tope-credito-pdf?${queryGestorInforme(params)}`,
    'clientes-tope-credito.pdf',
  )
}

function queryCreditoObservado(params: GestorInformeParams): string {
  const q = new URLSearchParams()
  if (params.oficinaId != null && params.oficinaId > 0) {
    q.set('oficinaId', String(params.oficinaId))
  }
  if (params.usuarioId != null && params.usuarioId > 0) {
    q.set('usuarioId', String(params.usuarioId))
  }
  return q.toString()
}

export function fetchCreditoObservado(
  params: GestorInformeParams,
): Promise<RptCreditoObservadoRow[]> {
  return apiFetch<RptCreditoObservadoRow[]>(
    `/credito/rpt-credito-observado?${queryCreditoObservado(params)}`,
  )
}

export function downloadCreditoObservadoCsv(
  params: GestorInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-observado-csv?${queryCreditoObservado(params)}`,
    'credito-observado.csv',
  )
}

export function downloadCreditoObservadoPdf(
  params: GestorInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-observado-pdf?${queryCreditoObservado(params)}`,
    'credito-observado.pdf',
  )
}

function queryCreditoMorosidad(params: CreditoMorosidadParams): string {
  return new URLSearchParams({
    oficinaId: String(params.oficinaId),
    hastaFecha: params.hastaFecha,
    diasAtrazoIni: String(params.diasAtrazoIni),
    diasAtrazoFin: String(params.diasAtrazoFin),
  }).toString()
}

export function fetchCreditoMorosidad(
  params: CreditoMorosidadParams,
): Promise<RptCreditoMorosidadRow[]> {
  return apiFetch<RptCreditoMorosidadRow[]>(
    `/credito/rpt-credito-morosidad?${queryCreditoMorosidad(params)}`,
  )
}

export function downloadCreditoMorosidadCsv(
  params: CreditoMorosidadParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-morosidad-csv?${queryCreditoMorosidad(params)}`,
    'credito-morosidad.csv',
  )
}

export function downloadCreditoMorosidadPdf(
  params: CreditoMorosidadParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-morosidad-pdf?${queryCreditoMorosidad(params)}`,
    'credito-morosidad.pdf',
  )
}

function queryRangoGestor(
  params: ClientesNuevosMesParams | CreditoCondonadoParams,
): string {
  const q = new URLSearchParams({
    oficinaId: String(params.oficinaId),
    fechaIni: params.fechaIni,
    fechaFin: params.fechaFin,
  })
  if (params.usuarioId != null && params.usuarioId > 0) {
    q.set('usuarioId', String(params.usuarioId))
  }
  return q.toString()
}

export function fetchClientesNuevosMes(
  params: ClientesNuevosMesParams,
): Promise<RptCreditoObservadoRow[]> {
  return apiFetch<RptCreditoObservadoRow[]>(
    `/credito/rpt-clientes-nuevos-mes?${queryRangoGestor(params)}`,
  )
}

export function downloadClientesNuevosMesCsv(
  params: ClientesNuevosMesParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-nuevos-mes-csv?${queryRangoGestor(params)}`,
    'clientes-nuevos-mes.csv',
  )
}

export function downloadClientesNuevosMesPdf(
  params: ClientesNuevosMesParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-nuevos-mes-pdf?${queryRangoGestor(params)}`,
    'clientes-nuevos-mes.pdf',
  )
}

/** Paridad caja gestor legacy: mes actual en servidor si no hay fechas. */
export function downloadClientesNuevosMesPdfGestor(params: {
  oficinaId: number
  usuarioId?: number
}): Promise<void> {
  return apiDownload(
    `/credito/rpt-clientes-nuevos-mes-pdf?${queryClientesNuevosMesOpcional(params)}`,
    'clientes-nuevos-mes.pdf',
  )
}

export function fetchCreditoCondonado(
  params: CreditoCondonadoParams,
): Promise<RptCreditoCondonadoRow[]> {
  return apiFetch<RptCreditoCondonadoRow[]>(
    `/credito/rpt-credito-condonado?${queryRangoGestor(params)}`,
  )
}

export function downloadCreditoCondonadoCsv(
  params: CreditoCondonadoParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-condonado-csv?${queryRangoGestor(params)}`,
    'credito-condonado.csv',
  )
}

export function downloadCreditoCondonadoPdf(
  params: CreditoCondonadoParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-condonado-pdf?${queryRangoGestor(params)}`,
    'credito-condonado.pdf',
  )
}

export function fetchCajasAsignadas(
  oficinaId: number,
): Promise<RptCajasAsignadasRow[]> {
  return apiFetch<RptCajasAsignadasRow[]>(
    `/credito/rpt-cajas-asignadas?oficinaId=${oficinaId}`,
  )
}

export function downloadCajasAsignadasCsv(oficinaId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-cajas-asignadas-csv?oficinaId=${oficinaId}`,
    'cajas-asignadas.csv',
  )
}

export function downloadCajasAsignadasPdf(oficinaId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-cajas-asignadas-pdf?oficinaId=${oficinaId}`,
    'cajas-asignadas.pdf',
  )
}

function queryCajaDiarioInforme(p: CajaDiarioInformeParams): string {
  const q = new URLSearchParams({
    oficinaId: String(p.oficinaId),
    fechaIni: p.fechaIni,
    fechaFin: p.fechaFin,
  })
  if (p.usuarioId != null && p.usuarioId > 0) {
    q.set('usuarioId', String(p.usuarioId))
  }
  return q.toString()
}

export function fetchCajaDiarioInforme(
  params: CajaDiarioInformeParams,
): Promise<RptCajaDiarioRow[]> {
  return apiFetch<RptCajaDiarioRow[]>(
    `/credito/rpt-caja-diario?${queryCajaDiarioInforme(params)}`,
  )
}

export function downloadCajaDiarioInformeCsv(
  params: CajaDiarioInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-caja-diario-csv?${queryCajaDiarioInforme(params)}`,
    'caja-diario.csv',
  )
}

export function downloadCajaDiarioInformePdf(
  params: CajaDiarioInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-caja-diario-pdf?${queryCajaDiarioInforme(params)}`,
    'caja-diario.pdf',
  )
}

function queryCreditoRentabilidad(p: CreditoRentabilidadParams): string {
  return new URLSearchParams({
    oficinaId: String(p.oficinaId),
    fechaIni: p.fechaIni,
    fechaFin: p.fechaFin,
    estadoCredito: p.estadoCredito,
  }).toString()
}

export function fetchCreditoRentabilidad(
  params: CreditoRentabilidadParams,
): Promise<RptCreditoRentabilidadRow[]> {
  return apiFetch<RptCreditoRentabilidadRow[]>(
    `/credito/rpt-credito-rentabilidad?${queryCreditoRentabilidad(params)}`,
  )
}

export function downloadCreditoRentabilidadCsv(
  params: CreditoRentabilidadParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-rentabilidad-csv?${queryCreditoRentabilidad(params)}`,
    'credito-rentabilidad.csv',
  )
}

export function downloadCreditoRentabilidadPdf(
  params: CreditoRentabilidadParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-rentabilidad-pdf?${queryCreditoRentabilidad(params)}`,
    'credito-rentabilidad.pdf',
  )
}

function queryInformeRangoGestor(p: InformeRangoGestorParams): string {
  const q = new URLSearchParams({
    oficinaId: String(p.oficinaId),
    fechaIni: p.fechaIni,
    fechaFin: p.fechaFin,
  })
  if (p.usuarioId != null && p.usuarioId > 0) {
    q.set('usuarioId', String(p.usuarioId))
  }
  return q.toString()
}

function queryCreditoAprobacion(p: CreditoAprobacionInformeParams): string {
  const q = new URLSearchParams({
    oficinaId: String(p.oficinaId),
    fechaAprobacion: p.fechaAprobacion,
  })
  if (p.usuarioId != null && p.usuarioId > 0) {
    q.set('usuarioId', String(p.usuarioId))
  }
  return q.toString()
}

function queryMovimientoCajaAnulado(p: MovimientoCajaAnuladoParams): string {
  return new URLSearchParams({
    oficinaId: String(p.oficinaId),
    fechaIni: p.fechaIni,
    fechaFin: p.fechaFin,
  }).toString()
}

function queryComprobantesCajaChica(p: ComprobantesCajaChicaParams): string {
  return new URLSearchParams({
    fechaIni: p.fechaIni,
    fechaFin: p.fechaFin,
  }).toString()
}

export function fetchCreditoAprobacion(
  params: CreditoAprobacionInformeParams,
): Promise<RptCreditoAprobacionRow[]> {
  return apiFetch<RptCreditoAprobacionRow[]>(
    `/credito/rpt-credito-aprobacion?${queryCreditoAprobacion(params)}`,
  )
}

export function downloadCreditoAprobacionCsv(
  params: CreditoAprobacionInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-aprobacion-csv?${queryCreditoAprobacion(params)}`,
    'credito-aprobacion.csv',
  )
}

export function downloadCreditoAprobacionPdf(
  params: CreditoAprobacionInformeParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-aprobacion-pdf?${queryCreditoAprobacion(params)}`,
    'credito-aprobacion.pdf',
  )
}

export function fetchCreditosActivos(
  params: InformeRangoGestorParams,
): Promise<RptCreditosActivosRow[]> {
  return apiFetch<RptCreditosActivosRow[]>(
    `/credito/rpt-creditos-activos?${queryInformeRangoGestor(params)}`,
  )
}

export function downloadCreditosActivosCsv(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-creditos-activos-csv?${queryInformeRangoGestor(params)}`,
    'creditos-activos.csv',
  )
}

export function downloadCreditosActivosPdf(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-creditos-activos-pdf?${queryInformeRangoGestor(params)}`,
    'creditos-activos.pdf',
  )
}

export function fetchCreditosCierres(
  params: InformeRangoGestorParams,
): Promise<RptCreditosCierresRow[]> {
  return apiFetch<RptCreditosCierresRow[]>(
    `/credito/rpt-creditos-cierres?${queryInformeRangoGestor(params)}`,
  )
}

export function downloadCreditosCierresCsv(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-creditos-cierres-csv?${queryInformeRangoGestor(params)}`,
    'creditos-cierres.csv',
  )
}

export function downloadCreditosCierresPdf(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-creditos-cierres-pdf?${queryInformeRangoGestor(params)}`,
    'creditos-cierres.pdf',
  )
}

export function fetchCreditosMorososPagados(
  params: InformeRangoGestorParams,
): Promise<RptCreditosMorososPagadosRow[]> {
  return apiFetch<RptCreditosMorososPagadosRow[]>(
    `/credito/rpt-creditos-morosos-pagados?${queryInformeRangoGestor(params)}`,
  )
}

export function downloadCreditosMorososPagadosCsv(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-creditos-morosos-pagados-csv?${queryInformeRangoGestor(params)}`,
    'creditos-morosos-pagados.csv',
  )
}

export function downloadCreditosMorososPagadosPdf(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-creditos-morosos-pagados-pdf?${queryInformeRangoGestor(params)}`,
    'creditos-morosos-pagados.pdf',
  )
}

export function fetchComprobantesCajaChica(
  params: ComprobantesCajaChicaParams,
): Promise<RptComprobantesCajaChicaRow[]> {
  return apiFetch<RptComprobantesCajaChicaRow[]>(
    `/credito/rpt-comprobantes-caja-chica?${queryComprobantesCajaChica(params)}`,
  )
}

export function downloadComprobantesCajaChicaCsv(
  params: ComprobantesCajaChicaParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-comprobantes-caja-chica-csv?${queryComprobantesCajaChica(params)}`,
    'comprobantes-caja-chica.csv',
  )
}

export function downloadComprobantesCajaChicaPdf(
  params: ComprobantesCajaChicaParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-comprobantes-caja-chica-pdf?${queryComprobantesCajaChica(params)}`,
    'comprobantes-caja-chica.pdf',
  )
}

function queryCreditoId(creditoId: number): string {
  return new URLSearchParams({ creditoId: String(creditoId) }).toString()
}

function queryCreditoTarea(params: CreditoTareaReportParams): string {
  const q = new URLSearchParams()
  if (params.estado?.trim()) {
    q.set('estado', params.estado.trim())
  }
  return q.toString()
}

export function fetchRptPlanPagos(creditoId: number): Promise<RptPlanPagosRow[]> {
  return apiFetch<RptPlanPagosRow[]>(
    `/credito/rpt-plan-pagos?${queryCreditoId(creditoId)}`,
  )
}

export function downloadRptPlanPagosCsv(creditoId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-plan-pagos-csv?${queryCreditoId(creditoId)}`,
    `plan-pagos-${creditoId}.csv`,
  )
}

export function downloadRptPlanPagosPdf(creditoId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-plan-pagos-pdf?${queryCreditoId(creditoId)}`,
    `plan-pagos-${creditoId}.pdf`,
  )
}

export function openRptPlanPagosPdfInTab(creditoId: number): Promise<void> {
  return apiOpenInTab(`/credito/rpt-plan-pagos-pdf?${queryCreditoId(creditoId)}`)
}

export function fetchRptEstadoCredito(
  creditoId: number,
): Promise<RptEstadoCreditoInforme> {
  return apiFetch<RptEstadoCreditoInforme>(
    `/credito/rpt-estado-credito?${queryCreditoId(creditoId)}`,
  )
}

export function downloadRptEstadoCreditoCsv(creditoId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-estado-credito-csv?${queryCreditoId(creditoId)}`,
    `estado-credito-${creditoId}.csv`,
  )
}

export function downloadRptEstadoCreditoPdf(creditoId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-estado-credito-pdf?${queryCreditoId(creditoId)}`,
    `estado-credito-${creditoId}.pdf`,
  )
}

export function openRptEstadoCreditoPdfInTab(creditoId: number): Promise<void> {
  return apiOpenInTab(`/credito/rpt-estado-credito-pdf?${queryCreditoId(creditoId)}`)
}

export function fetchRptCreditoTarea(
  params: CreditoTareaReportParams = {},
): Promise<RptCreditoTareaRow[]> {
  const q = queryCreditoTarea(params)
  return apiFetch<RptCreditoTareaRow[]>(
    `/credito/rpt-credito-tarea${q ? `?${q}` : ''}`,
  )
}

export function downloadRptCreditoTareaCsv(
  params: CreditoTareaReportParams = {},
): Promise<void> {
  const q = queryCreditoTarea(params)
  return apiDownload(
    `/credito/rpt-credito-tarea-csv${q ? `?${q}` : ''}`,
    'credito-tareas.csv',
  )
}

export function downloadRptCreditoTareaPdf(
  params: CreditoTareaReportParams = {},
): Promise<void> {
  const q = queryCreditoTarea(params)
  return apiDownload(
    `/credito/rpt-credito-tarea-pdf${q ? `?${q}` : ''}`,
    'credito-tareas.pdf',
  )
}

function queryPersonaId(personaId: number): string {
  return new URLSearchParams({ personaId: String(personaId) }).toString()
}

export function fetchRptCliente(personaId: number): Promise<RptClienteInforme> {
  return apiFetch<RptClienteInforme>(
    `/credito/rpt-cliente?${queryPersonaId(personaId)}`,
  )
}

export function downloadRptClienteCsv(personaId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-cliente-csv?${queryPersonaId(personaId)}`,
    `cliente-${personaId}.csv`,
  )
}

export function downloadRptClientePdf(personaId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-cliente-pdf?${queryPersonaId(personaId)}`,
    `cliente-${personaId}.pdf`,
  )
}

export function openRptClientePdfInTab(personaId: number): Promise<void> {
  return apiOpenInTab(`/credito/rpt-cliente-pdf?${queryPersonaId(personaId)}`)
}

export interface RptSimuladorPlanPagosParams {
  productoId: number
  monto: number
  nroCuotas: number
  interesMensual: number
  fechaPrimerPago: string
  formaPago: string
  gastosAdm?: number
  /** CAP | CUO | ADE (legacy pGA). ADE se trata como CAP en API. */
  ga?: string
  cliente?: string
  tipoDocumento?: string
  nroDocumento?: string
  direccionCliente?: string
  direccionNegocio?: string
  prendaDescripcion?: string
  asesor?: string
  telefonoCliente?: string
}

function querySimuladorPlanPagos(p: RptSimuladorPlanPagosParams): string {
  const q = new URLSearchParams({
    productoId: String(p.productoId),
    monto: String(p.monto),
    nroCuotas: String(p.nroCuotas),
    interesMensual: String(p.interesMensual),
    fechaPrimerPago: p.fechaPrimerPago.slice(0, 10),
    formaPago: p.formaPago,
    gastosAdm: String(p.gastosAdm ?? 0),
    ga: p.ga ?? 'ADE',
  })
  const optionalParams: Array<[string, string | undefined]> = [
    ['cliente', p.cliente],
    ['tipoDocumento', p.tipoDocumento],
    ['nroDocumento', p.nroDocumento],
    ['direccionCliente', p.direccionCliente],
    ['direccionNegocio', p.direccionNegocio],
    ['prendaDescripcion', p.prendaDescripcion],
    ['asesor', p.asesor],
    ['telefonoCliente', p.telefonoCliente],
  ]
  optionalParams.forEach(([key, value]) => {
    if (value?.trim()) {
      q.set(key, value.trim())
    }
  })
  return q.toString()
}

export function downloadRptSimuladorPlanPagosCsv(
  params: RptSimuladorPlanPagosParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-simulador-plan-pagos-csv?${querySimuladorPlanPagos(params)}`,
    'simulador-plan-pagos.csv',
  )
}

export function downloadRptSimuladorPlanPagosPdf(
  params: RptSimuladorPlanPagosParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-simulador-plan-pagos-pdf?${querySimuladorPlanPagos(params)}`,
    'simulador-plan-pagos.pdf',
  )
}

export function openRptSimuladorPlanPagosPdfInTab(
  params: RptSimuladorPlanPagosParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-simulador-plan-pagos-pdf?${querySimuladorPlanPagos(params)}`,
  )
}

export function fetchMovimientoCajaAnulado(
  params: MovimientoCajaAnuladoParams,
): Promise<RptMovimientoCajaAnuladoRow[]> {
  return apiFetch<RptMovimientoCajaAnuladoRow[]>(
    `/credito/rpt-movimiento-caja-anulado?${queryMovimientoCajaAnulado(params)}`,
  )
}

export function downloadMovimientoCajaAnuladoCsv(
  params: MovimientoCajaAnuladoParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-movimiento-caja-anulado-csv?${queryMovimientoCajaAnulado(params)}`,
    'movimientos-caja-anulados.csv',
  )
}

export function downloadMovimientoCajaAnuladoPdf(
  params: MovimientoCajaAnuladoParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-movimiento-caja-anulado-pdf?${queryMovimientoCajaAnulado(params)}`,
    'movimientos-caja-anulados.pdf',
  )
}

function queryReporteCredito(p: ReporteCreditoParams): string {
  const q = new URLSearchParams({
    oficinaId: String(p.oficinaId),
    fechaIni: p.fechaIni,
    fechaFin: p.fechaFin,
    estadoCredito: p.estadoCredito,
  })
  if (p.gestorId != null && p.gestorId > 0) {
    q.set('gestorId', String(p.gestorId))
  }
  return q.toString()
}

export function fetchReporteCredito(
  params: ReporteCreditoParams,
): Promise<RptCreditoRow[]> {
  return apiFetch<RptCreditoRow[]>(`/credito/rpt-credito?${queryReporteCredito(params)}`)
}

export function downloadReporteCreditoCsv(
  params: ReporteCreditoParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-csv?${queryReporteCredito(params)}`,
    'reporte-creditos.csv',
  )
}

export function downloadReporteCreditoPdf(
  params: ReporteCreditoParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-credito-pdf?${queryReporteCredito(params)}`,
    'reporte-creditos.pdf',
  )
}

function querySaldoCarteraCajaDiario(p: SaldoCarteraCajaDiarioParams): string {
  const q = new URLSearchParams({
    oficinaId: String(p.oficinaId),
    anioIni: String(p.anioIni),
    mesIni: String(p.mesIni),
    anioFin: String(p.anioFin),
    mesFin: String(p.mesFin),
  })
  if (p.usuarioId != null && p.usuarioId > 0) {
    q.set('usuarioId', String(p.usuarioId))
  }
  return q.toString()
}

export function fetchSaldoCarteraCajaDiario(
  params: SaldoCarteraCajaDiarioParams,
): Promise<RptSaldoCarteraCajaDiarioRow[]> {
  return apiFetch<RptSaldoCarteraCajaDiarioRow[]>(
    `/credito/rpt-saldo-cartera-caja-diario?${querySaldoCarteraCajaDiario(params)}`,
  )
}

export function downloadSaldoCarteraCajaDiarioCsv(
  params: SaldoCarteraCajaDiarioParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-saldo-cartera-caja-diario-csv?${querySaldoCarteraCajaDiario(params)}`,
    'saldo-cartera-caja-diario.csv',
  )
}

export function downloadSaldoCarteraCajaDiarioPdf(
  params: SaldoCarteraCajaDiarioParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-saldo-cartera-caja-diario-pdf?${querySaldoCarteraCajaDiario(params)}`,
    'saldo-cartera-caja-diario.pdf',
  )
}

export function fetchCobroDiarioDetalle(
  params: CobroDiarioParams,
): Promise<RptCobroDiarioDetalleRow[]> {
  return apiFetch<RptCobroDiarioDetalleRow[]>(
    `/credito/rpt-cobro-diario-detalle?${queryCobroDiario(params)}`,
  )
}

export function downloadCobroDiarioDetalleCsv(
  params: CobroDiarioParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-cobro-diario-detalle-csv?${queryCobroDiario(params)}`,
    'cobro-diario-detalle.csv',
  )
}

export function downloadCobroDiarioDetallePdf(
  params: CobroDiarioParams,
): Promise<void> {
  return apiDownload(
    `/credito/rpt-cobro-diario-detalle-pdf?${queryCobroDiario(params)}`,
    'cobro-diario-detalle.pdf',
  )
}

export function fetchRptAval(personaId: number): Promise<RptAvalRow[]> {
  return apiFetch<RptAvalRow[]>(`/credito/rpt-aval?personaId=${personaId}`)
}

export function downloadRptAvalCsv(personaId: number): Promise<void> {
  return apiDownload(`/credito/rpt-aval-csv?personaId=${personaId}`, 'aval-persona.csv')
}

export function downloadRptAvalPdf(personaId: number): Promise<void> {
  return apiDownload(`/credito/rpt-aval-pdf?personaId=${personaId}`, 'aval-persona.pdf')
}

export function fetchMontoPendientePlanPago(
  oficinaId: number,
): Promise<{ montoPendiente: number | null }> {
  return apiFetch<{ montoPendiente: number | null }>(
    `/credito/obtener-monto-pendiente-plan-pago?oficinaId=${oficinaId}`,
  )
}

export function cerrarCajasDiarios(body: {
  oficinaId: number
  sobrante: number
}): Promise<CajaDiarioOperacionResponse> {
  return apiFetch<CajaDiarioOperacionResponse>('/credito/cerrar-cajas-diarios', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface CentralRiesgoGenerarRow {
  anio: number | null
  mes: number | null
  creditoId: number
  periodo: string | null
  entidad: string | null
  tipoDoc: number
  numDoc: string | null
  razonSocial: string | null
  apePat: string | null
  apeMat: string | null
  nombres: string | null
  tipoPersona: number
  modalidadCredito: number
  deudaMenor30: string | null
  deudaMayor30: string | null
  calificacion: number
  diasAtrazo: number
  direccion: string | null
  celular: string | null
}

export interface CentralRiesgoParams {
  oficinaId: number
  anio: number
  mes: number
}

export interface CreditoVencidoMetricas {
  creditoVencido: number | null
  vencidoMenor60: number | null
  vencidoMayor60: number | null
  vencidoIrrecuperable: number | null
}

function queryCentralRiesgo(p: CentralRiesgoParams): string {
  return new URLSearchParams({
    oficinaId: String(p.oficinaId),
    anio: String(p.anio),
    mes: String(p.mes),
  }).toString()
}

export function fetchCentralRiesgoGenerar(
  params: CentralRiesgoParams,
): Promise<CentralRiesgoGenerarRow[]> {
  return apiFetch<CentralRiesgoGenerarRow[]>(
    `/credito/central-riesgo-generar?${queryCentralRiesgo(params)}`,
  )
}

export function downloadCentralRiesgoGenerarCsv(
  params: CentralRiesgoParams,
): Promise<void> {
  return apiDownload(
    `/credito/central-riesgo-generar-csv?${queryCentralRiesgo(params)}`,
    'central-riesgo-generar.csv',
  )
}

export function downloadCentralRiesgoGenerarPdf(
  params: CentralRiesgoParams,
): Promise<void> {
  return apiDownload(
    `/credito/central-riesgo-generar-pdf?${queryCentralRiesgo(params)}`,
    'central-riesgo-generar.pdf',
  )
}

export function downloadCentralRiesgoGenerarTxt(
  params: CentralRiesgoParams,
): Promise<void> {
  return apiDownload(
    `/credito/central-riesgo-generar-txt?${queryCentralRiesgo(params)}`,
    'DM007898.txt',
  )
}

export function fetchMetricasVencimientoCredito(
  creditoId: number,
): Promise<CreditoVencidoMetricas> {
  return apiFetch<CreditoVencidoMetricas>(
    `/credito/metricas-vencimiento-credito?creditoId=${creditoId}`,
  )
}

export function validarAnularCredito(
  oficinaId: number,
  creditoId: number,
): Promise<ValidarAnularCreditoResponse> {
  return apiFetch<ValidarAnularCreditoResponse>(
    `/credito/validar-anular-credito?oficinaId=${oficinaId}&creditoId=${creditoId}`,
  )
}

export function anularCredito(body: {
  oficinaId: number
  creditoId: number
  observacion: string
  claveAutorizacion: string
}): Promise<CreditoCicloOperacionResponse> {
  return apiFetch<CreditoCicloOperacionResponse>('/credito/anular-credito', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function prorrogarCredito(body: {
  oficinaId: number
  creditoId: number
  dias: number
}): Promise<CreditoCicloOperacionResponse> {
  return apiFetch<CreditoCicloOperacionResponse>('/credito/prorrogar-credito', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function reprogramarCredito(body: {
  oficinaId: number
  creditoId: number
}): Promise<CreditoCicloOperacionResponse> {
  return apiFetch<CreditoCicloOperacionResponse>('/credito/reprogramar-credito', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function crearSolicitudCredito(body: {
  oficinaId: number
  personaId: number
}): Promise<CrearSolicitudCreditoResponse> {
  return apiFetch<CrearSolicitudCreditoResponse>('/credito/crear-solicitud-credito', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function crearCredito(body: {
  oficinaId: number
  solicitudCreditoId: number
  productoId: number
  tipoCuota: string
  montoInicial: number
  montoGastosAdm: number
  indGastosAdm: string
  montoCredito: number
  modalidad: string
  numeroCuotas: number
  interesMensual: number
  fechaPrimerPago: string
  observacion?: string | null
  indCentralRiesgo: boolean
  prenda?: {
    descripcion: string
    montoTasacion: number
    fechaRemate: string
    observacion?: string | null
  } | null
}): Promise<CrearCreditoResponse> {
  return apiFetch<CrearCreditoResponse>('/credito/crear-credito', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

/** Exportaciones en pestaña nueva (JWT). Sustituye window.open a MVC sin sesión forms. */
export function openCreditoMorosidadPdfInTab(
  params: CreditoMorosidadParams,
): Promise<void> {
  return apiOpenInTab(`/credito/rpt-credito-morosidad-pdf?${queryCreditoMorosidad(params)}`)
}

export function openClientesNuevosMesPdfGestorInTab(params: {
  oficinaId: number
  usuarioId?: number
}): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-clientes-nuevos-mes-pdf?${queryClientesNuevosMesOpcional(params)}`,
  )
}

export function openClientesNuevosMesPdfInTab(
  params: ClientesNuevosMesParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-clientes-nuevos-mes-pdf?${queryRangoGestor(params)}`,
  )
}

export function openClientesNuevosMesCsvInTab(
  params: ClientesNuevosMesParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-clientes-nuevos-mes-csv?${queryRangoGestor(params)}`,
  )
}

export function openCreditoAprobacionPdfInTab(
  params: CreditoAprobacionInformeParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-credito-aprobacion-pdf?${queryCreditoAprobacion(params)}`,
  )
}

export function openCreditoAprobacionCsvInTab(
  params: CreditoAprobacionInformeParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-credito-aprobacion-csv?${queryCreditoAprobacion(params)}`,
  )
}

export function openCobroDiarioPdfInTab(params: CobroDiarioQueryParams): Promise<void> {
  return apiOpenInTab(`/credito/rpt-cobro-diario-pdf?${queryCobroDiario(params)}`)
}

export function openCobroDiarioCsvInTab(params: CobroDiarioQueryParams): Promise<void> {
  return apiOpenInTab(`/credito/rpt-cobro-diario-csv?${queryCobroDiario(params)}`)
}

export function openCreditoObservadoPdfInTab(params: GestorInformeParams): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-credito-observado-pdf?${queryGestorInforme(params)}`,
  )
}

export function openCreditoObservadoCsvInTab(params: GestorInformeParams): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-credito-observado-csv?${queryGestorInforme(params)}`,
  )
}

export function openClientesInactivosPdfGestorInTab(params: GestorInformeParams): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-clientes-inactivos-pdf?${queryGestorInforme(params)}`,
  )
}

export function openClientesBloqueadosPdfInTab(params: GestorInformeParams): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-clientes-bloqueados-pdf?${queryGestorInforme(params)}`,
  )
}

export function openClientesTopeCreditoPdfInTab(params: GestorInformeParams): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-clientes-tope-credito-pdf?${queryGestorInforme(params)}`,
  )
}

export function openReporteCreditoPdfInTab(params: ReporteCreditoParams): Promise<void> {
  return apiOpenInTab(`/credito/rpt-credito-pdf?${queryReporteCredito(params)}`)
}

export function openCreditoRentabilidadPdfInTab(
  params: CreditoRentabilidadParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-credito-rentabilidad-pdf?${queryCreditoRentabilidad(params)}`,
  )
}

export function openCreditoRentabilidadCsvInTab(
  params: CreditoRentabilidadParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-credito-rentabilidad-csv?${queryCreditoRentabilidad(params)}`,
  )
}

export function openCreditoCondonadoPdfInTab(
  params: CreditoCondonadoParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-credito-condonado-pdf?${queryRangoGestor(params)}`,
  )
}

export function openCreditoCondonadoCsvInTab(
  params: CreditoCondonadoParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-credito-condonado-csv?${queryRangoGestor(params)}`,
  )
}

export function openCreditosActivosCsvInTab(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-creditos-activos-csv?${queryInformeRangoGestor(params)}`,
  )
}

export function openCreditosCierresPdfInTab(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-creditos-cierres-pdf?${queryInformeRangoGestor(params)}`,
  )
}

export function openCreditosCierresCsvInTab(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-creditos-cierres-csv?${queryInformeRangoGestor(params)}`,
  )
}

export function openCreditosMorososPagadosPdfInTab(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-creditos-morosos-pagados-pdf?${queryInformeRangoGestor(params)}`,
  )
}

export function openCreditosMorososPagadosCsvInTab(
  params: InformeRangoGestorParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-creditos-morosos-pagados-csv?${queryInformeRangoGestor(params)}`,
  )
}

export function openCajaDiarioInformePdfInTab(
  params: CajaDiarioInformeParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-caja-diario-pdf?${queryCajaDiarioInforme(params)}`,
  )
}

export function openClientesInactivosPdfInTab(
  params: ClientesInactivosParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-clientes-inactivos-pdf?${queryClientesInactivos(params)}`,
  )
}

export function openComprobantesCajaChicaPdfInTab(
  params: ComprobantesCajaChicaParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-comprobantes-caja-chica-pdf?${queryComprobantesCajaChica(params)}`,
  )
}

export function openComprobantesCajaChicaCsvInTab(
  params: ComprobantesCajaChicaParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-comprobantes-caja-chica-csv?${queryComprobantesCajaChica(params)}`,
  )
}

export function openMovimientoCajaAnuladoPdfInTab(
  params: MovimientoCajaAnuladoParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-movimiento-caja-anulado-pdf?${queryMovimientoCajaAnulado(params)}`,
  )
}

export function openSaldoCarteraCajaDiarioPdfInTab(
  params: SaldoCarteraCajaDiarioParams,
): Promise<void> {
  return apiOpenInTab(
    `/credito/rpt-saldo-cartera-caja-diario-pdf?${querySaldoCarteraCajaDiario(params)}`,
  )
}

export function openCentralRiesgoTxtInTab(params: CentralRiesgoParams): Promise<void> {
  return apiOpenInTab(`/credito/central-riesgo-generar-txt?${queryCentralRiesgo(params)}`)
}

/** Exportaciones en pestaña nueva (JWT). Sustituye window.open a MVC sin sesión forms. */
