import { loadGoogleMapsApi } from '../hooks/useGoogleMapsLoader'
import type { MapLatLng } from '../config/googleMaps'

/** Centra el mapa en el distrito (paridad autocomplete distrito + geocoder legacy). */
export async function geocodeDistritoCliente(distritoLabel: string): Promise<MapLatLng | null> {
  const distrito = distritoLabel.trim()
  if (!distrito) {
    return null
  }

  await loadGoogleMapsApi()
  const geocoder = new google.maps.Geocoder()
  return geocodeOnce(geocoder, `${distrito}, Ayacucho, Peru`)
}

/** Centra el mapa en el distrito (paridad autocomplete distrito + geocoder legacy). */
/** Geocodifica dirección + distrito (paridad legacy geocoder + Ayacucho, Peru). */
export async function geocodeDomicilioCliente(
  direccion: string,
  distritoLabel: string,
): Promise<MapLatLng | null> {
  const distrito = distritoLabel.trim()
  if (!distrito) {
    return null
  }

  await loadGoogleMapsApi()
  const geocoder = new google.maps.Geocoder()
  const exacta = `${direccion.trim()}, ${distrito}, Ayacucho, Peru`.replace(/^,\s*/, '')
  const general = `${distrito}, Ayacucho, Peru`

  const exact = await geocodeOnce(geocoder, exacta)
  if (exact) {
    return exact
  }

  return geocodeOnce(geocoder, general)
}

function geocodeOnce(
  geocoder: google.maps.Geocoder,
  address: string,
): Promise<MapLatLng | null> {
  return new Promise((resolve) => {
    geocoder.geocode({ address, region: 'PE' }, (results, status) => {
      if (status === 'OK' && results?.[0]?.geometry?.location) {
        const loc = results[0].geometry.location
        resolve({ lat: loc.lat(), lng: loc.lng() })
      } else {
        resolve(null)
      }
    })
  })
}
