import { apiFetch } from './client'
import type { MaestroOperacionResponse } from './maestrosCrud'

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface RolGestionRow {
  rolId: number
  denominacion: string
  estado: boolean
}

export interface MenuAsignacion {
  menuId: number
  denominacion: string
  asignado: boolean
}

export interface RolMenusDetalle {
  rol: { rolId: number; denominacion: string; estado: boolean }
  menus: MenuAsignacion[]
}

export function fetchRolesGestion(incluirInactivos: boolean): Promise<RolGestionRow[]> {
  const q = new URLSearchParams({ incluirInactivos: String(incluirInactivos) })
  return apiFetch(`/roles/gestion?${q}`)
}

export function fetchRolMenusDetalle(rolId: number): Promise<RolMenusDetalle> {
  return apiFetch(`/roles/${rolId}/menus-detalle`)
}

export function guardarRol(body: {
  rolId: number
  denominacion: string
  estado: boolean
}): Promise<MaestroOperacionResponse> {
  return postJson('/roles/guardar', body)
}

export function activarRol(rolId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/roles/${rolId}/activar`, {})
}

export function asignarMenusRol(
  rolId: number,
  menuIds: number[],
): Promise<MaestroOperacionResponse> {
  return postJson(`/roles/${rolId}/asignar-menus`, { menuIds })
}
