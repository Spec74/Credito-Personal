import type { MapLatLng } from '../config/googleMaps'

export type DeviceGeolocationErrorCode =
  | 'unsupported'
  | 'denied'
  | 'unavailable'
  | 'timeout'
  | 'unknown'

export class DeviceGeolocationError extends Error {
  readonly code: DeviceGeolocationErrorCode

  constructor(code: DeviceGeolocationErrorCode, message: string) {
    super(message)
    this.name = 'DeviceGeolocationError'
    this.code = code
  }
}

export type DevicePosition = MapLatLng & {
  /** Precisión estimada del GPS en metros (si el dispositivo la reporta). */
  accuracyMeters: number | null
}

/**
 * Lee la ubicación actual del dispositivo (pide permiso GPS al usuario).
 * Requiere HTTPS o localhost.
 */
export function getCurrentDevicePosition(options?: {
  timeoutMs?: number
  maximumAgeMs?: number
  enableHighAccuracy?: boolean
}): Promise<DevicePosition> {
  if (typeof navigator === 'undefined' || !navigator.geolocation) {
    return Promise.reject(
      new DeviceGeolocationError(
        'unsupported',
        'Este dispositivo o navegador no admite GPS.',
      ),
    )
  }

  const timeoutMs = options?.timeoutMs ?? 20_000
  const maximumAgeMs = options?.maximumAgeMs ?? 5_000
  const enableHighAccuracy = options?.enableHighAccuracy ?? true

  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const accuracy = Number(pos.coords.accuracy)
        resolve({
          lat: pos.coords.latitude,
          lng: pos.coords.longitude,
          accuracyMeters: Number.isFinite(accuracy) ? accuracy : null,
        })
      },
      (err) => {
        if (err.code === err.PERMISSION_DENIED) {
          reject(
            new DeviceGeolocationError(
              'denied',
              'Active el permiso de ubicación en el navegador o del celular para registrar el GPS.',
            ),
          )
          return
        }
        if (err.code === err.POSITION_UNAVAILABLE) {
          reject(
            new DeviceGeolocationError(
              'unavailable',
              'No se pudo obtener el GPS. Verifique que la ubicación del celular esté activada.',
            ),
          )
          return
        }
        if (err.code === err.TIMEOUT) {
          reject(
            new DeviceGeolocationError(
              'timeout',
              'Tiempo agotado al leer el GPS. Intente de nuevo al aire libre.',
            ),
          )
          return
        }
        reject(
          new DeviceGeolocationError(
            'unknown',
            err.message || 'No se pudo leer la ubicación actual.',
          ),
        )
      },
      {
        enableHighAccuracy,
        timeout: timeoutMs,
        maximumAge: maximumAgeMs,
      },
    )
  })
}
