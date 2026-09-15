import { apiFetch } from './client'
import { getAccessToken } from '../auth/tokenStorage'
import type { MaestroOperacionResponse } from './maestrosCrud'

const baseUrl = import.meta.env.VITE_API_BASE_URL as string

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface ArticuloGestionRow {
  articuloId: number
  modeloId: number | null
  modeloDenominacion: string | null
  tipoArticuloId: number | null
  tipoArticuloDenominacion: string | null
  codArticulo: string
  denominacion: string
  descripcion: string | null
  indPerecible: boolean | null
  indImportado: boolean | null
  indCanjeable: boolean | null
  estado: boolean
  monto: number | null
  descuento: number | null
}

export interface ArticuloDetalle {
  articuloId: number
  modeloId: number | null
  tipoArticuloId: number | null
  codArticulo: string
  denominacion: string
  descripcion: string | null
  indPerecible: boolean | null
  indImportado: boolean | null
  indCanjeable: boolean | null
  estado: boolean
  monto: number | null
  descuento: number | null
  listaPrecioId: number | null
}

export interface ArticuloBuscarItem {
  articuloId: number
  denominacion: string
}

export function fetchArticulosGestion(
  incluirInactivos: boolean,
  modeloId?: number,
  tipoArticuloId?: number,
): Promise<ArticuloGestionRow[]> {
  const q = new URLSearchParams({ incluirInactivos: String(incluirInactivos) })
  if (modeloId && modeloId >= 1) q.set('modeloId', String(modeloId))
  if (tipoArticuloId && tipoArticuloId >= 1) q.set('tipoArticuloId', String(tipoArticuloId))
  return apiFetch(`/articulos/gestion?${q}`)
}

export function fetchArticuloDetalle(articuloId: number): Promise<ArticuloDetalle> {
  return apiFetch(`/articulos/${articuloId}/detalle`)
}

export function buscarArticulos(term: string, soloActivos = true): Promise<ArticuloBuscarItem[]> {
  const q = new URLSearchParams({ term, soloActivos: String(soloActivos) })
  return apiFetch(`/articulos/buscar?${q}`)
}

export function guardarArticulo(body: {
  articuloId: number
  modeloId: number
  tipoArticuloId: number
  codArticulo: string
  denominacion: string
  descripcion?: string | null
  monto: number
  descuento: number
  indPerecible: boolean
  indImportado: boolean
  indCanjeable: boolean
  estado: boolean
}): Promise<MaestroOperacionResponse> {
  return postJson('/articulos/guardar', body)
}

export function fetchArticuloImagenes(articuloId: number): Promise<{ archivos: string[] }> {
  return apiFetch(`/articulos/${articuloId}/imagenes`)
}

/** Carga imagen autenticada como object URL (revocar con URL.revokeObjectURL). */
export async function fetchArticuloImagenBlobUrl(
  articuloId: number,
  nombreArchivo: string,
): Promise<string> {
  const token = getAccessToken()
  const headers: HeadersInit = { Accept: 'image/*' }
  if (token) headers.Authorization = `Bearer ${token}`
  const res = await fetch(
    `${baseUrl}/articulos/${articuloId}/imagen/${encodeURIComponent(nombreArchivo)}`,
    { headers },
  )
  if (!res.ok) throw new Error('No se pudo cargar la imagen')
  const blob = await res.blob()
  return URL.createObjectURL(blob)
}

export async function subirImagenArticulo(
  articuloId: number,
  file: File,
): Promise<MaestroOperacionResponse> {
  const form = new FormData()
  form.append('archivo', file)
  return apiFetch(`/articulos/${articuloId}/imagen`, { method: 'POST', body: form })
}

export function eliminarImagenArticulo(
  articuloId: number,
  nombreArchivo: string,
): Promise<MaestroOperacionResponse> {
  const q = new URLSearchParams({ nombreArchivo })
  return apiFetch(`/articulos/${articuloId}/eliminar-imagen?${q}`, { method: 'POST' })
}
