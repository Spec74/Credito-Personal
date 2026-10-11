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

/**
 * Lee la ubicación actual del dispositivo (pide permiso GPS al usuario).
 * Usar con HTTPS / localhost.
 */
export function getCurrentDevicePosition(options?: {
  timeoutMs?: number
  maximumAgeMs?: number
  enableHighAccuracy?: boolean
}): Promise<MapLatLng> {
  if (typeof navigator === 'undefined' || !navigator.geolocation) {
    return Promise.reject(
      new DeviceGeolocationError(
        'unsupported',
        'Este dispositivo o navegador no admite GPS.',
      ),
    )
  }

  const timeoutMs = options?.timeoutMs ?? 20_000
  const maximumAgeMs = options?.maximumAgeMs ?? 10_000
  const enableHighAccuracy = options?.enableHighAccuracy ?? true

  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        resolve({
          lat: pos.coords.latitude,
          lng: pos.coords.longitude,
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
