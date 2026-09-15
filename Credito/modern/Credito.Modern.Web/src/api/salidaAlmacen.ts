import { apiFetch } from './client'

export interface BuscarSerieSalidaResponse {
  error: boolean
  mensaje: string | null
  serieId: number | null
  serie: string | null
  articuloId: number | null
  denominacion: string | null
}

export interface SerieSalidaLinea {
  serieId: number
  serie: string
  articuloId: number
  denominacion: string
}

export interface RealizarSalidaResponse {
  movimientoId: number
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function buscarSerieSalida(
  numeroSerie: string,
): Promise<BuscarSerieSalidaResponse> {
  const q = new URLSearchParams({ numeroSerie: numeroSerie.trim() })
  return apiFetch<BuscarSerieSalidaResponse>(`/almacen/buscar-serie-salida?${q}`)
}

export function realizarSalidaAlmacen(body: {
  oficinaId: number
  tipoMovimientoId: number
  glosa: string
  series: SerieSalidaLinea[]
}): Promise<RealizarSalidaResponse> {
  return postJson('/almacen/realizar-salida', body)
}
