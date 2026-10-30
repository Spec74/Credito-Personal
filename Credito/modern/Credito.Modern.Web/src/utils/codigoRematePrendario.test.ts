import { describe, expect, it } from 'vitest'
import { esCodigoRematePrendario } from './codigoRematePrendario'

describe('esCodigoRematePrendario', () => {
  it('reconoce PRM + id de prenda', () => {
    expect(esCodigoRematePrendario('PRM1842')).toBe(true)
    expect(esCodigoRematePrendario('PRM2')).toBe(true)
    expect(esCodigoRematePrendario('prm12')).toBe(true)
  })

  it('rechaza catálogo y vacíos', () => {
    expect(esCodigoRematePrendario('E2E CELULAR')).toBe(false)
    expect(esCodigoRematePrendario('PRM')).toBe(false)
    expect(esCodigoRematePrendario('')).toBe(false)
    expect(esCodigoRematePrendario(null)).toBe(false)
  })
})
