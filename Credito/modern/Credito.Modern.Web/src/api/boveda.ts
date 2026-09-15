import { apiDownload, apiFetch } from './client'
import type { CajaDiarioOperacionResponse, ClienteBuscarItem } from '../types/api'

export interface BovedaAbiertaDto {
  bovedaId: number
  oficinaId: number
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
  fechaIniOperacion: string
  fechaFinOperacion: string | null
  indCierre: boolean
  indTemporal: boolean
}

export interface RptMovimientoBovedaRow {
  movimientoBovedaId: number
  fechaReg: string
  codOperacion: string | null
  glosa: string | null
  entrada: number | null
  salida: number | null
  tipoPago: string | null
  agente: string | null
}

export interface ResumenCuentaBovedaResponse {
  texto: string | null
}

export interface ExisteBovedaTemporalResponse {
  existe: boolean
}

export interface BovedaEstadoDinero {
  saldoBoveda: number
  montoCajaChica: number
  montoCajas: number
  montoPlanPagoPendiente: number
  creditoVencido: number
  vencidoMenor60: number
  vencidoMayor60: number
  vencidoIrrecuperable: number
  totalFondo: number
}

export interface BovedaCuadreMedio {
  tipoPagoId: number
  tipoPago: string
  grupo: string
  monto: number
  esEfectivo: boolean
  esDigitalOBanco: boolean
  requiereVerificacion: boolean
  pagosNoVerificados: number
}

export interface BovedaCuadreResponsable {
  cajaDiarioId: number
  caja: string
  responsable: string | null
  fechaIniOperacion: string
  fechaFinOperacion: string | null
  indCierre: boolean
  transBoveda: boolean
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
  efectivo: number
  digitalBancos: number
  totalSistema: number
  medios: BovedaCuadreMedio[]
}

export interface BovedaCuadreValidacion {
  codigo: string
  concepto: string
  sistema: number
  contraste: number | null
  diferencia: number | null
  severidad: 'ok' | 'alerta' | 'info' | string
  mensaje: string
}

export interface BovedaCuadreDenominacion {
  codigo: string
  etiqueta: string
  valor: number
  grupo: string
}

export interface BovedaCuadreTotales {
  efectivoBoveda: number
  efectivoCajas: number
  efectivoSistema: number
  digitalBancosSistema: number
  totalMediosSistema: number
  totalFondo: number
  montoALlevarSugerido: number
  saldoPorLlevarSugerido: number
  diferenciaSistema: number
}

export interface BovedaCuadrePreview {
  boveda: BovedaAbiertaDto
  estadoDinero: BovedaEstadoDinero
  totales: BovedaCuadreTotales
  responsables: BovedaCuadreResponsable[]
  mediosBoveda: BovedaCuadreMedio[]
  validaciones: BovedaCuadreValidacion[]
  denominaciones: BovedaCuadreDenominacion[]
  pendientes: string[]
}

export interface BovedaListadoRow {
  bovedaId: number
  tipo: string
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
  fechaIniOperacion: string
  fechaFinOperacion: string | null
  indCierre: boolean
}

export interface BovedaListadoResult {
  rows: BovedaListadoRow[]
  total: number
  page: number
  pageSize: number
}

export interface BovedaDestinoTransferencia {
  bovedaId: number
  oficinaId: number
  oficina: string
  saldoFinal: number
  fechaIniOperacion: string
}

export function fetchBovedaEstadoDinero(oficinaId: number): Promise<BovedaEstadoDinero> {
  return apiFetch<BovedaEstadoDinero>(
    `/credito/boveda-estado-dinero?oficinaId=${oficinaId}`,
  )
}

export function fetchBovedaCuadrePreview(
  oficinaId: number,
  bovedaId?: number,
): Promise<BovedaCuadrePreview> {
  const q = new URLSearchParams({ oficinaId: String(oficinaId) })
  if (bovedaId != null && bovedaId > 0) {
    q.set('bovedaId', String(bovedaId))
  }
  return apiFetch<BovedaCuadrePreview>(`/credito/boveda-cuadre-preview?${q.toString()}`)
}

export function listarBovedasHistorial(
  oficinaId: number,
  page = 1,
  pageSize = 5,
): Promise<BovedaListadoResult> {
  return apiFetch<BovedaListadoResult>(
    `/credito/boveda-listar?oficinaId=${oficinaId}&page=${page}&pageSize=${pageSize}`,
  )
}

export interface ValidarCierreSaldosResponse {
  puedeCerrar: boolean
  mensaje: string
}

export function fetchBovedaAbierta(oficinaId: number): Promise<BovedaAbiertaDto> {
  return apiFetch<BovedaAbiertaDto>(
    `/credito/boveda-abierta?oficinaId=${oficinaId}`,
  )
}

export function fetchBovedasDestinoTransferencia(
  oficinaId: number,
): Promise<BovedaDestinoTransferencia[]> {
  return apiFetch<BovedaDestinoTransferencia[]>(
    `/credito/bovedas-destino-transferencia?oficinaId=${oficinaId}`,
  )
}

export interface BovedaTransferenciaPendiente {
  bovedaMovTempId: number
  monto: number
  oficinaOrigen: string
  descripcion: string | null
  usuarioReg: string
  fechaReg: string
}

export function fetchBovedaTransferenciasPendientes(
  bovedaId: number,
): Promise<BovedaTransferenciaPendiente[]> {
  return apiFetch<BovedaTransferenciaPendiente[]>(
    `/credito/boveda-transferencias-pendientes?bovedaId=${bovedaId}`,
  )
}

export function fetchExisteBovedaTemporal(
  oficinaId: number,
): Promise<ExisteBovedaTemporalResponse> {
  return apiFetch<ExisteBovedaTemporalResponse>(
    `/credito/existe-boveda-temporal?oficinaId=${oficinaId}`,
  )
}

export function fetchResumenCuentaBoveda(
  bovedaId: number,
): Promise<ResumenCuentaBovedaResponse> {
  return apiFetch<ResumenCuentaBovedaResponse>(
    `/credito/resumen-cuenta-boveda?bovedaId=${bovedaId}`,
  )
}

export function fetchRptMovimientoBoveda(
  bovedaId: number,
): Promise<RptMovimientoBovedaRow[]> {
  return apiFetch<RptMovimientoBovedaRow[]>(
    `/credito/rpt-movimiento-boveda?bovedaId=${bovedaId}`,
  )
}

export function downloadRptMovimientoBovedaCsv(bovedaId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-movimiento-boveda-csv?bovedaId=${bovedaId}`,
    'movimiento-boveda.csv',
  )
}

export function downloadMovimientoBovedaTicketPdf(
  movimientoBovedaId: number,
): Promise<void> {
  return apiDownload(
    `/credito/movimiento-boveda-ticket-pdf?movimientoBovedaId=${movimientoBovedaId}`,
    `ticket-boveda-${movimientoBovedaId}.pdf`,
  )
}

export function downloadRptMovimientoBovedaPdf(bovedaId: number): Promise<void> {
  return apiDownload(
    `/credito/rpt-movimiento-boveda-pdf?bovedaId=${bovedaId}`,
    'movimiento-boveda.pdf',
  )
}

export function fetchValidarCierreCajaChica(
  oficinaId: number,
): Promise<ValidarCierreSaldosResponse> {
  return apiFetch<ValidarCierreSaldosResponse>(
    `/credito/validar-cierre-caja-chica?oficinaId=${oficinaId}`,
  )
}

export function fetchValidarCierreSaldos(
  oficinaId: number,
): Promise<ValidarCierreSaldosResponse> {
  return apiFetch<ValidarCierreSaldosResponse>(
    `/credito/validar-cierre-saldos?oficinaId=${oficinaId}`,
  )
}

export interface BovedaMovOperacionResponse {
  movimientoBovedaId: number
  movimientoCajaId: number | null
}

export interface CajaAbiertaTransferenciaRow {
  cajaId: number
  etiqueta: string
}

export interface AsignarBovedaTemporalResponse {
  bovedaTemporalId: number | null
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function ingresoEgresoBoveda(body: {
  oficinaId: number
  importe: number
  descripcion: string
  tipoOperacionId: number
  tipoPagoId: number
}): Promise<BovedaMovOperacionResponse> {
  return postJson('/credito/ingreso-egreso-boveda', body)
}

export function transferirBovedaCaja(body: {
  oficinaId: number
  cajaId: number
  importe: number
  descripcion: string
}): Promise<BovedaMovOperacionResponse> {
  return postJson('/credito/transferir-boveda-caja', body)
}

export function transferirBovedaCajaChica(body: {
  oficinaId: number
  importe: number
  descripcion: string
}): Promise<BovedaMovOperacionResponse> {
  return postJson('/credito/transferir-boveda-caja-chica', body)
}

export interface TransferirBovedaBancosResponse {
  success: boolean
  mensaje: string
}

export function transferirBovedaBancos(body: {
  oficinaId: number
  tipoPagoOrigenId: number
  tipoPagoDestinoId: number
  importe: number
  glosa: string
}): Promise<TransferirBovedaBancosResponse> {
  return postJson('/credito/transferir-boveda-bancos', body)
}

export function cerrarBoveda(oficinaId: number): Promise<CajaDiarioOperacionResponse> {
  return postJson('/credito/cerrar-boveda', { oficinaId })
}

export function cerrarBovedaTemporal(
  oficinaId: number,
): Promise<CajaDiarioOperacionResponse> {
  return postJson('/credito/cerrar-boveda-temporal', { oficinaId })
}

export function transferirBoveda(body: {
  oficinaId: number
  bovedaInicioId: number
  bovedaDestinoId: number
  glosa: string
  monto: number
}): Promise<CajaDiarioOperacionResponse> {
  return postJson('/credito/transferir-boveda', {
    ...body,
    flagAceptar: 0,
    bovedaMovTempId: 0,
  })
}

/** Aceptación/rechazo de transferencia inter-oficina (bovedaMovTempId > 0). */
export function aceptarTransferenciaBoveda(body: {
  oficinaId: number
  bovedaMovTempId: number
  flagAceptar: number
}): Promise<CajaDiarioOperacionResponse> {
  return postJson('/credito/transferir-boveda', {
    oficinaId: body.oficinaId,
    bovedaInicioId: 0,
    bovedaDestinoId: 0,
    glosa: '',
    monto: 0,
    bovedaMovTempId: body.bovedaMovTempId,
    flagAceptar: body.flagAceptar,
  })
}

export function asignarBovedaTemporal(body: {
  oficinaId: number
  importe: number
  descripcion: string
  usuarioId: number
}): Promise<AsignarBovedaTemporalResponse> {
  return postJson('/credito/asignar-boveda-temporal', body)
}

export function fetchCajasAbiertasTransferenciaBoveda(
  oficinaId: number,
): Promise<CajaAbiertaTransferenciaRow[]> {
  return apiFetch<CajaAbiertaTransferenciaRow[]>(
    `/credito/cajas-abiertas-transferencia-boveda?oficinaId=${oficinaId}`,
  )
}

export function buscarUsuariosBoveda(term: string): Promise<ClienteBuscarItem[]> {
  return apiFetch<ClienteBuscarItem[]>(
    `/usuarios/buscar?term=${encodeURIComponent(term)}`,
  )
}

export function transferirCierreCajaChica(
  oficinaId: number,
): Promise<{ cajasTransferidas: number }> {
  return postJson('/credito/transferir-cierre-caja-chica', { oficinaId })
}

export function actualizarDatosPostCierreBoveda(
  oficinaId: number,
): Promise<void> {
  return postJson('/credito/actualizar-datos-post-cierre-boveda', { oficinaId })
}

/** Aceptación/rechazo de transferencia inter-oficina (bovedaMovTempId > 0). */
