import { describe, expect, it } from 'vitest'
import {
  collapsePlanillaByCredito,
  expandRowToPlanillaItems,
  validateRowMixtoMoney,
} from './cobroBloquePlanillaExpand'

describe('cobroBloquePlanillaExpand', () => {
  it('expande mixto 10+20 sin perder céntimos', () => {
    const items = expandRowToPlanillaItems(7, {
      montoPagar: 30,
      tipoPagoId: 1,
      fechaHoraTrans: '',
      cuotasSeleccionadas: [],
      medios: [
        { tipoPagoId: 2, importe: 10, fechaHoraTrans: '2026-11-06T10:00' },
        { tipoPagoId: 1, importe: 20, fechaHoraTrans: '' },
      ],
    })
    expect(items).toHaveLength(2)
    expect(items[0].montoPagar).toBe(10)
    expect(items[1].montoPagar).toBe(20)
    expect(validateRowMixtoMoney({
      montoPagar: 30,
      tipoPagoId: 1,
      fechaHoraTrans: '',
      cuotasSeleccionadas: [],
      medios: [
        { tipoPagoId: 2, importe: 10, fechaHoraTrans: '2026-11-06T10:00' },
        { tipoPagoId: 1, importe: 20, fechaHoraTrans: '' },
      ],
    })).toBeNull()
  })

  it('rechaza descuadre de 1 céntimo', () => {
    const err = validateRowMixtoMoney({
      montoPagar: 30,
      tipoPagoId: 1,
      fechaHoraTrans: '',
      cuotasSeleccionadas: [],
      medios: [
        { tipoPagoId: 2, importe: 10.01, fechaHoraTrans: '2026-11-06T10:00' },
        { tipoPagoId: 1, importe: 20, fechaHoraTrans: '' },
      ],
    })
    expect(err).toMatch(/céntimo/i)
  })

  it('colapsa enviados por crédito', () => {
    const collapsed = collapsePlanillaByCredito([
      { creditoId: 1, montoPagar: 10, tipoPagoId: 2, fechaHoraTrans: 'x' },
      { creditoId: 1, montoPagar: 20, tipoPagoId: 1, fechaHoraTrans: '' },
      { creditoId: 2, montoPagar: 5, tipoPagoId: 1, fechaHoraTrans: '' },
    ])
    expect(collapsed).toHaveLength(2)
    expect(collapsed.find((x) => x.creditoId === 1)?.montoPagar).toBe(30)
  })
})
