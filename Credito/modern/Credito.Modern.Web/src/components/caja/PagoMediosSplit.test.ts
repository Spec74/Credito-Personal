import { describe, expect, it } from 'vitest'
import {
  defaultPagoMediosSplit,
  sumPagoMedios,
  validatePagoMedios,
} from './PagoMediosSplit'

const opts = [
  { value: 1, label: 'EFECTIVO' },
  { value: 2, label: 'YAPE' },
  { value: 3, label: 'PLIN' },
]

describe('PagoMediosSplit', () => {
  it('arma digital + efectivo sumando el hint', () => {
    const lineas = defaultPagoMediosSplit(opts, 30)
    expect(lineas).toHaveLength(2)
    expect(sumPagoMedios(lineas)).toBe(30)
    expect(validatePagoMedios(lineas)).toBeNull()
  })

  it('exige fecha en medios digitales', () => {
    const lineas = defaultPagoMediosSplit(opts, 20)
    lineas[0] = { ...lineas[0], fechaTransferencia: '' }
    expect(validatePagoMedios(lineas)).toMatch(/digital/i)
  })

  it('exige igualdad al céntimo con el total esperado', () => {
    const lineas = defaultPagoMediosSplit(opts, 30)
    expect(validatePagoMedios(lineas, 30)).toBeNull()
    expect(validatePagoMedios(lineas, 30.01)).toMatch(/céntimo/i)
  })
})
