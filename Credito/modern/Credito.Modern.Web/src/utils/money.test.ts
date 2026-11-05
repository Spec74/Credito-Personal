import { describe, expect, it } from 'vitest'
import { roundMoney, sumMoney } from './money'

describe('money', () => {
  it('roundMoney evita 43.649999…', () => {
    let s = 0
    for (let i = 0; i < 15; i++) s += 2.91
    expect(s).toBeLessThan(43.65)
    expect(roundMoney(s)).toBe(43.65)
  })

  it('sumMoney suma 15×2.91 = 43.65 exacto', () => {
    expect(sumMoney(Array(15).fill(2.91))).toBe(43.65)
  })
})
