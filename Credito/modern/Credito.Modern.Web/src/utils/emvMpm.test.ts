import { describe, expect, it } from 'vitest'
import {
  crc16CcittFalse,
  formatEmvAmountPen,
  parseEmvTlv,
  verifyEmvCrc,
  withEmvTransactionAmount,
} from './emvMpm'

describe('crc16CcittFalse', () => {
  it('vector canónico 123456789 → 29B1', () => {
    expect(crc16CcittFalse('123456789')).toBe('29B1')
  })
})

describe('formatEmvAmountPen', () => {
  it('formatea 2 decimales', () => {
    expect(formatEmvAmountPen(570)).toBe('570.00')
    expect(formatEmvAmountPen(12.5)).toBe('12.50')
    expect(formatEmvAmountPen(0)).toBeNull()
    expect(formatEmvAmountPen(-1)).toBeNull()
  })
})

/** Payload mínimo sintético con CRC válido (estático, sin monto). */
function buildStaticSample(): string {
  const body =
    '000201' +
    '010211' +
    '52040000' +
    '5303604' +
    '5802PE' +
    '5913JUAN PEREZ QR' +
    '6004LIMA'
  const crc = crc16CcittFalse(`${body}6304`)
  return `${body}6304${crc}`
}

describe('withEmvTransactionAmount', () => {
  it('inyecta tag 54, marca 01=12 y CRC válido; conserva tag 59', () => {
    const base = buildStaticSample()
    expect(verifyEmvCrc(base)).toBe(true)

    const result = withEmvTransactionAmount(base, 150.5)
    expect(result.ok).toBe(true)
    if (!result.ok) return

    expect(verifyEmvCrc(result.payload)).toBe(true)
    expect(result.amountText).toBe('150.50')
    expect(result.merchantName).toBe('JUAN PEREZ QR')

    const tags = parseEmvTlv(result.payload)
    expect(tags.find((t) => t.id === '01')?.value).toBe('12')
    expect(tags.find((t) => t.id === '54')?.value).toBe('150.50')
    expect(tags.find((t) => t.id === '59')?.value).toBe('JUAN PEREZ QR')
  })

  it('actualiza monto si el base ya traía tag 54', () => {
    const first = withEmvTransactionAmount(buildStaticSample(), 10)
    expect(first.ok).toBe(true)
    if (!first.ok) return
    const second = withEmvTransactionAmount(first.payload, 99.99)
    expect(second.ok).toBe(true)
    if (!second.ok) return
    expect(parseEmvTlv(second.payload).find((t) => t.id === '54')?.value).toBe('99.99')
    expect(verifyEmvCrc(second.payload)).toBe(true)
  })

  it('falla sin monto o payload vacío', () => {
    expect(withEmvTransactionAmount('', 10).ok).toBe(false)
    expect(withEmvTransactionAmount(buildStaticSample(), 0).ok).toBe(false)
  })
})
