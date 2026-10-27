import { describe, expect, it } from 'vitest'
import {
  capitalizarApodo,
  daySeed,
  LOGIN_APODOS,
  LOGIN_LINEAS,
  pickLoginSaludoDia,
} from './loginSaludo'

describe('apodos y líneas de oficio', () => {
  it('tiene un banco amplio, único y usable en voz alta', () => {
    expect(LOGIN_APODOS.length).toBeGreaterThan(80)
    expect(new Set(LOGIN_APODOS).size).toBe(LOGIN_APODOS.length)
    for (const apodo of LOGIN_APODOS) {
      expect(apodo).toBe(apodo.toLowerCase())
      expect(apodo.trim().length).toBeGreaterThan(3)
      expect(apodo.length).toBeLessThan(18)
    }
    expect(LOGIN_LINEAS.length).toBeGreaterThan(24)
    for (const linea of LOGIN_LINEAS) {
      expect(linea.trim().length).toBeGreaterThan(8)
      expect(linea.length).toBeLessThan(42)
    }
  })
})

describe('pickLoginSaludoDia', () => {
  it('es estable el mismo día y cambia el saludo con la hora', () => {
    const manana = pickLoginSaludoDia(new Date(2026, 9, 27, 8))
    const tarde = pickLoginSaludoDia(new Date(2026, 9, 27, 16))
    expect(manana.saludo).toBe('Buenos días')
    expect(tarde.saludo).toBe('Buenas tardes')
    expect(manana.apodo).toBe(tarde.apodo)
    expect(manana.linea).toBe(tarde.linea)
  })

  it('puede variar el apodo entre días', () => {
    expect(daySeed(new Date(2026, 9, 27))).not.toBe(daySeed(new Date(2026, 9, 28)))
    expect(capitalizarApodo('constante')).toBe('Constante')
  })
})
