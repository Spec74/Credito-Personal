import { apiFetch } from './client'

export interface TipoMovimientoAlmacenListItem {
  tipoMovimientoId: number
  denominacion: string
  descripcion: string | null
  indEntrada: boolean
  indTransferencia: boolean | null
  indDevolucion: boolean | null
  estado: boolean
}

export interface MovimientoOperacionMensajeResponse {
  mensaje: string
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchTiposMovimientoAlmacen(): Promise<TipoMovimientoAlmacenListItem[]> {
  return apiFetch<TipoMovimientoAlmacenListItem[]>('/tipos-movimiento-almacen')
}

export function actualizarMovimientoAlmacen(body: {
  oficinaId: number
  movimientoId: number
  tipoMovimientoId: number
  fecha: string
  observacion?: string
}): Promise<MovimientoOperacionMensajeResponse> {
  return postJson('/almacen/actualizar-movimiento', body)
}

export function confirmarMovimientoAlmacen(body: {
  oficinaId: number
  movimientoId: number
}): Promise<MovimientoOperacionMensajeResponse> {
  return postJson('/almacen/confirmar-movimiento', body)
}

export function desconfirmarMovimientoAlmacen(body: {
  oficinaId: number
  movimientoId: number
}): Promise<MovimientoOperacionMensajeResponse> {
  return postJson('/almacen/desconfirmar-movimiento', body)
}

export function crearMovimientoDetalle(body: {
  oficinaId: number
  movimientoId: number
  movimientoDetId: number
  articuloId: number
  indAutogenerar: boolean
  listaSerie?: string
  cantidad: number
  indCorrelativo: boolean
  precioUnitario: number
  descuento: number
  medida: number
}): Promise<MovimientoOperacionMensajeResponse> {
  return postJson('/almacen/crear-movimiento-detalle', body)
}

export function eliminarMovimientoDetalle(body: {
  oficinaId: number
  movimientoDetId: number
}): Promise<MovimientoOperacionMensajeResponse> {
  return postJson('/almacen/eliminar-movimiento-detalle', body)
}
