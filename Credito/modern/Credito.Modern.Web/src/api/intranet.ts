import { apiFetch, getApiBaseUrl } from './client'
import { getAccessToken } from '../auth/tokenStorage'

export interface ComunicadoRow {
  comunicadoId: number
  titulo: string
  cuerpo: string
  alcance: string
  oficinaId: number | null
  fechaIni: string
  fechaFin: string | null
  permanente: boolean
  vigente: boolean
  leido: boolean
  adjuntos: number
  usuarioRegId: number
  nombreUsuario: string | null
  fechaReg: string
}

export interface ComunicadoAdjunto {
  adjuntoId: number
  comunicadoId: number
  nombreOriginal: string
  contentType: string
  tamanoBytes: number
}

export interface AgendaEvento {
  eventoId: number
  titulo: string
  notas: string | null
  fechaIni: string
  fechaFin: string | null
  todoElDia: boolean
  recordatorioMinutos: number | null
  alcance: string
  oficinaId: number | null
  usuarioId: number
  esMio: boolean
}

export interface BandejaItem {
  tipo: string
  id: number
  titulo: string
  fecha: string
}

export function fetchComunicados(vencidos = false) {
  return apiFetch<ComunicadoRow[]>(`/comunicados?vencidos=${vencidos ? 'true' : 'false'}`)
}

export function fetchComunicado(id: number) {
  return apiFetch<{ cabecera: ComunicadoRow; adjuntos: ComunicadoAdjunto[] }>(
    `/comunicados/${id}`,
  )
}

export function saveComunicado(body: {
  comunicadoId?: number | null
  titulo: string
  cuerpo: string
  alcance: string
  fechaIni: string
  fechaFin: string | null
}) {
  return apiFetch<{ id: number }>('/comunicados', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function bajaComunicado(id: number) {
  return apiFetch<{ id: number }>(`/comunicados/${id}/baja`, { method: 'POST' })
}

export function marcarComunicadoLeido(id: number) {
  return apiFetch<{ id: number }>(`/comunicados/${id}/leido`, { method: 'POST' })
}

export async function subirComunicadoAdjunto(id: number, file: File) {
  const fd = new FormData()
  fd.append('archivo', file)
  return apiFetch<{ id: number }>(`/comunicados/${id}/adjunto`, { method: 'POST', body: fd })
}

export function adjuntoUrl(adjuntoId: number) {
  return `${getApiBaseUrl()}/comunicados/adjunto/${adjuntoId}`
}

export async function descargarAdjunto(adjuntoId: number, nombre: string) {
  const token = getAccessToken()
  const res = await fetch(adjuntoUrl(adjuntoId), {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  })
  if (!res.ok) throw new Error('No se pudo descargar el archivo.')
  const blob = await res.blob()
  const href = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = href
  a.download = nombre
  a.click()
  URL.revokeObjectURL(href)
}

export function fetchAgenda(desde: string, hasta: string) {
  const q = new URLSearchParams({ desde, hasta })
  return apiFetch<AgendaEvento[]>(`/agenda?${q}`)
}

export function saveAgenda(body: {
  eventoId?: number | null
  titulo: string
  notas?: string | null
  fechaIni: string
  fechaFin?: string | null
  todoElDia: boolean
  recordatorioMinutos?: number | null
  alcance: string
}) {
  return apiFetch<{ id: number }>('/agenda', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function bajaAgenda(id: number) {
  return apiFetch<{ id: number }>(`/agenda/${id}/baja`, { method: 'POST' })
}

export function descartarAgenda(id: number) {
  return apiFetch<{ id: number }>(`/agenda/${id}/descartar`, { method: 'POST' })
}

export function fetchBandeja() {
  return apiFetch<BandejaItem[]>('/bandeja')
}
