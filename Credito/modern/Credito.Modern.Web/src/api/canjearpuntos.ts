import { apiFetch } from './client'

export interface TarjetaPunto {
  tarjetaPuntoId: number
  personaId: number
  totalPuntos: number
  estado: boolean
}

export interface ArticuloCanjeListItem {
  listaPrecioId: number
  articuloId: number
  tipoArticulo: string
  articuloDesc: string
  puntosCanje: number | null
  estado: boolean
}

export interface CanjearPuntosResponse {
  mensaje: string
}

export function fetchTarjetaPuntos(personaId: number): Promise<TarjetaPunto> {
  return apiFetch<TarjetaPunto>(
    `/ventas/tarjeta-puntos?personaId=${personaId}`,
  )
}

export function fetchArticulosCanjear(
  personaId: number,
): Promise<ArticuloCanjeListItem[]> {
  return apiFetch<ArticuloCanjeListItem[]>(
    `/ventas/articulos-canjear?personaId=${personaId}`,
  )
}

export function canjearPuntos(
  personaId: number,
  numeroSerie: string,
): Promise<CanjearPuntosResponse> {
  return apiFetch<CanjearPuntosResponse>('/ventas/canjear-puntos', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ personaId, numeroSerie }),
  })
}
