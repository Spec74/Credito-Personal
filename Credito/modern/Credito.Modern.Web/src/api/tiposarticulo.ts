import { apiFetch } from './client'
import { normalizeTiposArticulo } from './normalize'
import type { TipoArticuloListItem } from '../types/api'

export async function fetchTiposArticulo(): Promise<TipoArticuloListItem[]> {
  const raw = await apiFetch<unknown>('/tipos-articulo')
  return normalizeTiposArticulo(raw)
}
