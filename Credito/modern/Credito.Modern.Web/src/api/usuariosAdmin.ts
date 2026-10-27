import { apiFetch } from './client'
import type { MaestroOperacionResponse } from './maestrosCrud'

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface UsuarioGestionRow {
  usuarioId: number
  nombreUsuario: string
  nombreCompleto: string
  celular1: string | null
  emailPersonal: string | null
  estado: boolean
  enEstaSede: boolean
  sedes: string | null
}

export interface UsuarioGestionPage {
  page: number
  pageSize: number
  totalRecords: number
  totalPages: number
  rows: UsuarioGestionRow[]
  esCentral: boolean
  oficinaId: number
  oficina: string | null
  verRed: boolean
}

export interface OficinaAsignacion {
  oficinaId: number
  denominacion: string
  asignado: boolean
  editable: boolean
}

export interface RolAsignacion {
  rolId: number
  denominacion: string
  asignado: boolean
}

export interface UsuarioDetalle {
  usuarioId: number
  nombreUsuario: string
  estado: boolean
  personaId: number
  apePaterno: string
  apeMaterno: string
  nombre: string
  nombreCompleto: string
  numeroDocumento: string
  sexo: string | null
  fechaNacimiento: string | null
  celular1: string | null
  emailPersonal: string | null
  direccion: string | null
  oficinas: OficinaAsignacion[]
  puedeAsignarOtrasOficinas: boolean
  puedeDesactivarCuenta: boolean
  enEstaSede: boolean
  otrasSedes: string[] | null
}

export interface UsuarioReporteGestor {
  usuarioId: number
  nombreUsuario: string
  nombreCompleto: string
}

export function fetchUsuariosReporteGestores(): Promise<UsuarioReporteGestor[]> {
  return apiFetch('/usuarios/reporte-gestores')
}

export function fetchUsuariosGestion(params: {
  buscar?: string
  page?: number
  pageSize?: number
  incluirInactivos?: boolean
  verRed?: boolean
  sedeId?: number
}): Promise<UsuarioGestionPage> {
  const q = new URLSearchParams()
  q.set('page', String(params.page ?? 1))
  q.set('pageSize', String(params.pageSize ?? 25))
  if (params.incluirInactivos) q.set('incluirInactivos', 'true')
  if (params.buscar?.trim()) q.set('buscar', params.buscar.trim())
  if (params.verRed === false) q.set('verRed', 'false')
  if (params.verRed === true) q.set('verRed', 'true')
  if (params.sedeId && params.sedeId >= 1) q.set('sedeId', String(params.sedeId))
  return apiFetch(`/usuarios/gestion?${q}`)
}

export function fetchUsuarioDetalle(usuarioId: number): Promise<UsuarioDetalle> {
  return apiFetch(`/usuarios/${usuarioId}/detalle`)
}

export function validarDniUsuario(dni: string, usuarioId?: number): Promise<{ existe: boolean }> {
  const q = new URLSearchParams({ dni })
  if (usuarioId && usuarioId >= 1) q.set('usuarioId', String(usuarioId))
  return apiFetch(`/usuarios/validar-dni?${q}`)
}

export function fetchRolesAsignacion(
  usuarioId: number,
  oficinaId: number,
): Promise<RolAsignacion[]> {
  const q = new URLSearchParams({ oficinaId: String(oficinaId) })
  return apiFetch(`/usuarios/${usuarioId}/roles-asignacion?${q}`)
}

export function guardarUsuario(body: {
  usuarioId: number
  apePaterno: string
  apeMaterno: string
  nombre: string
  numeroDocumento: string
  sexo: string
  fechaNacimiento?: string | null
  telefonoMovil?: string | null
  emailPersonal?: string | null
  direccion?: string | null
  nombreUsuario: string
  claveUsuario: string
  estado: boolean
  oficinaIds?: number[]
  rolIds?: number[]
}): Promise<MaestroOperacionResponse> {
  return postJson('/usuarios/guardar', body)
}

/** Clave temporal legible (sin 0/O/1/l), alineada al hasher del API. */
export function generarClaveTemporal(length = 10): string {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
  const bytes = new Uint8Array(length)
  crypto.getRandomValues(bytes)
  const chars = Array.from(bytes, (b) => alphabet[b % alphabet.length])
  chars[0] = 'A'
  chars[1] = '7'
  return chars.join('')
}

export function sugerirNombreUsuario(nombre: string, apePaterno: string): string {
  const n = nombre.trim().split(/\s+/)[0] ?? ''
  const a = apePaterno.trim().replace(/\s+/g, '')
  if (!n || !a) return ''
  return `${n[0] ?? ''}${a}`.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 20)
}

export function activarUsuario(usuarioId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/usuarios/${usuarioId}/activar`, {})
}

export interface ResetearClaveResponse {
  success: boolean
  id: number | null
  mensaje: string | null
  claveTemporal: string | null
}

export function resetearClaveUsuario(usuarioId: number): Promise<ResetearClaveResponse> {
  return postJson(`/usuarios/${usuarioId}/resetear-clave`, {})
}

export function asignarOficinasUsuario(
  usuarioId: number,
  oficinaIds: number[],
): Promise<MaestroOperacionResponse> {
  return postJson(`/usuarios/${usuarioId}/asignar-oficinas`, { oficinaIds })
}

export function asignarRolesUsuario(
  usuarioId: number,
  oficinaId: number,
  rolIds: number[],
): Promise<MaestroOperacionResponse> {
  return postJson(`/usuarios/${usuarioId}/asignar-roles`, { oficinaId, rolIds })
}
