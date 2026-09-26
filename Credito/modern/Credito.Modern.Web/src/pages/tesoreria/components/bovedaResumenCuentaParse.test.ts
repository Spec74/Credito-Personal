import { describe, expect, it } from 'vitest'
import { parseResumenBovedaTexto } from './bovedaResumenCuentaParse'

describe('parseResumenBovedaTexto', () => {
  it('conserva Central y Huanta en Yape, Interbank y BCP', () => {
    const parsed = parseResumenBovedaTexto(
      'RESUMEN BOVEDA: EFECTIVO = 1171496.76  YAPE CENTRAL = 798476.72  YAPE HUANTA = -13531.10  INTERBANK CENTRAL = -353714.02  INTERBANK HUANTA = 0.00  BCP CENTRAL = 98174.39  BCP HUANTA = 0.00  BANCO DE LA NACION = 9071.40',
    )

    expect(parsed.titulo).toBe('RESUMEN BOVEDA')
    expect(parsed.items.map((i) => i.etiqueta)).toEqual([
      'Efectivo',
      'Yape Central',
      'Yape Huanta',
      'Interbank Central',
      'Interbank Huanta',
      'BCP Central',
      'BCP Huanta',
      'Banco de la Nación',
    ])
    expect(parsed.items.find((i) => i.etiqueta === 'Yape Huanta')?.monto).toBe(-13531.1)
  })

  it('sin sede mantiene etiqueta corta', () => {
    const parsed = parseResumenBovedaTexto('RESUMEN BOVEDA: EFECTIVO = 100  YAPE = 50')
    expect(parsed.items.map((i) => i.etiqueta)).toEqual(['Efectivo', 'Yape'])
  })
})
