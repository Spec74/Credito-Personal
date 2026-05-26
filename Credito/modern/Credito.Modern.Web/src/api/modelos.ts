import { apiFetch } from './client'
import type { ModeloListItem } from '../types/api'
import { normalizeModelos } from './normalize'

export async function fetchModelos(marcaId?: number): Promise<ModeloListItem[]> {
  const qs =
    marcaId != null && marcaId >= 1 ? `?marcaId=${marcaId}` : ''
  const raw = await apiFetch<unknown>(`/modelos${qs}`)
  return normalizeModelos(raw)
}
