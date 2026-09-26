import { describe, expect, it } from 'vitest'
import { parseResumenBovedaTexto } from './bovedaResumenCuentaParse'

describe('parseResumenBovedaTexto', () => {
  it('aclara Central cuando el SP solo marca Huanta (texto real)', () => {
    const parsed = parseResumenBovedaTexto(
      'RESUMEN BOVEDA: EFECTIVO = 1181135.76  YAPE = 805774.72  INTERBANK = -353554.02  BCO CREDITO = 98444.39  BCO NACION = 9071.40  YAPE HUANTA = -13531.10  INTERBANK HUANTA = 0.00  BCO CREDITO HUANTA = 0.00',
    )

    expect(parsed.titulo).toBe('RESUMEN BOVEDA')
    expect(parsed.items.map((i) => i.etiqueta)).toEqual([
      'Efectivo',
      'Yape Central',
      'Interbank Central',
      'BCP Central',
      'Banco de la Nación',
      'Yape Huanta',
      'Interbank Huanta',
      'BCP Huanta',
    ])
    expect(parsed.items.find((i) => i.etiqueta === 'Yape Huanta')?.monto).toBe(-13531.1)
    expect(parsed.items.find((i) => i.etiqueta === 'BCP Central')?.monto).toBe(98444.39)
  })

  it('conserva Central y Huanta si el SP ya los trae', () => {
    const parsed = parseResumenBovedaTexto(
      'RESUMEN BOVEDA: EFECTIVO = 1171496.76  YAPE CENTRAL = 798476.72  YAPE HUANTA = -13531.10  INTERBANK CENTRAL = -353714.02  INTERBANK HUANTA = 0.00  BCP CENTRAL = 98174.39  BCP HUANTA = 0.00  BANCO DE LA NACION = 9071.40',
    )

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
  })

  it('sin sede mantiene etiqueta corta', () => {
    const parsed = parseResumenBovedaTexto('RESUMEN BOVEDA: EFECTIVO = 100  YAPE = 50')
    expect(parsed.items.map((i) => i.etiqueta)).toEqual(['Efectivo', 'Yape'])
  })
})
