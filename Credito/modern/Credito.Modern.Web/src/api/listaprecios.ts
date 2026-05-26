import { apiFetch } from './client'
import type { ListaPrecioListItem } from '../types/api'
import { normalizeListaPrecios } from './normalize'

export async function fetchListaPrecios(
  articuloId?: number,
): Promise<ListaPrecioListItem[]> {
  const qs =
    articuloId != null && articuloId >= 1 ? `?articuloId=${articuloId}` : ''
  const raw = await apiFetch<unknown>(`/lista-precios${qs}`)
  return normalizeListaPrecios(raw)
}
