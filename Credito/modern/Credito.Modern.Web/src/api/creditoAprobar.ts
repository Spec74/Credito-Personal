import { apiFetch } from './client'
import type { CreditoCicloOperacionResult } from '../types/api'

export interface CreditoPorAprobarRow {
  creditoId: number
  personaId: number
  codigo: string | null
  cliente: string | null
  documento: string | null
  monto: number
  interes: number
  estado: string
  agente: string | null
}

export interface CreditosPorAprobarResponse {
  items: CreditoPorAprobarRow[]
  total: number
}

export interface CreditosPorAprobarQuery {
  oficinaId: number
  buscar?: string
  page?: number
  pageSize?: number
  sortField?: string
  sortOrder?: 'asc' | 'desc'
}

const SORT_API: Record<string, string> = {
  creditoId: 'CreditoId',
  codigo: 'Codigo',
  cliente: 'Cliente',
  documento: 'Documento',
  monto: 'Monto',
  interes: 'Interes',
  agente: 'Agente',
}

function queryParams(q: CreditosPorAprobarQuery): string {
  const p = new URLSearchParams()
  p.set('oficinaId', String(q.oficinaId))
  p.set('page', String(q.page ?? 1))
  p.set('pageSize', String(q.pageSize ?? 15))
  if (q.buscar?.trim()) {
    p.set('buscar', q.buscar.trim())
  }
  if (q.sortField) {
    p.set('sortField', SORT_API[q.sortField] ?? 'Agente')
  }
  if (q.sortOrder) {
    p.set('sortOrder', q.sortOrder)
  }
  return p.toString()
}

export function fetchCreditosPorAprobar(
  params: CreditosPorAprobarQuery,
): Promise<CreditosPorAprobarResponse> {
  return apiFetch<CreditosPorAprobarResponse>(
    `/credito/creditos-por-aprobar?${queryParams(params)}`,
  )
}

export function aprobarCredito(
  oficinaId: number,
  creditoId: number,
  opcion: 0 | 1,
): Promise<CreditoCicloOperacionResult> {
  return apiFetch<CreditoCicloOperacionResult>('/credito/aprobar-credito', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ oficinaId, creditoId, opcion }),
  })
}

export function rechazarCredito(
  oficinaId: number,
  creditoId: number,
): Promise<CreditoCicloOperacionResult> {
  return apiFetch<CreditoCicloOperacionResult>('/credito/rechazar-credito', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ oficinaId, creditoId }),
  })
}
