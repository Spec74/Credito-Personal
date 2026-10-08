import { importLibrary, setOptions } from '@googlemaps/js-api-loader'
import { getGoogleMapsApiKey } from '../config/googleMaps'

let loadPromise: Promise<void> | null = null

/**
 * Carga Maps + Places + Geocoding una sola vez (@googlemaps/js-api-loader v2).
 * Tras éxito, las APIs quedan en `google.maps` (Map, Marker, Autocomplete, Geocoder).
 */
export function loadGoogleMapsApi(): Promise<void> {
  const apiKey = getGoogleMapsApiKey()
  if (!apiKey) {
    return Promise.reject(
      new Error(
        'Configure VITE_GOOGLE_MAPS_API_KEY en .env / Vercel (tipo Config, no Secret) y restrinja la clave al dominio.',
      ),
    )
  }

  if (!loadPromise) {
    setOptions({
      key: apiKey,
      v: 'weekly',
      // Región/idioma PE para Places y geocoder.
      language: 'es',
      region: 'PE',
    })
    loadPromise = Promise.all([
      importLibrary('maps'),
      importLibrary('places'),
      importLibrary('geocoding'),
    ])
      .then(() => undefined)
      .catch((err) => {
        // Permitir reintento si falló (CSP, red, clave inválida).
        loadPromise = null
        throw err
      })
  }

  return loadPromise
}
