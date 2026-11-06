import { describe, expect, it } from 'vitest'
import {
  TIPO_PAGO_EFECTIVO,
  TIPO_PAGO_INTERBANK,
  TIPO_PAGO_INTERBANK_GERENTE,
  TIPO_PAGO_INTERBANK_RENE,
  TIPO_PAGO_YAPE,
  TIPO_PAGO_YAPE_GERENTE,
  TIPO_PAGO_YAPE_RENE,
  esMarcaTipoPagoCatalogo,
  esTipoPagoBilleteraCentral,
  esTipoPagoCobroPermitido,
  esTipoPagoDigital,
  esTipoPagoInterbankFamilia,
  esTipoPagoSedeObsoleto,
  esTipoPagoYapeFamilia,
  marcaDesdeTipoPagoId,
  medioNombrePorItemId,
} from './tipoPago'

describe('tipoPago multisucursal', () => {
  it('digital: efectivo no; billeteras y bancos sí', () => {
    expect(esTipoPagoDigital(TIPO_PAGO_EFECTIVO)).toBe(false)
    expect(esTipoPagoDigital(TIPO_PAGO_YAPE_GERENTE)).toBe(true)
    expect(esTipoPagoDigital(TIPO_PAGO_INTERBANK_RENE)).toBe(true)
    expect(esTipoPagoDigital(9)).toBe(true)
  })

  it('Central: Gerente/René OK; genéricos 2/3 no', () => {
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_YAPE_GERENTE, true)).toBe(true)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_YAPE_RENE, true)).toBe(true)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_INTERBANK_GERENTE, true)).toBe(true)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_INTERBANK_RENE, true)).toBe(true)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_YAPE, true)).toBe(false)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_INTERBANK, true)).toBe(false)
    expect(esTipoPagoCobroPermitido(9, true)).toBe(true)
  })

  it('Sucursal: genéricos OK; billeteras Central no', () => {
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_YAPE, false)).toBe(true)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_INTERBANK, false)).toBe(true)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_YAPE_GERENTE, false)).toBe(false)
    expect(esTipoPagoCobroPermitido(TIPO_PAGO_INTERBANK_GERENTE, false)).toBe(false)
    expect(esTipoPagoCobroPermitido(9, false)).toBe(true)
  })

  it('familias y flags', () => {
    expect(esTipoPagoYapeFamilia(TIPO_PAGO_YAPE)).toBe(true)
    expect(esTipoPagoYapeFamilia(TIPO_PAGO_YAPE_RENE)).toBe(true)
    expect(esTipoPagoInterbankFamilia(TIPO_PAGO_INTERBANK)).toBe(true)
    expect(esTipoPagoInterbankFamilia(TIPO_PAGO_INTERBANK_RENE)).toBe(true)
    expect(esTipoPagoBilleteraCentral(TIPO_PAGO_YAPE_GERENTE)).toBe(true)
    expect(esTipoPagoBilleteraCentral(TIPO_PAGO_INTERBANK_RENE)).toBe(true)
    expect(esTipoPagoSedeObsoleto(6)).toBe(true)
    expect(esTipoPagoCobroPermitido(6, true)).toBe(false)
  })

  it('catálogo UI: nombres, marcas y filtro de combo', () => {
    expect(medioNombrePorItemId(TIPO_PAGO_YAPE_RENE)).toBe('Yape René')
    expect(medioNombrePorItemId(5)).toBe('Banco de la Nación')
    expect(marcaDesdeTipoPagoId(TIPO_PAGO_INTERBANK_GERENTE)).toBe('interbank')
    expect(
      esMarcaTipoPagoCatalogo(
        { itemId: 10, denominacion: 'YAPE GERENTE', desCorta: 'CENTRAL' },
        true,
      ),
    ).toBe(true)
    expect(
      esMarcaTipoPagoCatalogo(
        { itemId: 2, denominacion: 'YAPE' },
        true,
      ),
    ).toBe(false)
    expect(
      esMarcaTipoPagoCatalogo(
        { itemId: 2, denominacion: 'YAPE HUANTA' },
        false,
      ),
    ).toBe(false)
  })
})
