import { describe, expect, it } from 'vitest'
import {
  isColorSchemePreference,
  readColorSchemePreference,
  resolveCredixColorScheme,
} from './credixColorScheme'

describe('resolveCredixColorScheme', () => {
  it('respeta la preferencia explícita', () => {
    expect(resolveCredixColorScheme({ preference: 'dark', prefersDark: false })).toBe('dark')
    expect(resolveCredixColorScheme({ preference: 'light', prefersDark: true })).toBe('light')
  })

  it('en system sigue el sistema operativo', () => {
    expect(resolveCredixColorScheme({ preference: 'system', prefersDark: true })).toBe('dark')
    expect(resolveCredixColorScheme({ preference: 'system', prefersDark: false })).toBe('light')
  })
})

describe('readColorSchemePreference', () => {
  it('cae a system si no hay valor o es inválido', () => {
    expect(readColorSchemePreference(null)).toBe('system')
    expect(
      readColorSchemePreference({
        getItem: () => 'invertido',
      }),
    ).toBe('system')
    expect(
      readColorSchemePreference({
        getItem: () => 'dark',
      }),
    ).toBe('dark')
  })
})

describe('isColorSchemePreference', () => {
  it('acepta solo light, dark y system', () => {
    expect(isColorSchemePreference('dark')).toBe(true)
    expect(isColorSchemePreference('auto')).toBe(false)
    expect(isColorSchemePreference(null)).toBe(false)
  })
})
