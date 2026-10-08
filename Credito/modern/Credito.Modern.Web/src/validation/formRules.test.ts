import { describe, expect, it } from 'vitest'
import dayjs from 'dayjs'
import {
  FIELD_MAX,
  celularPeRule,
  disabledFechaNacimiento,
  dniRule,
  edadEnAnios,
  maxLen,
  mensajeClaveNueva,
  mensajeDireccionRealista,
  moneyRequired,
  requiredText,
} from './formRules'

describe('formRules', () => {
  it('expone máximos alineados al backend', () => {
    expect(FIELD_MAX.denominacion).toBe(100)
    expect(FIELD_MAX.celular).toBe(9)
    expect(FIELD_MAX.dni).toBe(8)
  })

  it('requiredText incluye required y max', () => {
    const rules = requiredText(50)
    expect(rules).toHaveLength(2)
    expect(rules[0]).toMatchObject({ required: true })
    expect(rules[1]).toMatchObject({ max: 50 })
  })

  it('moneyRequired exige número positivo', () => {
    const rules = moneyRequired()
    expect(rules.some((r) => 'min' in r && r.min === 0.01)).toBe(true)
  })

  it('patrones de documento y celular', () => {
    expect(dniRule.pattern).toEqual(/^\d{8}$/)
    expect(celularPeRule.pattern).toEqual(/^9\d{8}$/)
  })

  it('maxLen reutilizable', () => {
    expect(maxLen(10)).toMatchObject({ max: 10 })
  })

  it('dirección realista rechaza basura y acepta domicilio normal', () => {
    expect(mensajeDireccionRealista(null)).toBeNull()
    expect(mensajeDireccionRealista('123')).toBeTruthy()
    expect(mensajeDireccionRealista('99999999')).toBeTruthy()
    expect(mensajeDireccionRealista('Jr. Lima 245')).toBeNull()
  })

  it('clave nueva exige 8+ con letra y número', () => {
    expect(mensajeClaveNueva('')).toBeTruthy()
    expect(mensajeClaveNueva('abc')).toBeTruthy()
    expect(mensajeClaveNueva('abcdefgh')).toBeTruthy()
    expect(mensajeClaveNueva('12345678')).toBeTruthy()
    expect(mensajeClaveNueva('clave12a')).toBeNull()
  })

  it('fecha nacimiento: edad y disabledDate', () => {
    const hoy = dayjs('2026-10-08')
    expect(edadEnAnios(dayjs('2000-10-08'), hoy)).toBe(26)
    expect(edadEnAnios(dayjs('2008-10-09'), hoy)).toBe(17)
    expect(disabledFechaNacimiento(dayjs('2026-12-01'), 18)).toBe(true)
    expect(disabledFechaNacimiento(dayjs('2010-01-01'), 18)).toBe(true)
    expect(disabledFechaNacimiento(dayjs('1990-05-01'), 18)).toBe(false)
  })
})
