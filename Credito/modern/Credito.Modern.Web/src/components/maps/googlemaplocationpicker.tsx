import { useCallback, useEffect, useRef, useState } from 'react'
import { Alert, Input, Spin, type InputRef } from 'antd'
import {
  DEFAULT_MAP_CENTER,
  formatCoordinates,
  isGoogleMapsConfigured,
  type MapLatLng,
} from '../../config/googleMaps'
import { loadGoogleMapsApi } from '../../hooks/useGoogleMapsLoader'
import './google-map-location-picker.css'

export type GoogleMapLocationPickerProps = {
  value?: MapLatLng | null
  onChange?: (value: MapLatLng | null) => void
  /** Cambia al abrir/cerrar modal para forzar contenedor DOM limpio. */
  layoutKey?: string | number
  disabled?: boolean
  height?: number
  searchPlaceholder?: string
  hintText?: string
}

function samePosition(a: MapLatLng | null | undefined, b: MapLatLng | null | undefined): boolean {
  if (!a && !b) return true
  if (!a || !b) return false
  return Math.abs(a.lat - b.lat) < 1e-8 && Math.abs(a.lng - b.lng) < 1e-8
}

function getGoogleMaps(): typeof google.maps | null {
  return typeof google !== 'undefined' ? google.maps : null
}

function disposeMap(
  map: google.maps.Map | null,
  marker: google.maps.Marker | null,
  autocomplete: google.maps.places.Autocomplete | null,
) {
  const maps = getGoogleMaps()
  if (maps?.event) {
    if (map) maps.event.clearInstanceListeners(map)
    if (marker) maps.event.clearInstanceListeners(marker)
    if (autocomplete) maps.event.clearInstanceListeners(autocomplete)
  }
  marker?.setMap(null)
}

export function GoogleMapLocationPicker({
  value,
  onChange,
  layoutKey = 'default',
  disabled = false,
  height = 260,
  searchPlaceholder = 'Buscar dirección en Google Maps…',
  hintText = 'Busque una dirección o arrastre el marcador para fijar la ubicación.',
}: GoogleMapLocationPickerProps) {
  const mapDivRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<InputRef>(null)
  const mapRef = useRef<google.maps.Map | null>(null)
  const markerRef = useRef<google.maps.Marker | null>(null)
  const autocompleteRef = useRef<google.maps.places.Autocomplete | null>(null)
  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [ready, setReady] = useState(false)

  const applyPosition = useCallback((pos: MapLatLng, pan = true) => {
    const map = mapRef.current
    const marker = markerRef.current
    if (!map || !marker) return
    marker.setPosition(pos)
    if (pan) {
      map.panTo(pos)
      const z = map.getZoom()
      if (z == null || z < 14) map.setZoom(16)
    }
    onChangeRef.current?.(pos)
  }, [])

  useEffect(() => {
    if (!isGoogleMapsConfigured()) {
      setError('Falta VITE_GOOGLE_MAPS_API_KEY en el entorno de la SPA.')
      setLoading(false)
      return
    }

    let cancelled = false
    let retryTimer: ReturnType<typeof setTimeout> | undefined

    const initMap = () => {
      if (cancelled) return

      const maps = getGoogleMaps()
      const inputEl = searchRef.current?.input
      const mapEl = mapDivRef.current

      if (!maps || !mapEl || !inputEl) {
        retryTimer = setTimeout(initMap, 80)
        return
      }

      try {
        const initial = value ?? DEFAULT_MAP_CENTER
        const map = new maps.Map(mapEl, {
          center: initial,
          zoom: value ? 16 : 14,
          mapTypeControl: false,
          streetViewControl: false,
          fullscreenControl: !disabled,
          gestureHandling: disabled ? 'none' : 'auto',
        })
        mapRef.current = map

        const marker = new maps.Marker({
          map,
          position: initial,
          draggable: !disabled,
        })
        markerRef.current = marker

        marker.addListener('dragend', () => {
          const p = marker.getPosition()
          if (!p) return
          onChangeRef.current?.({ lat: p.lat(), lng: p.lng() })
        })

        map.addListener('click', (e: google.maps.MapMouseEvent) => {
          if (disabled || !e.latLng) return
          const pos = { lat: e.latLng.lat(), lng: e.latLng.lng() }
          marker.setPosition(pos)
          onChangeRef.current?.(pos)
        })

        if (maps.places?.Autocomplete) {
          const autocomplete = new maps.places.Autocomplete(inputEl, {
            fields: ['geometry', 'formatted_address', 'name'],
          })
          autocompleteRef.current = autocomplete
          autocomplete.addListener('place_changed', () => {
            const place = autocomplete.getPlace()
            const loc = place.geometry?.location
            if (!loc) return
            applyPosition({ lat: loc.lat(), lng: loc.lng() })
          })
        }

        setReady(true)
        setLoading(false)
      } catch (err: unknown) {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'No se pudo inicializar el mapa')
        setLoading(false)
      }
    }

    setLoading(true)
    setError(null)
    setReady(false)

    void loadGoogleMapsApi()
      .then(() => {
        if (!cancelled) initMap()
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'No se pudo cargar Google Maps')
        setLoading(false)
      })

    return () => {
      cancelled = true
      if (retryTimer) clearTimeout(retryTimer)
      disposeMap(mapRef.current, markerRef.current, autocompleteRef.current)
      autocompleteRef.current = null
      markerRef.current = null
      mapRef.current = null
      setReady(false)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [layoutKey])

  useEffect(() => {
    if (!ready || !value || !markerRef.current) return
    const marker = markerRef.current
    const current = marker.getPosition()
    const currentPos =
      current != null ? { lat: current.lat(), lng: current.lng() } : null
    if (!samePosition(currentPos, value)) {
      marker.setPosition(value)
      mapRef.current?.panTo(value)
    }
  }, [value, ready])

  useEffect(() => {
    if (!ready || !mapRef.current) return
    const maps = getGoogleMaps()
    if (!maps?.event) return

    const id = window.setTimeout(() => {
      maps.event.trigger(mapRef.current!, 'resize')
      if (value) mapRef.current?.panTo(value)
      else mapRef.current?.panTo(DEFAULT_MAP_CENTER)
    }, 250)
    return () => window.clearTimeout(id)
  }, [layoutKey, ready, value])

  useEffect(() => {
    markerRef.current?.setDraggable(!disabled)
  }, [disabled, ready])

  if (error) {
    return <Alert type="warning" showIcon title={error} />
  }

  const mapHostKey = `gmaps-host-${layoutKey}`

  return (
    <div className="gmaps-picker">
      <Input
        ref={searchRef}
        className="gmaps-picker__search"
          placeholder={searchPlaceholder}
        disabled={disabled || loading}
        allowClear
      />
      <div className="gmaps-picker__map-wrap" style={{ height }}>
        {loading ? (
          <div className="gmaps-picker__overlay" aria-hidden>
            <Spin size="large" />
          </div>
        ) : null}
        {/* Contenedor vacío: Google Maps toma control del DOM; React no debe renderizar hijos aquí. */}
        <div key={mapHostKey} ref={mapDivRef} className="gmaps-picker__map-host" />
      </div>
      <p className="gmaps-picker__hint">{hintText}</p>
      {value ? (
        <span className="gmaps-picker__coords">{formatCoordinates(value.lat, value.lng)}</span>
      ) : (
        <span className="gmaps-picker__coords">Sin ubicación seleccionada</span>
      )}
    </div>
  )
}
