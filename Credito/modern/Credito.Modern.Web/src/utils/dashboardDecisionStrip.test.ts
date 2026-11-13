import { describe, expect, it } from 'vitest'
import { buildDashboardDecisions } from './dashboardDecisionStrip'

describe('buildDashboardDecisions', () => {
  it('prioriza mora alta y flujo negativo', () => {
    const items = buildDashboardDecisions({
      pctMoraCartera: 22,
      coberturaMes: 65,
      flujoNetoHoy: -500,
      cobradoHoy: 100,
      cobradoAyer: 1000,
      creditosPorVencerSemana: 8,
      clientesMora: 40,
    })
    expect(items[0]?.id).toBe('mora-alta')
    expect(items.some((i) => i.id === 'flujo-negativo')).toBe(true)
    expect(items.some((i) => i.id === 'cobertura-baja')).toBe(true)
    expect(items.length).toBeLessThanOrEqual(4)
  })

  it('emite estable cuando no hay alertas', () => {
    const items = buildDashboardDecisions({
      pctMoraCartera: 3,
      coberturaMes: 110,
      flujoNetoHoy: 2000,
      cobradoHoy: 1500,
      cobradoAyer: 1400,
      creditosPorVencerSemana: 1,
      clientesMora: 2,
    })
    expect(items.some((i) => i.id === 'cobertura-sana' || i.id === 'estable')).toBe(true)
  })
})
