import { describe, expect, it } from 'vitest'
import { formatMesEs } from './formatMesEs'

describe('formatMesEs', () => {
  it('formatea mes corto en español', () => {
    expect(formatMesEs('2026-01-01')).toMatch(/ene/i)
    expect(formatMesEs('2026-01-01')).toMatch(/2026/)
  })

  it('formatea mes largo en español', () => {
    expect(formatMesEs('2026-11-01', 'long')).toMatch(/noviembre/i)
  })
})
