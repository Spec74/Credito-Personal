import { apiFetch } from './client'
import type { AlmacenListItem } from '../types/api'
import { normalizeAlmacenes } from './normalize'

export async function fetchAlmacenes(oficinaId?: number): Promise<AlmacenListItem[]> {
  const qs =
    oficinaId != null && oficinaId >= 1 ? `?oficinaId=${oficinaId}` : ''
  const raw = await apiFetch<unknown>(`/almacenes${qs}`)
  return normalizeAlmacenes(raw)
}
