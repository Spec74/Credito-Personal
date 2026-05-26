import { apiFetch } from './client'

export interface TransferenciaListRow {
  transferenciaId: number
  almacenOrigen: string
  almacenDestino: string
  fecha: string
  estado: string
}

export interface TransferenciaListPage {
  items: TransferenciaListRow[]
  totalCount: number
  page: number
  pageSize: number
}

export interface TransferenciaCabecera {
  transferenciaId: number
  almacenOrigenId: number
  almacenDestinoId: number
  almacenOrigen: string
  almacenDestino: string
  fecha: string
  estado: string
  editable: boolean
}

export interface TransferenciaDetalleLinea {
  transferenciaId: number
  articuloId: number
  articulo: string
  cantidad: number
  series: string
}

export interface ValidarSerieTransferenciaResponse {
  error: boolean
  mensaje: string | null
  serieId: number | null
  numeroSerie: string | null
  articuloId: number | null
  denominacion: string | null
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchTransferencias(params: {
  oficinaId: number
  buscar?: string
  almacenId?: number
  articuloId?: number
  page?: number
  pageSize?: number
}): Promise<TransferenciaListPage> {
  const q = new URLSearchParams({
    oficinaId: String(params.oficinaId),
    almacenId: String(params.almacenId ?? 0),
    articuloId: String(params.articuloId ?? 0),
    page: String(params.page ?? 1),
    pageSize: String(params.pageSize ?? 25),
  })
  if (params.buscar?.trim()) {
    q.set('buscar', params.buscar.trim())
  }
  return apiFetch<TransferenciaListPage>(`/almacen/transferencias?${q}`)
}

export function fetchTransferenciaCabecera(
  transferenciaId: number,
  oficinaId: number,
): Promise<TransferenciaCabecera> {
  const q = new URLSearchParams({ oficinaId: String(oficinaId) })
  return apiFetch<TransferenciaCabecera>(
    `/almacen/transferencia/${transferenciaId}?${q}`,
  )
}

export function fetchTransferenciaDetalle(
  transferenciaId: number,
  oficinaId: number,
): Promise<TransferenciaDetalleLinea[]> {
  const q = new URLSearchParams({ oficinaId: String(oficinaId) })
  return apiFetch<TransferenciaDetalleLinea[]>(
    `/almacen/transferencia/${transferenciaId}/detalle?${q}`,
  )
}

export function crearTransferencia(body: {
  oficinaId: number
  almacenDestinoId: number
}): Promise<{ transferenciaId: number }> {
  return postJson('/almacen/crear-transferencia', body)
}

export function validarSerieTransferencia(body: {
  oficinaId: number
  transferenciaId: number
  numeroSerie: string
}): Promise<ValidarSerieTransferenciaResponse> {
  return postJson('/almacen/validar-serie-transferencia', body)
}

export function eliminarSerieTransferencia(body: {
  oficinaId: number
  transferenciaId: number
  articuloId: number
}): Promise<boolean> {
  return postJson('/almacen/eliminar-serie-transferencia', body)
}

export function desconfirmarTransferencia(body: {
  oficinaId: number
  transferenciaId: number
}): Promise<boolean> {
  return postJson('/almacen/desconfirmar-transferencia', body)
}

export function confirmarTransferencia(body: {
  oficinaId: number
  transferenciaId: number
}): Promise<{ success: boolean; mensaje: string }> {
  return postJson('/almacen/confirmar-transferencia', body)
}
