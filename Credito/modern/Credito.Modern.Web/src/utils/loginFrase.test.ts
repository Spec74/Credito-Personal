import { describe, expect, it } from 'vitest'
import { fechaLargaEsPe, LOGIN_FRASES, pickLoginFrase, saludoSegunHora } from './loginFrase'

describe('pickLoginFrase', () => {
  it('elige al azar dentro del banco', () => {
    expect(LOGIN_FRASES.length).toBeGreaterThan(3)
    expect(pickLoginFrase(() => 0)).toEqual(LOGIN_FRASES[0])
    expect(pickLoginFrase(() => 0.99)).toEqual(LOGIN_FRASES[LOGIN_FRASES.length - 1])
  })

  it('no deja frases vacías ni fuentes inventadas en blanco', () => {
    for (const f of LOGIN_FRASES) {
      expect(f.texto.trim().length).toBeGreaterThan(12)
      if (f.fuente != null) {
        expect(f.fuente.trim().length).toBeGreaterThan(2)
      }
    }
  })
})

describe('saludoSegunHora', () => {
  it('cambia con la hora del día', () => {
    expect(saludoSegunHora(new Date(2026, 9, 27, 8))).toBe('Buenos días')
    expect(saludoSegunHora(new Date(2026, 9, 27, 15))).toBe('Buenas tardes')
    expect(saludoSegunHora(new Date(2026, 9, 27, 21))).toBe('Buenas noches')
  })
})

describe('fechaLargaEsPe', () => {
  it('escribe el día en español', () => {
    const texto = fechaLargaEsPe(new Date(2026, 9, 27, 9))
    expect(texto).toMatch(/octubre/i)
    expect(texto).toMatch(/27/)
    expect(texto.charAt(0)).toBe(texto.charAt(0).toUpperCase())
  })
})
