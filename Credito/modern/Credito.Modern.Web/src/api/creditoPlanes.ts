import { apiDownload, apiFetch, apiOpenInTab } from './client'
import type {
  CalcularTemResult,
  EstadoPlanPagoCuota,
  RptMovimientoCreditoRow,
  CajaDiarioOperacionResponse,
  CrearSolicitudCreditoResponse,
  CreditoCicloOperacionResponse,
  ReprogramarCreditoResponse,
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

export interface RutaCobroParada {
  orden: number
  creditoId: number
  cliente: string
  montoCobrar: number
  direccion: string | null
  latitud: number | null
  longitud: number | null
  tieneGps: boolean
}

export interface GenerarRutaCobrosResponse {
  exito: boolean
  urlCortita: string | null
  mensaje: string | null
  urlNavegacionGoogle: string | null
  latitudOrigen: number | null
  longitudOrigen: number | null
  paradas: RutaCobroParada[] | null
}

export function generarRutaCobros(
  creditoIds: number[],
  opts?: { usuarioId?: number; oficinaId?: number },
): Promise<GenerarRutaCobrosResponse> {
  return apiFetch<GenerarRutaCobrosResponse>('/credito/generar-ruta-cobros', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      creditoIds,
      usuarioId: opts?.usuarioId,
      oficinaId: opts?.oficinaId,
    }),
  })
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
  const fecha = (p.fechaPrimerPago ?? '').toString().trim().slice(0, 10)
  if (!/^\d{4}-\d{2}-\d{2}$/.test(fecha)) {
    throw new Error('Falta la fecha de primer pago para generar el reporte')
  }
  if (!(p.productoId > 0)) {
    throw new Error('Seleccione un producto antes de generar el reporte')
  }
  if (!(p.monto > 0)) {
    throw new Error('El monto debe ser mayor a cero')
  }
  if (!(p.nroCuotas >= 1)) {
    throw new Error('El número de cuotas es inválido')
  }
  const forma = (p.formaPago ?? '').trim().toUpperCase()
  if (!forma) {
    throw new Error('Falta la modalidad de pago')
  }
  const q = new URLSearchParams({
    productoId: String(p.productoId),
    monto: String(p.monto),
    nroCuotas: String(p.nroCuotas),
    interesMensual: String(p.interesMensual),
    fechaPrimerPago: fecha,
    formaPago: forma,
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

export interface CreditoVencidoMetricas {
  creditoVencido: number | null
  vencidoMenor60: number | null
  vencidoMayor60: number | null
  vencidoIrrecuperable: number | null
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
}): Promise<ReprogramarCreditoResponse> {
  return apiFetch<ReprogramarCreditoResponse>('/credito/reprogramar-credito', {
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

/** Barrel: informes y central de riesgo viven en módulos propios. */
export * from './centralRiesgoApi'
export * from './creditoInformesExport'
