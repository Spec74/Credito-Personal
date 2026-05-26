import { apiFetch } from './client'

export interface MaestroOperacionResponse {
  success: boolean
  id: number | null
  mensaje: string | null
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface MarcaGestionRow {
  marcaId: number
  denominacion: string | null
  estado: boolean
}

export interface ModeloGestionRow {
  modeloId: number
  denominacion: string
  marcaId: number | null
  marcaDenominacion: string | null
  estado: boolean
}

export interface TipoArticuloGestionRow {
  tipoArticuloId: number
  denominacion: string | null
  descripcion: string | null
  indTieneCodigo: boolean
  estado: boolean
  indMovimientoAlmacen: boolean
}

export interface OficinaGestionRow {
  oficinaId: number
  denominacion: string | null
  descripcion: string | null
  telefono: string | null
  usuarioAsignadoId: number
  indPrincipal: boolean
  estado: boolean
  latitud?: number | null
  longitud?: number | null
}

export interface AlmacenGestionRow {
  almacenId: number
  oficinaId: number
  oficinaDenominacion: string | null
  denominacion: string
  descripcion: string | null
  estado: boolean
}

export interface ListaPrecioGestionRow {
  listaPrecioId: number
  articuloId: number
  monto: number
  descuento: number
  estado: boolean
  puntos: number | null
  puntosCanje: number | null
}

export function fetchMarcasGestion(incluirInactivos: boolean): Promise<MarcaGestionRow[]> {
  const q = new URLSearchParams({ incluirInactivos: String(incluirInactivos) })
  return apiFetch(`/marcas/gestion?${q}`)
}

export function guardarMarca(body: {
  marcaId: number
  denominacion: string
  estado: boolean
}): Promise<MaestroOperacionResponse> {
  return postJson('/marcas/guardar', body)
}

export function activarMarca(marcaId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/marcas/${marcaId}/activar`, {})
}

export function fetchModelosGestion(
  incluirInactivos: boolean,
  marcaId?: number,
): Promise<ModeloGestionRow[]> {
  const q = new URLSearchParams({ incluirInactivos: String(incluirInactivos) })
  if (marcaId && marcaId >= 1) q.set('marcaId', String(marcaId))
  return apiFetch(`/modelos/gestion?${q}`)
}

export function guardarModelo(body: {
  modeloId: number
  marcaId: number
  denominacion: string
  estado: boolean
}): Promise<MaestroOperacionResponse> {
  return postJson('/modelos/guardar', body)
}

export function activarModelo(modeloId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/modelos/${modeloId}/activar`, {})
}

export function fetchTiposArticuloGestion(
  incluirInactivos: boolean,
): Promise<TipoArticuloGestionRow[]> {
  const q = new URLSearchParams({ incluirInactivos: String(incluirInactivos) })
  return apiFetch(`/tipos-articulo/gestion?${q}`)
}

export function guardarTipoArticulo(body: {
  tipoArticuloId: number
  denominacion: string
  descripcion?: string | null
  estado: boolean
}): Promise<MaestroOperacionResponse> {
  return postJson('/tipos-articulo/guardar', body)
}

export function activarTipoArticulo(tipoArticuloId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/tipos-articulo/${tipoArticuloId}/activar`, {})
}

export function fetchOficinasGestion(params: {
  incluirInactivos: boolean
  buscar?: string
}): Promise<OficinaGestionRow[]> {
  const q = new URLSearchParams({ incluirInactivos: String(params.incluirInactivos) })
  if (params.buscar?.trim()) q.set('buscar', params.buscar.trim())
  return apiFetch(`/oficinas/gestion?${q}`)
}

export function guardarOficina(body: {
  oficinaId: number
  denominacion: string
  descripcion?: string | null
  telefono?: string | null
  usuarioAsignadoId: number
  estado: boolean
  indPrincipal: boolean
  latitud?: number | null
  longitud?: number | null
}): Promise<MaestroOperacionResponse> {
  return postJson('/oficinas/guardar', body)
}

export function activarOficina(oficinaId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/oficinas/${oficinaId}/activar`, {})
}

export function fetchAlmacenesGestion(
  incluirInactivos: boolean,
  oficinaId?: number,
): Promise<AlmacenGestionRow[]> {
  const q = new URLSearchParams({ incluirInactivos: String(incluirInactivos) })
  if (oficinaId && oficinaId >= 1) q.set('oficinaId', String(oficinaId))
  return apiFetch(`/almacenes/gestion?${q}`)
}

export function guardarAlmacen(body: {
  almacenId: number
  oficinaId: number
  denominacion: string
  descripcion?: string | null
  estado: boolean
}): Promise<MaestroOperacionResponse> {
  return postJson('/almacenes/guardar', body)
}

export function activarAlmacen(almacenId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/almacenes/${almacenId}/activar`, {})
}

export function fetchListaPreciosGestion(
  incluirInactivos: boolean,
  articuloId?: number,
): Promise<ListaPrecioGestionRow[]> {
  const q = new URLSearchParams({ incluirInactivos: String(incluirInactivos) })
  if (articuloId && articuloId >= 1) q.set('articuloId', String(articuloId))
  return apiFetch(`/lista-precios/gestion?${q}`)
}

export function guardarListaPrecio(body: {
  listaPrecioId: number
  articuloId: number
  monto: number
  descuento: number
  puntos?: number | null
  puntosCanje?: number | null
  estado: boolean
}): Promise<MaestroOperacionResponse> {
  return postJson('/lista-precios/guardar', body)
}

export function activarListaPrecio(listaPrecioId: number): Promise<MaestroOperacionResponse> {
  return postJson(`/lista-precios/${listaPrecioId}/activar`, {})
}
