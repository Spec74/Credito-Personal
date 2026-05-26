import { apiDownload, apiFetch } from './client'
import type { CajaDiarioVentaRapida, CodigoBarrasLstRow } from '../types/api'

export interface ArticuloVentaRapida {
  articuloId: number
  codArticulo: string
  denominacion: string
  precioVenta: number | null
  stock: number
}

export interface PedidoLinea {
  articuloId: number
  cantidad: number
  descuento: number
}

export interface RealizarPedidoResponse {
  ordenVentaId: number
  resultId: number | null
}

export interface EnviarOrdenVentaResponse {
  ordenVentaId: number
  creditoId: number | null
}

export interface OrdenVentaOperacionMensajeResponse {
  mensaje: string
}

export interface OrdenVentaOperacionResultResponse {
  resultCode: number
}

export interface OrdenVentaListRow {
  ordenVentaId: number
  fechaReg: string
  cliente: string
  totalDescuento: number
  totalNeto: number
  tipoVenta: string
  estado: string
  estadoCredito: string | null
  puedeEliminar: boolean
}

export interface OrdenVentaListPage {
  items: OrdenVentaListRow[]
  totalCount: number
  page: number
  pageSize: number
}

export interface OrdenVentaCabecera {
  ordenVentaId: number
  oficinaId: number
  personaId: number
  cliente: string
  subtotal: number
  totalImpuesto: number
  totalNeto: number
  totalDescuento: number
  estado: string
  tipoVenta: string
  fechaReg: string
  estadoCredito: string | null
  puedeEliminar: boolean
}

export interface OrdenVentaDetLinea {
  ordenVentaDetId: number
  articuloId: number
  cantidad: number
  descripcion: string
  valorVenta: number
  descuento: number
  subtotal: number
  estado: boolean
}

export interface OrdenVentaDetalleResponse {
  cabecera: OrdenVentaCabecera
  detalle: OrdenVentaDetLinea[]
  cantidadTotal: number
}

export interface CrearOrdenVentaResponse {
  ordenVentaId: number
}

export interface RptRentabilidadVentaRow {
  nro: number | null
  codigo: string | null
  articulo: string | null
  movimientoId: number | null
  fechaEnt: string | null
  precioEnt: number | null
  ordenVentaId: number | null
  fechaSal: string | null
  precioSal: number | null
  modalidad: string | null
  rentabilidad: number | null
  cliente: string | null
}

export interface RentabilidadVentaParams {
  oficinaId: number
  fechaIni: string
  fechaFin: string
  indContado?: boolean
  indCredito?: boolean
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

function queryRentabilidadVenta(p: RentabilidadVentaParams): string {
  const q = new URLSearchParams({
    oficinaId: String(p.oficinaId),
    fechaIni: p.fechaIni,
    fechaFin: p.fechaFin,
  })
  if (p.indContado != null) q.set('indContado', String(p.indContado))
  if (p.indCredito != null) q.set('indCredito', String(p.indCredito))
  return q.toString()
}

export function fetchCajaDiarioVentaRapida(
  oficinaId: number,
): Promise<CajaDiarioVentaRapida> {
  return apiFetch<CajaDiarioVentaRapida>(
    `/ventas/caja-diario-venta-rapida?oficinaId=${oficinaId}`,
  )
}

export function fetchArticuloVentaRapida(
  oficinaId: number,
  codigo: string,
): Promise<ArticuloVentaRapida> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    codigo: codigo.trim(),
  })
  return apiFetch<ArticuloVentaRapida>(`/ventas/articulo-venta-rapida?${q}`)
}

export function realizarPedido(body: {
  oficinaId: number
  cajaDiarioId: number
  personaId: number
  pedidos: PedidoLinea[]
}): Promise<RealizarPedidoResponse> {
  return postJson('/ventas/realizar-pedido', body)
}

export function fetchOrdenesVenta(params: {
  oficinaId: number
  entregado?: boolean
  buscar?: string
  page?: number
  pageSize?: number
}): Promise<OrdenVentaListPage> {
  const q = new URLSearchParams({
    oficinaId: String(params.oficinaId),
    entregado: String(params.entregado ?? false),
    page: String(params.page ?? 1),
    pageSize: String(params.pageSize ?? 25),
  })
  if (params.buscar?.trim()) {
    q.set('buscar', params.buscar.trim())
  }
  return apiFetch<OrdenVentaListPage>(`/ventas/ordenes-venta?${q}`)
}

export function fetchOrdenVentaDetalle(
  oficinaId: number,
  ordenVentaId: number,
): Promise<OrdenVentaDetalleResponse> {
  return apiFetch<OrdenVentaDetalleResponse>(
    `/ventas/orden-venta/${ordenVentaId}?oficinaId=${oficinaId}`,
  )
}

export function crearOrdenVenta(body: {
  oficinaId: number
  personaId: number
  tipoVenta?: 'CON' | 'CRE'
}): Promise<CrearOrdenVentaResponse> {
  return postJson('/ventas/crear-orden-venta', body)
}

export function agregarOrdenVentaDetalle(body: {
  oficinaId: number
  ordenVentaId: number
  numeroSerie: string
}): Promise<OrdenVentaOperacionMensajeResponse> {
  return postJson('/ventas/agregar-orden-venta-detalle', body)
}

export function actualizarOrdenVentaDetalle(body: {
  oficinaId: number
  ordenVentaDetId: number
  descuento: number
}): Promise<OrdenVentaOperacionResultResponse> {
  return postJson('/ventas/actualizar-orden-venta-detalle', body)
}

export function eliminarOrdenVentaDetalle(body: {
  oficinaId: number
  ordenVentaDetId: number
}): Promise<OrdenVentaOperacionResultResponse> {
  return postJson('/ventas/eliminar-orden-venta-detalle', body)
}

export function eliminarOrdenVenta(body: {
  oficinaId: number
  ordenVentaId: number
}): Promise<OrdenVentaOperacionResultResponse> {
  return postJson('/ventas/eliminar-orden-venta', body)
}

export function enviarOrdenVentaContado(body: {
  oficinaId: number
  ordenVentaId: number
}): Promise<EnviarOrdenVentaResponse> {
  return postJson('/ventas/enviar-orden-venta-contado', body)
}

export function enviarOrdenVentaCredito(body: {
  oficinaId: number
  ordenVentaId: number
}): Promise<EnviarOrdenVentaResponse> {
  return postJson('/ventas/enviar-orden-venta-credito', body)
}

export function fetchRentabilidadVenta(
  params: RentabilidadVentaParams,
): Promise<RptRentabilidadVentaRow[]> {
  return apiFetch<RptRentabilidadVentaRow[]>(
    `/ventas/rpt-rentabilidad-venta?${queryRentabilidadVenta(params)}`,
  )
}

export function downloadRentabilidadVentaCsv(
  params: RentabilidadVentaParams,
): Promise<void> {
  return apiDownload(
    `/ventas/rpt-rentabilidad-venta-csv?${queryRentabilidadVenta(params)}`,
    'rentabilidad-venta.csv',
  )
}

export function downloadRentabilidadVentaPdf(
  params: RentabilidadVentaParams,
): Promise<void> {
  return apiDownload(
    `/ventas/rpt-rentabilidad-venta-pdf?${queryRentabilidadVenta(params)}`,
    'rentabilidad-venta.pdf',
  )
}

export function fetchCodigoBarrasLst(
  movimientoId: number,
): Promise<CodigoBarrasLstRow[]> {
  return apiFetch<CodigoBarrasLstRow[]>(
    `/ventas/codigo-barras-lst?movimientoId=${movimientoId}`,
  )
}

export function downloadCodigoBarrasLstCsv(movimientoId: number): Promise<void> {
  return apiDownload(
    `/ventas/codigo-barras-lst-csv?movimientoId=${movimientoId}`,
    `codigo-barras-${movimientoId}.csv`,
  )
}

export function downloadCodigoBarrasLstPdf(movimientoId: number): Promise<void> {
  return apiDownload(
    `/ventas/codigo-barras-lst-pdf?movimientoId=${movimientoId}`,
    `codigo-barras-${movimientoId}.pdf`,
  )
}

