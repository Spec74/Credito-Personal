import { describe, expect, it } from 'vitest'
import {
  mergeContactoFromInforme,
  normalizeCreditoGestorPendienteRow,
} from './creditoGestorPendiente'

describe('creditoGestorPendiente', () => {
  it('mapea celular camelCase y PascalCase', () => {
    expect(
      normalizeCreditoGestorPendienteRow({
        creditoId: 1,
        personaNombre: 'Ana',
        celular: ' 987654321 ',
      }).celular,
    ).toBe('987654321')

    expect(
      normalizeCreditoGestorPendienteRow({
        CreditoId: 2,
        PersonaNombre: 'Luis',
        Celular: '999888777',
      }).celular,
    ).toBe('999888777')

    expect(
      normalizeCreditoGestorPendienteRow({
        creditoId: 3,
        Celular: '   ',
      }).celular,
    ).toBeNull()
  })

  it('completa celular desde informe cobro diario', () => {
    const base = normalizeCreditoGestorPendienteRow({
      creditoId: 10,
      personaNombre: 'Ana',
      celular: null,
    })
    const merged = mergeContactoFromInforme([base], [
      { creditoId: 10, celular: '911222333', direccion: 'Av. Test' },
    ])
    expect(merged[0]?.celular).toBe('911222333')
    expect(merged[0]?.direccion).toBe('Av. Test')
  })
})
