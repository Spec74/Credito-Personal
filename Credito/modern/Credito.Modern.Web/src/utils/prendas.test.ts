import { describe, expect, it } from 'vitest'
import { prendaSimuladorDesdeBienes, prendaVacia, buildSimuladorPrendarioPath } from './prendas'

describe('prendaSimuladorDesdeBienes', () => {
  it('arma descripción y tasación desde el formulario de bienes', () => {
    const pre = prendaSimuladorDesdeBienes(
      [
        { ...prendaVacia(), descripcion: 'MOTO KTM', valorTasacion: 4000, observaciones: 'NA' },
        { ...prendaVacia(), descripcion: '  ', valorTasacion: 10 },
      ],
      '2026-10-30',
    )
    expect(pre).toEqual({
      descripcion: 'MOTO KTM',
      montoTasacion: 4000,
      fechaRemate: '2026-10-30',
      observacion: 'NA',
    })
  })

  it('arma la ruta del simulador con producto prendario', () => {
    const href = buildSimuladorPrendarioPath({
      personaId: 9,
      solicitudCreditoId: 86048,
      prendas: [{ ...prendaVacia(), descripcion: 'MOTO KTM', valorTasacion: 900 }],
      fechaRemate: '2026-10-30',
    })
    const q = new URLSearchParams(href.split('?')[1])
    expect(q.get('productoId')).toBe('2')
    expect(q.get('prendaDescripcion')).toBe('MOTO KTM')
    expect(q.get('prendaMontoTasacion')).toBe('900')
    expect(href.startsWith('/credito/simulador?')).toBe(true)
  })
})
