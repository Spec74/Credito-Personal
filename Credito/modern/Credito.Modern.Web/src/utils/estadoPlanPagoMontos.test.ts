import { describe, expect, it } from 'vitest'
import {
  importePagadoPlanCuota,
  saldoPorPagarPlanCuota,
  totalPagadoPlanCuotas,
  totalSaldoPorPagarPlanCuotas,
} from './estadoPlanPagoMontos'

describe('estadoPlanPagoMontos', () => {
  it('PAG usa pagoCuota; PEN usa pagoLibre (no el saldo a pagar)', () => {
    expect(
      importePagadoPlanCuota({
        estado: 'PAG',
        pagoCuota: 61.15,
        pagoLibre: 0,
      }),
    ).toBe(61.15)

    expect(
      importePagadoPlanCuota({
        estado: 'PEN',
        pagoCuota: 61.15,
        pagoLibre: 0,
      }),
    ).toBe(0)

    expect(
      importePagadoPlanCuota({
        estado: 'PEN',
        pagoCuota: 19.55,
        pagoLibre: 41.6,
      }),
    ).toBe(41.6)
  })

  it('totalPagado no suma saldos pendientes como cobrado', () => {
    const total = totalPagadoPlanCuotas([
      { estado: 'PAG', pagoCuota: 61.15, pagoLibre: 0 },
      { estado: 'PAG', pagoCuota: 61.15, pagoLibre: 0 },
      { estado: 'PEN', pagoCuota: 19.55, pagoLibre: 41.6 },
      { estado: 'PEN', pagoCuota: 61.15, pagoLibre: 0 },
      { estado: 'PEN', pagoCuota: 61.25, pagoLibre: 0 },
    ])
    expect(total).toBe(163.9)
  })

  it('saldoPorPagar: PAG=0; PEN usa pagoCuota (saldo real)', () => {
    expect(
      saldoPorPagarPlanCuota({
        estado: 'PAG',
        pagoCuota: 61.15,
        pagoLibre: 0,
      }),
    ).toBe(0)

    expect(
      saldoPorPagarPlanCuota({
        estado: 'PEN',
        pagoCuota: 19.55,
        pagoLibre: 41.6,
      }),
    ).toBe(19.55)

    expect(
      totalSaldoPorPagarPlanCuotas([
        { estado: 'PAG', pagoCuota: 61.15, pagoLibre: 0 },
        { estado: 'PEN', pagoCuota: 19.55, pagoLibre: 41.6 },
        { estado: 'PEN', pagoCuota: 61.25, pagoLibre: 0 },
      ]),
    ).toBe(80.8)
  })
})
