import { describe, expect, it } from 'vitest'
import { formatFecha, formatRelativo } from './formatFecha'

describe('formatRelativo', () => {
  const now = Date.parse('2026-10-23T12:00:00.000Z')

  it('dice Ahora si acaba de pasar', () => {
    expect(formatRelativo('2026-10-23T11:59:40.000Z', now)).toBe('Ahora')
  })

  it('usa minutos y horas', () => {
    expect(formatRelativo('2026-10-23T11:45:00.000Z', now)).toBe('Hace 15 min')
    expect(formatRelativo('2026-10-23T10:00:00.000Z', now)).toBe('Hace 2 h')
    expect(formatRelativo('2026-10-23T14:00:00.000Z', now)).toBe('En 2 h')
  })

  it('usa ayer / mañana / días y cae a fecha', () => {
    expect(formatRelativo('2026-10-22T12:00:00.000Z', now)).toBe('Ayer')
    expect(formatRelativo('2026-10-24T12:00:00.000Z', now)).toBe('Mañana')
    expect(formatRelativo('2026-10-20T12:00:00.000Z', now)).toBe('Hace 3 días')
    expect(formatRelativo('2026-10-01T12:00:00.000Z', now)).toBe(formatFecha('2026-10-01'))
  })
})
