import { apiFetch } from './client'
import type { MarcaListItem } from '../types/api'
import { normalizeMarcas } from './normalize'

export async function fetchMarcas(): Promise<MarcaListItem[]> {
  const raw = await apiFetch<unknown>('/marcas')
  return normalizeMarcas(raw)
}
