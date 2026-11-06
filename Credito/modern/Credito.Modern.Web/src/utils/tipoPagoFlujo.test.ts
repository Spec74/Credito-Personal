import { describe, expect, it } from 'vitest'
import { buildTipoPagoOptions } from '../pages/tesoreria/components/TipoPagoBrand'
import {
  esMarcaTipoPago,
  medioNombreMarca,
  medioNombrePorItemId,
} from '../pages/tesoreria/components/bovedaResumenCuentaParse'
import { marcaDesdeTipoPagoId } from '../pages/tesoreria/components/resumencuentabrands'
import {
  esTipoPagoCobroPermitido,
  esTipoPagoBilleteraCentral,
  esTipoPagoGenericoCentral,
} from './tipoPago'

const CATALOGO = [
  { itemId: 1, denominacion: 'EFECTIVO', desCorta: null as string | null },
  { itemId: 2, denominacion: 'YAPE', desCorta: null },
  { itemId: 3, denominacion: 'INTERBANK', desCorta: null },
  { itemId: 4, denominacion: 'BCO CREDITO', desCorta: null },
  { itemId: 5, denominacion: 'BCO NACION', desCorta: null },
  { itemId: 6, denominacion: 'YAPE HUANTA', desCorta: 'OBSOLETO' },
  { itemId: 9, denominacion: 'PLIN', desCorta: null },
  { itemId: 10, denominacion: 'YAPE GERENTE', desCorta: 'CENTRAL' },
  { itemId: 11, denominacion: 'YAPE RENE', desCorta: 'CENTRAL' },
  { itemId: 12, denominacion: 'INTERBANK GERENTE', desCorta: 'CENTRAL' },
  { itemId: 13, denominacion: 'INTERBANK RENE', desCorta: 'CENTRAL' },
]

describe('flujo medios 100% — matriz sede', () => {
  it('Central: tipables Gerente/René + bancos; sin genéricos 2/3', () => {
    const opts = buildTipoPagoOptions(CATALOGO, { esCentral: true })
    expect(opts.map((o) => o.value)).toEqual([1, 10, 11, 12, 13, 4, 5, 9])
    expect(opts.map((o) => o.label)).toEqual([
      'Efectivo',
      'Yape Gerente',
      'Yape René',
      'Interbank Gerente',
      'Interbank René',
      'BCP',
      'Banco de la Nación',
      'Plin',
    ])
    for (const o of opts) {
      expect(esTipoPagoCobroPermitido(o.value, true)).toBe(true)
      expect(marcaDesdeTipoPagoId(o.value)).not.toBeNull()
    }
    expect(esTipoPagoCobroPermitido(2, true)).toBe(false)
    expect(esTipoPagoCobroPermitido(3, true)).toBe(false)
  })

  it('Sucursal: solo genéricos; sin billeteras CENTRAL', () => {
    const opts = buildTipoPagoOptions(CATALOGO, { esCentral: false })
    expect(opts.map((o) => o.value)).toEqual([1, 2, 3, 4, 5, 9])
    expect(opts.map((o) => o.label)).toEqual([
      'Efectivo',
      'Yape',
      'Interbank',
      'BCP',
      'Banco de la Nación',
      'Plin',
    ])
    for (const id of [10, 11, 12, 13]) {
      expect(esMarcaTipoPago(CATALOGO.find((c) => c.itemId === id)!, false)).toBe(false)
      expect(esTipoPagoCobroPermitido(id, false)).toBe(false)
      expect(esTipoPagoBilleteraCentral(id)).toBe(true)
    }
    expect(esTipoPagoGenericoCentral(2)).toBe(true)
    expect(esTipoPagoGenericoCentral(3)).toBe(true)
  })

  it('nombres con tilde no se degradan (chips/caja)', () => {
    expect(medioNombreMarca('Yape René')).toBe('Yape René')
    expect(medioNombreMarca('Interbank René')).toBe('Interbank René')
    expect(medioNombreMarca('Banco de la Nación')).toBe('Banco de la Nación')
    expect(medioNombrePorItemId(11)).toBe('Yape René')
    expect(medioNombrePorItemId(13)).toBe('Interbank René')
    expect(medioNombrePorItemId(5)).toBe('Banco de la Nación')
    expect(marcaDesdeTipoPagoId(5)).toBe('banco-nacion')
    expect(marcaDesdeTipoPagoId(11)).toBe('yape')
    expect(marcaDesdeTipoPagoId(13)).toBe('interbank')
  })

  it('obsoletos 6–8 nunca tipables', () => {
    for (const id of [6, 7, 8]) {
      expect(esTipoPagoCobroPermitido(id, true)).toBe(false)
      expect(esTipoPagoCobroPermitido(id, false)).toBe(false)
      expect(esMarcaTipoPago({ itemId: id, denominacion: 'X', desCorta: 'OBSOLETO' }, true)).toBe(
        false,
      )
    }
  })

  it('remesa a Central: el select usa catálogo Central (billeteras, no genéricos 2/3)', () => {
    const opts = buildTipoPagoOptions(CATALOGO, { esCentral: true })
    expect(opts.map((o) => o.value)).toContain(10)
    expect(opts.map((o) => o.value)).toContain(11)
    expect(opts.map((o) => o.value)).toContain(12)
    expect(opts.map((o) => o.value)).toContain(13)
    expect(opts.map((o) => o.value)).not.toContain(2)
    expect(opts.map((o) => o.value)).not.toContain(3)
  })
})
