import { hasValidCoordinates } from '../config/googleMaps'

export type ClienteMapaPunto = {
  direccion?: string | null
  latitud?: number | null
  longitud?: number | null
}

/** True si el cliente tiene GPS usable (no 0,0). */
export function clienteTieneGps(punto: ClienteMapaPunto): boolean {
  return hasValidCoordinates(punto.latitud, punto.longitud)
}

/**
 * Enlace profesional a mapas:
 * - Con GPS → coordenadas exactas (Google Maps).
 * - Sin GPS → búsqueda por dirección textual.
 * La etiqueta visible debe seguir siendo la dirección (ver `clienteMapaLabel`).
 */
export function clienteMapaHref(punto: ClienteMapaPunto): string | null {
  if (clienteTieneGps(punto)) {
    return `https://www.google.com/maps?q=${Number(punto.latitud)},${Number(punto.longitud)}`
  }
  const dir = punto.direccion?.trim()
  if (dir) {
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(dir)}`
  }
  return null
}

/** Texto del enlace: siempre la dirección legible; fallback si solo hay GPS. */
export function clienteMapaLabel(punto: ClienteMapaPunto): string {
  const dir = punto.direccion?.trim()
  if (dir) return dir
  return clienteTieneGps(punto) ? 'Ver ubicación GPS' : ''
}

export function clienteMapaTitle(punto: ClienteMapaPunto): string {
  if (clienteTieneGps(punto)) {
    return 'Abrir ubicación GPS exacta en el mapa'
  }
  if (punto.direccion?.trim()) {
    return 'Buscar esta dirección en el mapa (sin GPS registrado)'
  }
  return 'Sin ubicación'
}
