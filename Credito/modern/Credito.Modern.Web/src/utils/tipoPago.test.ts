import { describe, expect, it } from 'vitest'
import {
  TIPO_PAGO_EFECTIVO,
  esTipoPagoCobroPermitido,
  esTipoPagoDigital,
  esTipoPagoSedeObsoleto,
} from './tipoPago'

describe('tipoPago', () => {
  it('efectivo no es digital; Yape/Interbank/BCP/Nación sí', () => {
    expect(esTipoPagoDigital(TIPO_PAGO_EFECTIVO)).toBe(false)
    expect(esTipoPagoDigital(2)).toBe(true) // Yape
    expect(esTipoPagoDigital(3)).toBe(true) // Interbank
    expect(esTipoPagoDigital(4)).toBe(true) // BCP
    expect(esTipoPagoDigital(5)).toBe(true) // Banco de la Nación
  })

  it('marca 6–8 como sede obsoleta y no permitida en cobro', () => {
    expect(esTipoPagoSedeObsoleto(6)).toBe(true)
    expect(esTipoPagoCobroPermitido(6)).toBe(false)
    expect(esTipoPagoCobroPermitido(4)).toBe(true)
    expect(esTipoPagoCobroPermitido(1)).toBe(true)
  })
})
