import { apiFetch } from './client'

export interface TareaListItem {
  tareaId: number
  creditoId: number
  clienteDni: string
  clienteNombre: string
  montoCredito: number
  nombreUsuario: string
  fechaCreacion: string
  fechaCompletada: string | null
  estado: string
  totalSubtareas: number
  subtareasCompletadas: number
}

export interface SubtareaItem {
  subtareaId: number
  tareaId: number
  titulo: string
  completada: boolean
  fechaCompletada: string | null
}

export interface TareaDetalle {
  tareaId: number
  creditoId: number
  clienteDni: string
  clienteNombre: string
  montoCredito: number
  nombreUsuario: string | null
  fechaCreacion: string
  fechaCompletada: string | null
  estado: string
  totalSubtareas: number
  subtareasCompletadas: number
  subtareas: SubtareaItem[]
}

export interface CreditoTareaBuscar {
  creditoId: number
  personaId: number
  dni: string
  nombre: string
  montoCredito: number
  label: string
}

export interface SubtareaGuardarInput {
  titulo: string
  completada: boolean
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchTareas(estado?: string): Promise<TareaListItem[]> {
  const q = estado ? `?estado=${encodeURIComponent(estado)}` : ''
  return apiFetch<TareaListItem[]>(`/credito/tareas${q}`)
}

export function fetchPuedeEditarTarea(): Promise<{ puedeEditar: boolean }> {
  return apiFetch<{ puedeEditar: boolean }>('/credito/tareas/puede-editar')
}

export function buscarCreditosTarea(term: string): Promise<CreditoTareaBuscar[]> {
  const q = new URLSearchParams({ term: term.trim() })
  return apiFetch<CreditoTareaBuscar[]>(`/credito/tareas/creditos-buscar?${q}`)
}

export function fetchTareaDetalle(tareaId: number): Promise<TareaDetalle> {
  return apiFetch<TareaDetalle>(`/credito/tareas/${tareaId}`)
}

export function guardarTarea(body: {
  tareaId: number
  creditoId: number
  subtareas: SubtareaGuardarInput[]
}): Promise<{ tareaId: number; mensaje: string }> {
  return postJson('/credito/tareas/guardar', body)
}

export function eliminarTarea(tareaId: number): Promise<{ success: boolean; mensaje: string }> {
  return postJson(`/credito/tareas/${tareaId}/eliminar`, {})
}

export function completarTarea(
  tareaId: number,
  completada: boolean,
): Promise<{ success: boolean; mensaje: string }> {
  return postJson(`/credito/tareas/${tareaId}/completar`, { completada })
}
