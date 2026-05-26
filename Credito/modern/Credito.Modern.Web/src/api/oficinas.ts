import { apiFetch } from './client'
import type { OficinaListItem } from '../types/api'
import { normalizeOficinas } from './normalize'

export async function fetchOficinas(): Promise<OficinaListItem[]> {
  const raw = await apiFetch<unknown>('/oficinas')
  return normalizeOficinas(raw)
}
