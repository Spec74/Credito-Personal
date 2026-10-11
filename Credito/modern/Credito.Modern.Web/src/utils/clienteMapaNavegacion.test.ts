import { describe, expect, it } from 'vitest'
import {
  clienteMapaHref,
  clienteMapaLabel,
  clienteTieneGps,
} from './clienteMapaNavegacion'

describe('clienteMapaNavegacion', () => {
  it('prioriza GPS exacto y muestra dirección como etiqueta', () => {
    const punto = {
      direccion: 'Jr. Lima 245',
      latitud: -13.16,
      longitud: -74.22,
    }
    expect(clienteTieneGps(punto)).toBe(true)
    expect(clienteMapaLabel(punto)).toBe('Jr. Lima 245')
    expect(clienteMapaHref(punto)).toBe('https://www.google.com/maps?q=-13.16,-74.22')
  })

  it('sin GPS usa búsqueda por dirección', () => {
    const punto = { direccion: 'Av. Centenario', latitud: null, longitud: null }
    expect(clienteTieneGps(punto)).toBe(false)
    expect(clienteMapaHref(punto)).toContain('maps/search')
    expect(clienteMapaHref(punto)).toContain(encodeURIComponent('Av. Centenario'))
  })

  it('ignora 0,0 y cae a búsqueda por dirección', () => {
    const punto = { direccion: 'Jr. Lima', latitud: 0, longitud: 0 }
    expect(clienteTieneGps(punto)).toBe(false)
    expect(clienteMapaHref(punto)).toContain('maps/search')
  })

  it('sin dirección y con GPS usa etiqueta de respaldo', () => {
    expect(clienteMapaLabel({ latitud: -13.16, longitud: -74.22 })).toBe(
      'Ver ubicación GPS',
    )
  })
})
