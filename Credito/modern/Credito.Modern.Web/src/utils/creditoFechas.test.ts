import { describe, expect, it } from 'vitest'
import {
  codigoFormaPago,
  fechaPrimerPagoPorModalidad,
  fechaVencimientoPorModalidad,
} from './creditoFechas'

const hoy = new Date(2026, 9, 29, 18, 30, 0)

describe('codigoFormaPago', () => {
  it('normaliza letra y etiqueta', () => {
    expect(codigoFormaPago('Q')).toBe('Q')
    expect(codigoFormaPago('quincenal')).toBe('Q')
    expect(codigoFormaPago('Semanal')).toBe('S')
    expect(codigoFormaPago('DIARIO')).toBe('D')
    expect(codigoFormaPago('M')).toBe('M')
  })
})

describe('fechaPrimerPagoPorModalidad', () => {
  it('recalcula el 1.er pago según modalidad, anclado a hoy', () => {
    expect(fechaPrimerPagoPorModalidad('D', false, hoy)).toBe('2026-10-30')
    expect(fechaPrimerPagoPorModalidad('S', false, hoy)).toBe('2026-11-05')
    expect(fechaPrimerPagoPorModalidad('Q', false, hoy)).toBe('2026-11-13')
    expect(fechaPrimerPagoPorModalidad('Quincenal', false, hoy)).toBe('2026-11-13')
    expect(fechaPrimerPagoPorModalidad('M', false, hoy)).toBe('2026-11-29')
    expect(fechaPrimerPagoPorModalidad('D', true, hoy)).toBe('2026-11-29')
  })
})

describe('fechaVencimientoPorModalidad', () => {
  it('con 1 cuota coincide con el primer pago', () => {
    expect(fechaVencimientoPorModalidad('Q', 1, '2026-11-13')).toBe('2026-11-13')
    expect(fechaVencimientoPorModalidad('S', 1, '2026-11-05')).toBe('2026-11-05')
  })

  it('suma periodos de 15/7/30 en el resto de cuotas', () => {
    expect(fechaVencimientoPorModalidad('Q', 2, '2026-11-13')).toBe('2026-11-28')
    expect(fechaVencimientoPorModalidad('S', 4, '2026-11-05')).toBe('2026-11-26')
    expect(fechaVencimientoPorModalidad('M', 12, '2026-11-29')).toBe('2027-10-29')
    expect(fechaVencimientoPorModalidad('D', 3, '2026-10-30')).toBe('2026-11-01')
  })
})
