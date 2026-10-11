import { describe, expect, it } from 'vitest'
import {
  mergeContactoFromInforme,
  normalizeCreditoGestorPendienteRow,
  sugeridaFromInforme,
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

  it('mapea latitud/longitud y descarta 0,0', () => {
    expect(
      normalizeCreditoGestorPendienteRow({
        creditoId: 4,
        latitud: -13.16,
        longitud: -74.22,
      }),
    ).toMatchObject({ latitud: -13.16, longitud: -74.22 })

    expect(
      normalizeCreditoGestorPendienteRow({
        creditoId: 5,
        Latitud: 0,
        Longitud: 0,
      }),
    ).toMatchObject({ latitud: null, longitud: null })
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

  it('rellena cuota sugerida 0 desde cuotaTotal/mora del informe', () => {
    expect(sugeridaFromInforme({ cuotaTotal: 25, mora: 5 })).toBe(30)
    expect(sugeridaFromInforme({ cuotaTotal: 0, cuotaPlan: 40, mora: 0 })).toBe(40)

    const base = normalizeCreditoGestorPendienteRow({
      creditoId: 11,
      personaNombre: 'Ana',
      deudaPendiente: 100,
      cuotaSugerida: 0,
    })
    const merged = mergeContactoFromInforme([base], [
      { creditoId: 11, cuotaTotal: 25, mora: 5 },
    ])
    expect(merged[0]?.cuotaSugerida).toBe(30)
  })
})
