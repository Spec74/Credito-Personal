import { importLibrary, setOptions } from '@googlemaps/js-api-loader'
import { getGoogleMapsApiKey } from '../config/googleMaps'

let loadPromise: Promise<void> | null = null

/** Carga Maps + Places una sola vez (API funcional @googlemaps/js-api-loader v2). */
export function loadGoogleMapsApi(): Promise<void> {
  const apiKey = getGoogleMapsApiKey()
  if (!apiKey) {
    return Promise.reject(
      new Error(
        'Configure VITE_GOOGLE_MAPS_API_KEY en .env.development.local o variables de build.',
      ),
    )
  }

  if (!loadPromise) {
    setOptions({ key: apiKey, v: 'weekly' })
    loadPromise = Promise.all([
      importLibrary('maps'),
      importLibrary('places'),
    ]).then(() => undefined)
  }

  return loadPromise
}
