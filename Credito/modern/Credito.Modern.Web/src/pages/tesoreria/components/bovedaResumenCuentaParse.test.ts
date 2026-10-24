import { describe, expect, it } from 'vitest'
import {
  esMarcaTipoPago,
  etiquetaSede,
  medioComplemento,
  medioEtiquetaUi,
  medioNombreMarca,
  parseResumenBovedaTexto,
} from './bovedaResumenCuentaParse'

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

describe('medioComplemento', () => {
  it('nombra Efectivo porque el icono no trae letras', () => {
    expect(medioComplemento('Efectivo', 'efectivo')).toBe('Efectivo')
  })

  it('no repite el wordmark: solo sede', () => {
    expect(medioComplemento('Yape Central', 'yape')).toBe('Central')
    expect(medioComplemento('BCP Huanta', 'bcp')).toBe('Huanta')
    expect(medioComplemento('Interbank', 'interbank')).toBe('')
    expect(medioComplemento('Banco de la Nación', 'banco-nacion')).toBe('')
  })

  it('resuelve catálogo BCO CREDITO / YAPE HUANTA', () => {
    expect(medioEtiquetaUi('BCO CREDITO')).toEqual({
      variant: 'bcp',
      aria: 'BCP',
      visible: '',
    })
    expect(medioEtiquetaUi('YAPE HUANTA')).toEqual({
      variant: 'yape',
      aria: 'Yape Huanta',
      visible: 'Huanta',
    })
    expect(medioEtiquetaUi('EFECTIVO')).toEqual({
      variant: 'efectivo',
      aria: 'Efectivo',
      visible: 'Efectivo',
    })
    expect(medioEtiquetaUi('YAPE ANDAHUAYLAS')).toEqual({
      variant: 'yape',
      aria: 'Yape Andahuaylas',
      visible: 'Andahuaylas',
    })
    expect(medioEtiquetaUi('BCO CREDITO PICHARI')).toEqual({
      variant: 'bcp',
      aria: 'BCP Pichari',
      visible: 'Pichari',
    })
  })
})

describe('esMarcaTipoPago', () => {
  it('deja marcas 1–5 y excluye sede disfrazada', () => {
    expect(esMarcaTipoPago({ itemId: 2, denominacion: 'YAPE' })).toBe(true)
    expect(esMarcaTipoPago({ itemId: 6, denominacion: 'YAPE HUANTA' })).toBe(false)
    expect(esMarcaTipoPago({ itemId: 9, denominacion: 'YAPE PICHARI' })).toBe(false)
    expect(esMarcaTipoPago({ itemId: 4, denominacion: 'BCO CREDITO' })).toBe(true)
  })
})

describe('medioNombreMarca', () => {
  it('siempre nombra la marca en el combo', () => {
    expect(medioNombreMarca('EFECTIVO')).toBe('Efectivo')
    expect(medioNombreMarca('YAPE')).toBe('Yape')
    expect(medioNombreMarca('YAPE HUANTA')).toBe('Yape')
    expect(medioNombreMarca('BCO CREDITO')).toBe('BCP')
    expect(medioNombreMarca('BCO NACION')).toBe('Nación')
    expect(medioNombreMarca('INTERBANK')).toBe('Interbank')
  })
})

describe('etiquetaSede', () => {
  it('acorta Oficina.Denominacion', () => {
    expect(etiquetaSede('OFICINA PRINCIPAL', true)).toBe('Central')
    expect(etiquetaSede('SUCURSAL HUANTA', false)).toBe('Huanta')
    expect(etiquetaSede('PICHARI', false)).toBe('Pichari')
    expect(etiquetaSede('ANDAHUAYLAS', false)).toBe('Andahuaylas')
  })
})
