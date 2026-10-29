import { describe, expect, it } from 'vitest'
import { fechaPrimerPagoPorModalidad } from './creditoFechas'

describe('fechaPrimerPagoPorModalidad (ISO)', () => {
  it('devuelve YYYY-MM-DD para cada modalidad', () => {
    expect(fechaPrimerPagoPorModalidad('M', true)).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(fechaPrimerPagoPorModalidad('D')).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(fechaPrimerPagoPorModalidad('S')).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(fechaPrimerPagoPorModalidad('Q')).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(fechaPrimerPagoPorModalidad('M')).toMatch(/^\d{4}-\d{2}-\d{2}$/)
  })
})
