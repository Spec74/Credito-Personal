import { apiFetch } from './client'
import type { ValorTablaListItem } from '../types/api'

export function fetchValoresTabla(tablaId: number): Promise<ValorTablaListItem[]> {
  return apiFetch<ValorTablaListItem[]>(
    `/valores-tabla?tablaId=${tablaId}&soloItemIdPositivo=true`,
  )
}
