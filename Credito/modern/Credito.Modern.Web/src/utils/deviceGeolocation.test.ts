import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  DeviceGeolocationError,
  getCurrentDevicePosition,
} from './deviceGeolocation'

type GeoSuccess = PositionCallback
type GeoError = PositionErrorCallback

function stubGeolocation(impl: {
  success?: GeolocationPosition
  error?: { code: number; message: string }
}) {
  const getCurrentPosition = vi.fn(
    (success: GeoSuccess, error?: GeoError) => {
      if (impl.success) {
        success(impl.success)
        return
      }
      if (impl.error && error) {
        error({
          code: impl.error.code,
          message: impl.error.message,
          PERMISSION_DENIED: 1,
          POSITION_UNAVAILABLE: 2,
          TIMEOUT: 3,
        } as GeolocationPositionError)
      }
    },
  )
  vi.stubGlobal('navigator', { geolocation: { getCurrentPosition } })
  return getCurrentPosition
}

describe('deviceGeolocation', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('resuelve lat/lng y accuracy', async () => {
    stubGeolocation({
      success: {
        coords: {
          latitude: -13.16,
          longitude: -74.22,
          accuracy: 12,
          altitude: null,
          altitudeAccuracy: null,
          heading: null,
          speed: null,
        },
        timestamp: Date.now(),
      } as GeolocationPosition,
    })
    await expect(getCurrentDevicePosition()).resolves.toEqual({
      lat: -13.16,
      lng: -74.22,
      accuracyMeters: 12,
    })
  })

  it('mapea permiso denegado', async () => {
    stubGeolocation({ error: { code: 1, message: 'denied' } })
    await expect(getCurrentDevicePosition()).rejects.toMatchObject({
      name: 'DeviceGeolocationError',
      code: 'denied',
    })
  })

  it('mapea unavailable y timeout', async () => {
    stubGeolocation({ error: { code: 2, message: 'n/a' } })
    await expect(getCurrentDevicePosition()).rejects.toBeInstanceOf(
      DeviceGeolocationError,
    )
    stubGeolocation({ error: { code: 3, message: 'timeout' } })
    await expect(getCurrentDevicePosition()).rejects.toMatchObject({
      code: 'timeout',
    })
  })

  it('falla si no hay geolocation API', async () => {
    vi.stubGlobal('navigator', {})
    await expect(getCurrentDevicePosition()).rejects.toMatchObject({
      code: 'unsupported',
    })
  })

  it('pasa enableHighAccuracy / timeout / maximumAge', async () => {
    const getCurrentPosition = stubGeolocation({
      success: {
        coords: {
          latitude: -13,
          longitude: -74,
          accuracy: Number.NaN,
          altitude: null,
          altitudeAccuracy: null,
          heading: null,
          speed: null,
        },
        timestamp: 1,
      } as GeolocationPosition,
    })
    const pos = await getCurrentDevicePosition({
      timeoutMs: 9000,
      maximumAgeMs: 100,
      enableHighAccuracy: false,
    })
    expect(pos.accuracyMeters).toBeNull()
    expect(getCurrentPosition).toHaveBeenCalledWith(
      expect.any(Function),
      expect.any(Function),
      { enableHighAccuracy: false, timeout: 9000, maximumAge: 100 },
    )
  })
})
