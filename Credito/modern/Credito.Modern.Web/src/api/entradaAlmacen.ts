import { apiDownload, apiFetch } from './client'
import type { MovimientoOperacionMensajeResponse } from './almacenMovimiento'

export interface MovimientoEntradaListRow {
  movimientoId: number
  tipo: string
  tipoMovimientoId: number
  tipoMovimiento: string
  fecha: string
  documento: string | null
  estado: string
  observacion: string | null
}

export interface MovimientoEntradaListPage {
  items: MovimientoEntradaListRow[]
  totalCount: number
  page: number
  pageSize: number
}

export interface MovimientoEntradaCabecera {
  movimientoId: number
  almacenId: number
  oficinaId: number
  almacen: string
  tipo: string
  tipoMovimientoId: number
  tipoMovimiento: string
  fecha: string
  documento: string | null
  estado: string
  estadoId: number
  observacion: string | null
  subTotal: number
  igv: number
  ajusteRedondeo: number
  totalImporte: number
  editable: boolean
}

export interface MovimientoDocRow {
  movimientoDocId: number
  tipoDocumentoId: number
  tipoDocumento: string
  serieDocumento: string
  nroDocumento: string
  puedeEliminar: boolean
}

export interface MovimientoEntradaDetLinea {
  movimientoDetId: number
  articuloId: number
  cantidad: number
  unidadMedidaT10: number
  unidadMedida: string
  descripcion: string
  precioUnitario: number
  descuento: number
  importe: number
  indCorrelativo: boolean
  puedeEliminar: boolean
}

export interface MovimientoEntradaDetalleResponse {
  cabecera: MovimientoEntradaCabecera
  documentos: MovimientoDocRow[]
  detalle: MovimientoEntradaDetLinea[]
}

export interface TipoDocumentoAlmacenItem {
  tipoDocumentoId: number
  denominacion: string
}

export interface ExisteSerieArticuloResponse {
  resultado: string | null
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchMovimientosEntrada(params: {
  oficinaId: number
  almacenId: number
  buscar?: string
  articuloId?: number
  page?: number
  pageSize?: number
}): Promise<MovimientoEntradaListPage> {
  const q = new URLSearchParams({
    oficinaId: String(params.oficinaId),
    almacenId: String(params.almacenId),
    articuloId: String(params.articuloId ?? 0),
    page: String(params.page ?? 1),
    pageSize: String(params.pageSize ?? 25),
  })
  if (params.buscar?.trim()) q.set('buscar', params.buscar.trim())
  return apiFetch<MovimientoEntradaListPage>(`/almacen/movimientos-entrada?${q}`)
}

export function fetchMovimientoEntradaDetalle(
  oficinaId: number,
  movimientoId: number,
): Promise<MovimientoEntradaDetalleResponse> {
  return apiFetch<MovimientoEntradaDetalleResponse>(
    `/almacen/movimiento-entrada/${movimientoId}?oficinaId=${oficinaId}`,
  )
}

export function crearMovimientoEntrada(body: {
  oficinaId: number
  almacenId: number
  tipoMovimientoId: number
}): Promise<{ movimientoId: number }> {
  return postJson('/almacen/crear-movimiento', body)
}

export function agregarMovimientoDocumento(body: {
  oficinaId: number
  movimientoId: number
  tipoDocumentoId: number
  serieDocumento: string
  nroDocumento: string
}): Promise<MovimientoOperacionMensajeResponse> {
  return postJson('/almacen/agregar-movimiento-documento', body)
}

export function eliminarMovimientoDocumento(body: {
  oficinaId: number
  movimientoDocId: number
}): Promise<MovimientoOperacionMensajeResponse> {
  return postJson('/almacen/eliminar-movimiento-documento', body)
}

export function actualizarImporteMovimiento(body: {
  oficinaId: number
  movimientoId: number
  ajusteRedondeo: number
}): Promise<MovimientoOperacionMensajeResponse> {
  return postJson('/almacen/actualizar-importe-movimiento', body)
}

export function fetchTiposDocumentoAlmacenMov(): Promise<TipoDocumentoAlmacenItem[]> {
  return apiFetch<TipoDocumentoAlmacenItem[]>('/tipos-documento-almacen-mov')
}

export interface RptConstanciaAlmacenCabecera {
  movimientoId: number
  oficina: string
  almacen: string
  tipo: string
  tipoMovimiento: string
  tipoMovimientoDesc: string
  fecha: string
  documento: string | null
  estado: string
  observacion: string | null
  importe: number
}

export interface RptConstanciaAlmacenDetLinea {
  cantidad: number
  descripcion: string
  precioUnitario: number
  descuento: number
  importe: number
}

export interface RptConstanciaAlmacenInforme {
  cabecera: RptConstanciaAlmacenCabecera
  detalle: RptConstanciaAlmacenDetLinea[]
}

export function fetchRptConstanciaAlmacen(
  oficinaId: number,
  movimientoId: number,
): Promise<RptConstanciaAlmacenInforme> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    movimientoId: String(movimientoId),
  })
  return apiFetch<RptConstanciaAlmacenInforme>(`/almacen/rpt-constancia-almacen?${q}`)
}

export function downloadRptConstanciaAlmacenCsv(
  oficinaId: number,
  movimientoId: number,
): Promise<void> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    movimientoId: String(movimientoId),
  })
  return apiDownload(
    `/almacen/rpt-constancia-almacen-csv?${q}`,
    `constancia-almacen-${movimientoId}.csv`,
  )
}

export function downloadRptConstanciaAlmacenPdf(
  oficinaId: number,
  movimientoId: number,
): Promise<void> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    movimientoId: String(movimientoId),
  })
  return apiDownload(
    `/almacen/rpt-constancia-almacen-pdf?${q}`,
    `constancia-almacen-${movimientoId}.pdf`,
  )
}

export function validarSeriesEntrada(
  oficinaId: number,
  listaSerie: string,
  cantidad?: number,
  indCorrelativo?: boolean,
): Promise<ExisteSerieArticuloResponse> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    listaSerie,
  })
  if (cantidad != null) q.set('cantidad', String(cantidad))
  if (indCorrelativo != null) q.set('indCorrelativo', String(indCorrelativo))
  return apiFetch<ExisteSerieArticuloResponse>(`/almacen/existe-serie-articulo?${q}`)
}
