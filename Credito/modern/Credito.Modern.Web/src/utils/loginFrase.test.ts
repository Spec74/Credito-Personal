import { describe, expect, it } from 'vitest'
import { LOGIN_FRASES, pickLoginFrase } from './loginFrase'

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
