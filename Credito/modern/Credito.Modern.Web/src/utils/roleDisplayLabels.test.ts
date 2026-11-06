import { describe, expect, it } from 'vitest'
import { roleDisplayHint, roleDisplayLabel, roleSelectLabel } from './roleDisplayLabels'

describe('roleDisplayLabels', () => {
  it('distingue administrador de sede vs de red', () => {
    expect(roleDisplayLabel('ADMINISTRADOR', { esCentral: true })).toBe('Administrador de red')
    expect(roleDisplayLabel('ADMINISTRADOR', { esCentral: false })).toBe('Administrador de sede')
    expect(roleDisplayLabel('ADMINISTRADOR')).toBe('Administrador')
  })

  it('aclara caja y caja central', () => {
    expect(roleDisplayLabel('CAJA')).toBe('Cajero')
    expect(roleDisplayLabel('CAJA CENTRAL')).toMatch(/caja central/i)
    expect(roleDisplayHint('CAJA CENTRAL')).toMatch(/maestro de cajas/i)
  })

  it('aclara trio de lectura / saldos', () => {
    expect(roleDisplayHint('LECTURA')).toMatch(/crédito/i)
    expect(roleDisplayHint('LECTURA_SALDO')).toMatch(/saldos/i)
    expect(roleDisplayHint('SALDOS CAJA')).toMatch(/saldos/i)
  })

  it('documenta APROBADOR 2 sin poder de aprobación primaria', () => {
    expect(roleDisplayHint('APROBADOR 2')).toMatch(/no aprueba/i)
    expect(roleSelectLabel('APROBADOR 1')).toContain('APROBADOR 1')
  })
})
