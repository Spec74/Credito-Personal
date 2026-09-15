import { apiDownload, apiFetch } from './client'
import type { ClienteBuscarItem, TipoOperacionListItem } from '../types/api'

export interface CajaChicaSesion {
  id: number
  usuarioId: number
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
  fechaIniOperacion: string
  indCierre: boolean
}

export interface MovimientoCajaChicaRow {
  movimientoCajaChicaId: number
  cajaChicaDiarioId: number
  fechaReg: string
  indEntrada: boolean
  persona: string | null
  operacion: string
  descripcion: string | null
  importePago: number
  estado: boolean
}

export interface RendicionPendienteRow {
  movimientoCajaChicaId: number
  cliente: string
  descripcion: string | null
  fechaReg: string
  importe: number
  importeRendido: number
}

export interface RendicionComprobanteRow {
  id: number
  movimientoCajaChicaId: number
  ruc: string | null
  razonSocial: string | null
  detalleGasto: string | null
  tipoDocumento: string | null
  fecha: string
  serie: string | null
  numero: string | null
  importe: number
}

export interface TipoDocumentoListItem {
  tipoDocumentoId: number
  denominacion: string | null
  indCajaChica: boolean
  estado: boolean
}

function postJson<T>(path: string, body?: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })
}

export function fetchCajaChicaSesion(): Promise<CajaChicaSesion> {
  return apiFetch<CajaChicaSesion>('/credito/caja-chica-sesion')
}

export function fetchMovimientosCajaChica(
  tipo: 'E' | 'S',
): Promise<MovimientoCajaChicaRow[]> {
  return apiFetch<MovimientoCajaChicaRow[]>(
    `/credito/movimientos-caja-chica?tipo=${tipo}`,
  )
}

export function fetchRendicionesPendientesCajaChica(): Promise<
  RendicionPendienteRow[]
> {
  return apiFetch<RendicionPendienteRow[]>(
    '/credito/rendiciones-pendientes-caja-chica',
  )
}

export function fetchRendicionesCajaChica(
  movimientoCajaChicaId: number,
): Promise<RendicionComprobanteRow[]> {
  return apiFetch<RendicionComprobanteRow[]>(
    `/credito/rendiciones-caja-chica?movimientoCajaChicaId=${movimientoCajaChicaId}`,
  )
}

export function fetchTieneRendicionesPendientesCajaChica(): Promise<number> {
  return apiFetch<number>('/credito/tiene-rendiciones-pendientes-caja-chica')
}

export function fetchMontoCajaChica(): Promise<number> {
  return apiFetch<number>('/credito/monto-caja-chica')
}

export function buscarUsuarios(term: string): Promise<ClienteBuscarItem[]> {
  return apiFetch<ClienteBuscarItem[]>(
    `/usuarios/buscar?term=${encodeURIComponent(term)}`,
  )
}

export function fetchTiposDocumentoCajaChica(): Promise<TipoDocumentoListItem[]> {
  return apiFetch<TipoDocumentoListItem[]>('/tipos-documento').then((rows) =>
    rows.filter((r) => r.indCajaChica && r.estado),
  )
}

export function fetchTipoOperacionesCajaChica(): Promise<TipoOperacionListItem[]> {
  return apiFetch<TipoOperacionListItem[]>('/tipo-operaciones').then((rows) =>
    rows.filter((r) => r.indCajaChica),
  )
}

export function entradaSalidaCajaChica(body: {
  oficinaId: number
  personaId: number
  tipoOperacionId: number
  importe: number
  descripcion: string
}): Promise<{ resultCode: number }> {
  return postJson('/credito/entrada-salida-caja-chica-diario', body)
}

export function crearRendicionCajaChica(body: {
  movimientoCajaChicaId: number
  tipoDocumentoId: number
  fecha: string
  serie: string
  numero: string
  ruc: string
  razonSocial: string
  detalleGasto: string
  importe: number
}): Promise<void> {
  return postJson('/credito/rendiciones-caja-chica', body)
}

export function eliminarRendicionCajaChica(rendicionId: number): Promise<void> {
  return apiFetch(`/credito/rendiciones-caja-chica/${rendicionId}`, {
    method: 'DELETE',
  })
}

export function cerrarRendicionCajaChica(
  movimientoCajaChicaId: number,
): Promise<{ devolucion: number }> {
  return postJson(
    `/credito/cerrar-rendicion-caja-chica?movimientoCajaChicaId=${movimientoCajaChicaId}`,
  )
}

export function cerrarCajaChicaDiario(): Promise<{ cajaChicaDiarioId: number }> {
  return postJson('/credito/cerrar-caja-chica-diario')
}

export function transferirSaldosCajaChicaBoveda(body: {
  oficinaId: number
  importe: number
  descripcion: string
}): Promise<boolean> {
  return postJson('/credito/transferir-saldos-caja-chica-boveda', body)
}

export function downloadMovimientoCajaChicaTicketPdf(
  movimientoCajaChicaId: number,
): Promise<void> {
  return apiDownload(
    `/credito/movimiento-caja-chica-ticket-pdf?movimientoCajaChicaId=${movimientoCajaChicaId}`,
    `ticket-caja-chica-${movimientoCajaChicaId}.pdf`,
  )
}

