import { describe, expect, it } from 'vitest'
import { situacionPrendario } from './prendarioSituacion'

describe('situacionPrendario', () => {
  it('muestra el ciclo de crédito mientras no está desembolsado', () => {
    expect(situacionPrendario({ categoria: 'Otro', estado: 'CRE' }).label).toBe('Solicitud')
    expect(situacionPrendario({ categoria: 'Otro', estado: 'PEN' }).label).toBe('Pendiente')
    expect(situacionPrendario({ categoria: 'Otro', estado: 'APR' }).label).toBe('Aprobado')
  })

  it('en desembolsados usa la cartera, no el código DES', () => {
    expect(situacionPrendario({ categoria: 'Vigente', estado: 'DES' }).label).toBe('Vigente')
    expect(situacionPrendario({ categoria: 'Vencido', estado: 'DES' }).label).toBe('Vencido')
    expect(situacionPrendario({ categoria: 'Rematado', estado: 'DES' }).label).toBe('Rematado')
  })

  it('prioriza sin bienes sobre el estado', () => {
    expect(situacionPrendario({ categoria: 'SinBienes', estado: 'DES' }).label).toBe('Sin bienes')
  })
})
