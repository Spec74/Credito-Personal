import { describe, expect, it } from 'vitest'
import type { CobranzaPagosRow } from '../../../api/cobranzaPagos'
import {
  buildCobranzaInsights,
  filterCobranzaSegmento,
  sortCobranzaPrioridad,
} from './cobranzaPagosInsights'

function row(partial: Partial<CobranzaPagosRow> & { nro: number }): CobranzaPagosRow {
  return {
    cliente: `C${partial.nro}`,
    formaPago: 'SEMANAL',
    montoCredito: 1000,
    interes: 100,
    montoTotal: 1100,
    fechaPrimerPago: '2026-01-01',
    fechaVencimiento: '2026-06-01',
    saldo: 500,
    totalPago: 600,
    diasAtrazoMora: 0,
    pagos: null,
    pagosLista: [],
    pagosCount: 0,
    impagosCount: 0,
    ...partial,
  }
}

describe('cobranzaPagosInsights', () => {
  const rows = [
    row({ nro: 1, diasAtrazoMora: 45, saldo: 800 }),
    row({ nro: 2, diasAtrazoMora: 5, saldo: 200 }),
    row({ nro: 3, diasAtrazoMora: 0, saldo: 300 }),
    row({ nro: 4, diasAtrazoMora: 0, saldo: 0 }),
  ]

  it('segmenta mora grave y al día', () => {
    expect(filterCobranzaSegmento(rows, 'mora_grave')).toHaveLength(1)
    expect(filterCobranzaSegmento(rows, 'al_dia')).toHaveLength(1)
    expect(filterCobranzaSegmento(rows, 'sin_saldo')).toHaveLength(1)
  })

  it('ordena por mora y saldo', () => {
    const sorted = sortCobranzaPrioridad(rows)
    expect(sorted[0]?.nro).toBe(1)
    expect(sorted[1]?.nro).toBe(2)
  })

  it('prioriza mora grave en decisiones', () => {
    const insights = buildCobranzaInsights(rows, {
      totalClientes: 4,
      totalCredito: 4000,
      totalPagado: 2400,
      totalSaldo: 1300,
    })
    expect(insights.clientesMora).toBe(2)
    expect(insights.clientesMoraGrave).toBe(1)
    expect(insights.decisions[0]?.id).toBe('mora-grave')
    expect(insights.recuperacionPct).toBeGreaterThan(0)
  })
})
