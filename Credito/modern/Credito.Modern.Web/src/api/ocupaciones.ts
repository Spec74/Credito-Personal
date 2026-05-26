import { apiFetch } from './client'

export interface OcupacionListItem {
  ocupacionId: number
  denominacion: string
  estado: boolean | null
}

export function fetchOcupaciones(): Promise<OcupacionListItem[]> {
  return apiFetch<OcupacionListItem[]>('/ocupaciones')
}
