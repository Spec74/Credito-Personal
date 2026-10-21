import { describe, expect, it } from 'vitest'
import {
  buildCerrarCajasDiariosBody,
  cierreUsaOficinaSesion,
  coincideSedeReferencia,
  loginSedePath,
  parseLoginOficinaId,
  resolverBovedaDestinoPorSede,
  SEDES_REFERENCIA,
} from './sucursalRed'

describe('cierre cajas B-02 (oficina ≠ 1)', () => {
  it('Pichari y Huanta envían su propia oficina, no la 1', () => {
    const pichari = 4
    const huanta = 3
    expect(buildCerrarCajasDiariosBody(pichari, 0)).toEqual({ oficinaId: pichari, sobrante: 0 })
    expect(buildCerrarCajasDiariosBody(huanta, 12.5)).toEqual({ oficinaId: huanta, sobrante: 12.5 })
    expect(cierreUsaOficinaSesion(pichari, pichari)).toBe(true)
    expect(cierreUsaOficinaSesion(1, pichari)).toBe(false)
  })

  it('rechaza cierre sin oficina de sesión', () => {
    expect(() => buildCerrarCajasDiariosBody(0)).toThrow(/oficina/i)
  })
})

describe('fondeo por sede', () => {
  it('resuelve bóveda abierta a partir de la oficina destino', () => {
    const destinos = [
      { bovedaId: 11, oficinaId: 4, oficina: 'PICHARI', saldoFinal: 800 },
      { bovedaId: 12, oficinaId: 3, oficina: 'HUANTA', saldoFinal: 200 },
    ]
    expect(resolverBovedaDestinoPorSede(destinos, 4)?.bovedaId).toBe(11)
    expect(resolverBovedaDestinoPorSede(destinos, 99)).toBeNull()
  })

  it('reconoce las sedes de referencia', () => {
    expect(SEDES_REFERENCIA.map((s) => s.clave)).toEqual([
      'huamanga',
      'pichari',
      'huanta',
      'andahuaylas',
    ])
    expect(coincideSedeReferencia('Agencia Pichari', 'pichari')).toBe(true)
  })
})

describe('cambio de sede', () => {
  it('arma /login con la oficina destino', () => {
    expect(loginSedePath(4)).toBe('/login?oficinaId=4')
    expect(loginSedePath(0)).toBe('/login')
  })

  it('lee oficinaId del query de login', () => {
    expect(parseLoginOficinaId('?oficinaId=3')).toBe(3)
    expect(parseLoginOficinaId('oficinaId=1&from=/inicio')).toBe(1)
    expect(parseLoginOficinaId('?oficinaId=abc')).toBe(0)
    expect(parseLoginOficinaId('')).toBe(0)
  })
})
