import { apiFetch } from './client'
import type { MaestroOperacionResponse } from './maestrosCrud'

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface CajaGestionRow {
  cajaId: number
  denominacion: string
  oficinaId: number
  oficinaDenominacion: string | null
  cajeroId: number | null
  cajeroNombre: string | null
  estado: boolean
  indAbierto: boolean
}

export interface CajaGestionPage {
  page: number
  pageSize: number
  totalRecords: number
  totalPages: number
  rows: CajaGestionRow[]
}

export interface CajaGestorItem {
  usuarioId: number
  nombreCompleto: string
}

export function fetchCajasGestion(params: {
  buscar?: string
  page?: number
  pageSize?: number
  incluirInactivos?: boolean
}): Promise<CajaGestionPage> {
  const q = new URLSearchParams()
  q.set('page', String(params.page ?? 1))
  q.set('pageSize', String(params.pageSize ?? 25))
  if (params.incluirInactivos) q.set('incluirInactivos', 'true')
  if (params.buscar?.trim()) q.set('buscar', params.buscar.trim())
  return apiFetch(`/cajas/gestion?${q}`)
}

export function fetchCajaGestores(): Promise<CajaGestorItem[]> {
  return apiFetch('/cajas/gestores-activos')
}

export function guardarCaja(body: {
  cajaId: number
  oficinaId: number
  denominacion: string
  cajeroId?: number | null
  estado: boolean
}): Promise<MaestroOperacionResponse> {
  return postJson('/cajas/guardar', body)
}

export function activarCaja(cajaId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/cajas/${cajaId}/activar`, {})
}
