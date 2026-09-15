/** Centro por defecto (Ayacucho), paridad legacy Oficina/Index. */
export const DEFAULT_MAP_CENTER = { lat: -13.15878, lng: -74.22321 } as const

export type MapLatLng = { lat: number; lng: number }

export function getGoogleMapsApiKey(): string {
  const key = import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string | undefined
  return key?.trim() ?? ''
}

export function isGoogleMapsConfigured(): boolean {
  return getGoogleMapsApiKey().length > 0
}

export function hasValidCoordinates(
  lat: number | null | undefined,
  lng: number | null | undefined,
): boolean {
  return lat != null && lng != null && lat !== 0 && lng !== 0
}

export function toMapLatLng(
  lat: number | null | undefined,
  lng: number | null | undefined,
): MapLatLng | null {
  return hasValidCoordinates(lat, lng) ? { lat: lat!, lng: lng! } : null
}

export function formatCoordinates(lat: number, lng: number): string {
  return `${lat.toFixed(6)}, ${lng.toFixed(6)}`
}
