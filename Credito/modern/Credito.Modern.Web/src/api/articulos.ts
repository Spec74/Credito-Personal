import { apiFetch } from './client'
import type { ArticuloListItem } from '../types/api'
import { normalizeArticulos } from './normalize'

export async function fetchArticulos(
  modeloId?: number,
  tipoArticuloId?: number,
): Promise<ArticuloListItem[]> {
  const q = new URLSearchParams()
  if (modeloId != null && modeloId >= 1) {
    q.set('modeloId', String(modeloId))
  }
  if (tipoArticuloId != null && tipoArticuloId >= 1) {
    q.set('tipoArticuloId', String(tipoArticuloId))
  }
  const qs = q.toString()
  const raw = await apiFetch<unknown>(`/articulos${qs ? `?${qs}` : ''}`)
  return normalizeArticulos(raw)
}
