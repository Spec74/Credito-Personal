import { describe, expect, it } from 'vitest'
import { esObservacionReprogramacion } from './creditoEstados'

describe('esObservacionReprogramacion', () => {
  it('detecta la marca del USP', () => {
    expect(esObservacionReprogramacion('REPROGRAMADO CREDITO 1')).toBe(true)
    expect(esObservacionReprogramacion('  reprogramado credito 12  ')).toBe(true)
  })

  it('rechaza observaciones normales', () => {
    expect(esObservacionReprogramacion(null)).toBe(false)
    expect(esObservacionReprogramacion('')).toBe(false)
    expect(esObservacionReprogramacion('Cliente solicita plazo')).toBe(false)
  })
})
