import { apiFetch } from './client'
import { ApiError } from './errors'
import type { ClienteBuscarItem } from '../types/api'

export interface ClienteDetalle {
  personaId: number
  clienteId: number
  tipoPersona: string
  nombre: string
  apePaterno: string | null
  apeMaterno: string | null
  numeroDocumento: string
  tipoDocumento: string
  sexo: string
  email: string | null
  celular1: string | null
  direccion: string | null
  direccionRef: string | null
  distritoId: number | null
  distritoLabel: string | null
  fechaNacimiento: string | null
  actividadEconId: number | null
  calificacion: string
  nota: string | null
  clienteEstado: boolean
  personaEstado: boolean
  bloqueado: boolean
  topeCredito: number | null
  estadoCivilId: number | null
  tipoViviendaId: number | null
  conyuguePersonaId: number | null
  conyugueLabel: string | null
  clasificacionRiesgoSbsId: number | null
  clasificacionRiesgoSbsObs: string | null
  direccionNegocio: string | null
  direccionNegocioRef: string | null
  latitud: number | null
  longitud: number | null
  depuradoDescripcion: string | null
}

export interface ClienteListadoRow {
  personaId: number
  codigo: string
  cliente: string
  documento: string
  celular: string | null
  email: string | null
  direccion: string | null
}

export interface ClienteListadoResult {
  rows: ClienteListadoRow[]
  total: number
  page: number
  pageSize: number
}

export interface PersonaPorDocumento {
  personaId: number
  tieneCliente: boolean
  tipoPersona: string
  nombre: string
  apePaterno: string | null
  apeMaterno: string | null
  numeroDocumento: string
  sexo: string
  email: string | null
  celular1: string | null
  direccion: string | null
  fechaNacimiento: string | null
}

export interface GuardarClienteRequest {
  clienteId: number
  tipoPersona: string
  nombre: string
  apePaterno?: string | null
  apeMaterno?: string | null
  numeroDocumento: string
  sexoMasculino: boolean
  email?: string | null
  celular1?: string | null
  nota?: string | null
  fechaNacimiento?: string | null
  direccion?: string | null
  direccionRef?: string | null
  distritoId?: number | null
  direccionNegocio?: string | null
  direccionNegocioRef?: string | null
  latitud?: number | null
  longitud?: number | null
  ocupacionId?: number | null
  ocupacionOtros?: string | null
  calificacion: string
  activo: boolean
  topeCredito?: number | null
  estadoCivilId?: number | null
  tipoViviendaId?: number | null
  conyuguePersonaId?: number | null
  clasificacionRiesgoSbsId?: number | null
  clasificacionRiesgoSbsObs?: string | null
}

export interface GuardarClienteResponse {
  personaId: number
  clienteId: number
}

export interface ListarClientesParams {
  buscar?: string
  page?: number
  pageSize?: number
  sortField?: string
  sortDir?: 'asc' | 'desc'
}

/** Paridad ClienteBL.BuscarCliente modernizada: mínimo 2 caracteres, ranking rápido y hasta 20 filas. */
export function buscarClientes(term: string): Promise<ClienteBuscarItem[]> {
  return apiFetch<ClienteBuscarItem[]>(
    `/clientes/buscar?term=${encodeURIComponent(term)}`,
  )
}

/** Paridad jqGrid ListarCliente / LstClienteJGrid. */
export function listarClientes(
  params: ListarClientesParams = {},
): Promise<ClienteListadoResult> {
  const q = new URLSearchParams()
  if (params.buscar?.trim()) {
    q.set('buscar', params.buscar.trim())
  }
  if (params.page != null) {
    q.set('page', String(params.page))
  }
  if (params.pageSize != null) {
    q.set('pageSize', String(params.pageSize))
  }
  if (params.sortField) {
    q.set('sortField', params.sortField)
  }
  if (params.sortDir) {
    q.set('sortDir', params.sortDir)
  }
  const qs = q.toString()
  return apiFetch<ClienteListadoResult>(`/clientes/listar${qs ? `?${qs}` : ''}`)
}

export async function obtenerPersonaPorDocumento(
  documento: string,
): Promise<PersonaPorDocumento | null> {
  const doc = documento.trim()
  if (!doc) {
    return null
  }
  try {
    return await apiFetch<PersonaPorDocumento>(
      `/clientes/por-documento?documento=${encodeURIComponent(doc)}`,
    )
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) {
      return null
    }
    throw e
  }
}

export function obtenerCliente(personaId: number): Promise<ClienteDetalle> {
  return apiFetch<ClienteDetalle>(`/clientes/${personaId}`)
}

/** Paridad `ValidarClienteDNI`: true si ya existe en MAESTRO.Cliente (no solo Persona). */
export function documentoYaEsCliente(
  documento: string,
  excluirPersonaId?: number,
): Promise<boolean> {
  const q = new URLSearchParams({ documento })
  if (excluirPersonaId != null && excluirPersonaId > 0) {
    q.set('excluirPersonaId', String(excluirPersonaId))
  }
  return apiFetch<boolean>(`/clientes/existe-documento?${q.toString()}`)
}

export function guardarCliente(
  body: GuardarClienteRequest,
): Promise<GuardarClienteResponse> {
  return apiFetch<GuardarClienteResponse>('/clientes/guardar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function toggleClienteActivo(personaId: number): Promise<boolean> {
  return apiFetch<boolean>(`/clientes/${personaId}/activar`, { method: 'POST' })
}

export function toggleClienteBloqueado(personaId: number): Promise<boolean> {
  return apiFetch<boolean>(`/clientes/${personaId}/bloquear`, { method: 'POST' })
}

export function buscarDistritosCliente(term: string): Promise<ClienteBuscarItem[]> {
  return apiFetch<ClienteBuscarItem[]>(
    `/clientes/distritos-buscar?term=${encodeURIComponent(term)}`,
  )
}

export function buscarPersonasConyuge(term: string): Promise<ClienteBuscarItem[]> {
  return apiFetch<ClienteBuscarItem[]>(
    `/clientes/personas-buscar?term=${encodeURIComponent(term)}`,
  )
}

export function habilitarClienteDepurado(personaId: number): Promise<boolean> {
  return apiFetch<boolean>(`/clientes/${personaId}/habilitar-depurado`, { method: 'POST' })
}

export function crearPersonaRapida(body: {
  dni: string
  nombre: string
  apePaterno: string
  apeMaterno: string
  celular?: string | null
}): Promise<{ success: boolean; personaId: number; label: string }> {
  return apiFetch('/clientes/crear-persona-rapida', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

