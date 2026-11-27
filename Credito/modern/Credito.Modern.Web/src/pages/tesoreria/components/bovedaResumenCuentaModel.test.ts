import { describe, expect, it } from 'vitest'
import type { BovedaMedioMarca } from '../../../api/boveda'
import {
  buildBovedaMedioCards,
  filterBovedaMedioCards,
  etiquetaBilletera,
} from './bovedaResumenCuentaModel'

function marca(
  tipoPagoId: number,
  total: number,
  opts?: Partial<BovedaMedioMarca>,
): BovedaMedioMarca {
  return {
    tipoPagoId,
    marca: opts?.marca ?? `Tipo ${tipoPagoId}`,
    variant: opts?.variant ?? 'otro',
    total,
    sedes: opts?.sedes ?? [],
  }
}

describe('buildBovedaMedioCards', () => {
  it('sucursal: lista plana con enCaja por tipo', () => {
    const cards = buildBovedaMedioCards(
      [marca(1, 100, { variant: 'efectivo' }), marca(2, 50, { variant: 'yape' })],
      [marca(2, 20)],
      false,
    )
    expect(cards).toHaveLength(2)
    expect(cards[0]?.marca.tipoPagoId).toBe(1)
    expect(cards[1]?.enCaja).toBe(20)
    expect(cards[1]?.hijos).toEqual([])
  })

  it('central: agrupa Gerente/René bajo Yape e Interbank', () => {
    const cards = buildBovedaMedioCards(
      [
        marca(1, 80, { variant: 'efectivo' }),
        marca(10, 30, { variant: 'yape', marca: 'Yape Gerente' }),
        marca(11, 20, { variant: 'yape', marca: 'Yape René' }),
        marca(12, 15, { variant: 'interbank' }),
        marca(4, 5, { variant: 'bcp' }),
      ],
      [marca(10, 7), marca(1, 3)],
      true,
    )
    const yape = cards.find((c) => c.marca.tipoPagoId === 2)
    const ibk = cards.find((c) => c.marca.tipoPagoId === 3)
    const efectivo = cards.find((c) => c.marca.tipoPagoId === 1)
    const bcp = cards.find((c) => c.marca.tipoPagoId === 4)

    expect(yape?.hijos.map((h) => h.tipoPagoId)).toEqual([10, 11])
    expect(yape?.marca.total).toBe(50)
    expect(yape?.enCaja).toBe(7)
    expect(ibk?.hijos.map((h) => h.tipoPagoId)).toEqual([12])
    expect(efectivo?.enCaja).toBe(3)
    expect(bcp?.hijos).toEqual([])
    expect(cards.some((c) => c.marca.tipoPagoId === 10)).toBe(false)
  })

  it('central: usa padre API si existe (no re-suma ledger)', () => {
    const cards = buildBovedaMedioCards(
      [
        marca(2, 100, { variant: 'yape', marca: 'Yape' }),
        marca(10, 40, { variant: 'yape' }),
        marca(11, 60, { variant: 'yape' }),
      ],
      null,
      true,
    )
    expect(cards).toHaveLength(1)
    expect(cards[0]?.marca.total).toBe(100)
    expect(cards[0]?.hijos).toHaveLength(2)
  })
})

describe('filterBovedaMedioCards', () => {
  it('oculta ceros salvo efectivo y pendientes en caja', () => {
    const cards = buildBovedaMedioCards(
      [
        marca(1, 0, { variant: 'efectivo' }),
        marca(2, 0, { variant: 'yape' }),
        marca(9, 0, { variant: 'plin' }),
      ],
      [marca(9, 12)],
      false,
    )
    const visible = filterBovedaMedioCards(cards, false)
    expect(visible.map((c) => c.marca.tipoPagoId).sort()).toEqual([1, 9])
  })
})

describe('etiquetaBilletera', () => {
  it('nombra Gerente/René', () => {
    expect(etiquetaBilletera(10)).toBe('Gerente')
    expect(etiquetaBilletera(11)).toBe('René')
    expect(etiquetaBilletera(2)).toBeNull()
  })
})
