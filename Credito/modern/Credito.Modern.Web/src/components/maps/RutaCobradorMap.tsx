import { useEffect, useRef, useState } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { DEFAULT_MAP_CENTER } from '../../config/googleMaps'
import { formatMoney } from '../../utils/formatMoney'
import './ruta-cobrador-map.css'

export type RutaMapParada = {
  orden: number
  creditoId: number
  cliente: string
  montoCobrar: number
  direccion?: string | null
  latitud?: number | null
  longitud?: number | null
  tieneGps: boolean
}

type Props = {
  paradas: RutaMapParada[]
  origen?: { lat: number; lng: number } | null
  height?: number | string
}

function markerHtml(orden: number): string {
  return `<span class="ruta-cobrador-pin">${orden}</span>`
}

/** Intenta geometría vial OSRM; si falla, polyline recta entre paradas. */
async function fetchOsrmLatLngs(
  points: { lat: number; lng: number }[],
): Promise<[number, number][] | null> {
  if (points.length < 2) return null
  const coords = points.map((p) => `${p.lng},${p.lat}`).join(';')
  const url = `https://router.project-osrm.org/route/v1/driving/${coords}?overview=full&geometries=geojson`
  try {
    const res = await fetch(url)
    if (!res.ok) return null
    const data = (await res.json()) as {
      code?: string
      routes?: { geometry?: { coordinates?: [number, number][] } }[]
    }
    if (data.code !== 'Ok' || !data.routes?.[0]?.geometry?.coordinates?.length) return null
    return data.routes[0].geometry.coordinates.map(([lng, lat]) => [lat, lng])
  } catch {
    return null
  }
}

/**
 * Mapa de ruta del cobrador (OpenStreetMap + Leaflet).
 * Marcadores numerados con nombre y monto; polyline de la secuencia.
 */
export function RutaCobradorMap({ paradas, origen, height = 420 }: Props) {
  const containerRef = useRef<HTMLDivElement>(null)
  const mapRef = useRef<L.Map | null>(null)
  const [hint, setHint] = useState<string | null>(null)

  useEffect(() => {
    const el = containerRef.current
    if (!el) return

    const map = L.map(el, { zoomControl: true, attributionControl: true })
    mapRef.current = map
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    }).addTo(map)

    const bounds = L.latLngBounds([])
    const gpsPoints: { lat: number; lng: number }[] = []

    if (origen && origen.lat !== 0 && origen.lng !== 0) {
      const origenIcon = L.divIcon({
        className: 'ruta-cobrador-pin-wrap',
        html: '<span class="ruta-cobrador-pin ruta-cobrador-pin--origen">O</span>',
        iconSize: [32, 32],
        iconAnchor: [16, 16],
      })
      L.marker([origen.lat, origen.lng], { icon: origenIcon })
        .addTo(map)
        .bindPopup('<strong>Inicio (oficina)</strong>')
      bounds.extend([origen.lat, origen.lng])
      gpsPoints.push(origen)
    }

    for (const p of paradas) {
      if (!p.tieneGps || p.latitud == null || p.longitud == null) continue
      const lat = Number(p.latitud)
      const lng = Number(p.longitud)
      if (!Number.isFinite(lat) || !Number.isFinite(lng) || lat === 0 || lng === 0) continue

      const icon = L.divIcon({
        className: 'ruta-cobrador-pin-wrap',
        html: markerHtml(p.orden),
        iconSize: [32, 32],
        iconAnchor: [16, 16],
      })
      const popup = [
        `<strong>${p.orden}. ${escapeHtml(p.cliente)}</strong>`,
        `<div>Cobrar: <strong>${escapeHtml(formatMoney(p.montoCobrar))}</strong></div>`,
        p.direccion
          ? `<div class="ruta-cobrador-popup-dir">${escapeHtml(p.direccion)}</div>`
          : '',
      ].join('')
      L.marker([lat, lng], { icon }).addTo(map).bindPopup(popup)
      bounds.extend([lat, lng])
      gpsPoints.push({ lat, lng })
    }

    let cancelled = false
    ;(async () => {
      if (gpsPoints.length < 2) {
        if (bounds.isValid()) map.fitBounds(bounds.pad(0.2))
        else map.setView([DEFAULT_MAP_CENTER.lat, DEFAULT_MAP_CENTER.lng], 13)
        setHint(
          gpsPoints.length === 0
            ? 'Ningún cliente seleccionado tiene GPS. Complete lat/lng en la ficha del cliente.'
            : null,
        )
        return
      }

      const road = await fetchOsrmLatLngs(gpsPoints)
      if (cancelled) return
      if (road && road.length > 1) {
        L.polyline(road, { color: '#114885', weight: 5, opacity: 0.85 }).addTo(map)
        setHint(null)
      } else {
        L.polyline(
          gpsPoints.map((p) => [p.lat, p.lng] as [number, number]),
          { color: '#114885', weight: 4, opacity: 0.7, dashArray: '8 6' },
        ).addTo(map)
        setHint('Ruta aproximada (línea entre paradas). Use «Navegar» para GPS vial en el celular.')
      }
      if (bounds.isValid()) map.fitBounds(bounds.pad(0.18))
      map.invalidateSize()
    })()

    const t = window.setTimeout(() => map.invalidateSize(), 120)
    return () => {
      cancelled = true
      window.clearTimeout(t)
      map.remove()
      mapRef.current = null
    }
  }, [paradas, origen])

  return (
    <div className="ruta-cobrador-map">
      <div ref={containerRef} className="ruta-cobrador-map__canvas" style={{ height }} />
      {hint ? <p className="ruta-cobrador-map__hint">{hint}</p> : null}
    </div>
  )
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}
