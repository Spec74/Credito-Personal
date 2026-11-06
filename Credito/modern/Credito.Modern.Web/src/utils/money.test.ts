import { describe, expect, it } from 'vitest'
import { moneyEquals, roundMoney, sumMoney } from './money'

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

  it('sumMoney resta descuentos en céntimos', () => {
    expect(sumMoney([20.77, 2.91, -0.1])).toBe(23.58)
  })

  it('conteo con céntimos 3×0.20 = 0.60', () => {
    expect(sumMoney([roundMoney(3 * 0.2)])).toBe(0.6)
  })

  it('roundMoney y sumMoney coinciden en midpoints (1.005)', () => {
    expect(roundMoney(1.005)).toBe(sumMoney([1.005]))
    expect(moneyEquals(roundMoney(1.005), sumMoney([1.005]))).toBe(true)
  })

  it('moneyEquals es exacto al céntimo', () => {
    expect(moneyEquals(10.1, 10.1)).toBe(true)
    expect(moneyEquals(10.1, 10.11)).toBe(false)
    expect(moneyEquals(0.1 + 0.2, 0.3)).toBe(true)
  })
})
