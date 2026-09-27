import { describe, expect, it } from 'vitest'
import {
  esCelularPeruano,
  prendaSimuladorDesdeBienes,
  prendaVacia,
  buildSimuladorPrendarioPath,
  validarMontoVsTasacion,
  validarPrendasForm,
} from './prendas'

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

describe('validarPrendasForm', () => {
  it('rechaza formulario vacío', () => {
    const r = validarPrendasForm([prendaVacia()])
    expect(r.ok).toBe(false)
    expect(r.mensaje).toMatch(/al menos un bien/i)
  })

  it('rechaza tasación cero con descripción', () => {
    const r = validarPrendasForm([{ ...prendaVacia(), descripcion: 'Anillo', valorTasacion: 0 }])
    expect(r.ok).toBe(false)
    expect(r.errores.some((e) => e.campo === 'valorTasacion')).toBe(true)
  })

  it('acepta bien válido', () => {
    const r = validarPrendasForm([
      { ...prendaVacia(), descripcion: 'Anillo oro', valorTasacion: 1200.5 },
    ])
    expect(r.ok).toBe(true)
    expect(r.bienes).toHaveLength(1)
  })
})

describe('validarMontoVsTasacion', () => {
  it('bloquea monto mayor a tasación', () => {
    expect(validarMontoVsTasacion(5000, 3000)).toMatch(/tasación/i)
  })

  it('acepta monto dentro de tasación', () => {
    expect(validarMontoVsTasacion(2000, 3000)).toBeNull()
  })
})

describe('esCelularPeruano', () => {
  it('valida 9 dígitos que empiezan con 9', () => {
    expect(esCelularPeruano('987654321')).toBe(true)
    expect(esCelularPeruano('187654321')).toBe(false)
  })
})
