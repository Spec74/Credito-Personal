import { describe, expect, it } from 'vitest'
import {
  TIPO_PAGO_EFECTIVO,
  TIPO_PAGO_YAPE,
  TIPO_PAGO_YAPE_GERENTE,
  TIPO_PAGO_YAPE_RENE,
  esTipoPagoCobroPermitido,
  esTipoPagoDigital,
  esTipoPagoRollup,
  esTipoPagoSedeObsoleto,
  esTipoPagoYapeFamilia,
} from './tipoPago'

describe('tipoPago', () => {
  it('efectivo no es digital; Yape familia / Plin / bancos sí', () => {
    expect(esTipoPagoDigital(TIPO_PAGO_EFECTIVO)).toBe(false)
    expect(esTipoPagoDigital(TIPO_PAGO_YAPE_GERENTE)).toBe(true)
    expect(esTipoPagoDigital(TIPO_PAGO_YAPE_RENE)).toBe(true)
    expect(esTipoPagoDigital(9)).toBe(true)
  })

  it('Yape padre es rollup; Gerente/René son postables y familia', () => {
    expect(esTipoPagoRollup(TIPO_PAGO_YAPE)).toBe(true)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_YAPE)).toBe(false)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_YAPE_GERENTE)).toBe(true)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_YAPE_RENE)).toBe(true)
    expect(esTipoPagoYapeFamilia(TIPO_PAGO_YAPE)).toBe(true)
    expect(esTipoPagoYapeFamilia(TIPO_PAGO_YAPE_GERENTE)).toBe(true)
    expect(esTipoPagoYapeFamilia(TIPO_PAGO_YAPE_RENE)).toBe(true)
  })

  it('marca 6–8 como sede obsoleta y no permitida en cobro', () => {
    expect(esTipoPagoSedeObsoleto(6)).toBe(true)
    expect(esTipoPagoCobroPermitido(6)).toBe(false)
    expect(esTipoPagoCobroPermitido(4)).toBe(true)
    expect(esTipoPagoCobroPermitido(1)).toBe(true)
  })
})
