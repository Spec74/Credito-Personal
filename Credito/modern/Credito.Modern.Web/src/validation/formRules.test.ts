import { describe, expect, it } from 'vitest'
import {
  FIELD_MAX,
  celularPeRule,
  dniRule,
  maxLen,
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
})
