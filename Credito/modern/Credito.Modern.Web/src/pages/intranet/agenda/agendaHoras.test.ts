import { describe, expect, it } from 'vitest'
import dayjs from 'dayjs'
import { aplicarFinHora, aplicarInicio, etiquetaHora, opcionesHora } from './agendaHoras'

describe('agendaHoras', () => {
  it('etiqueta en 24 h sin segundos', () => {
    expect(etiquetaHora(9 * 60)).toBe('09:00')
    expect(etiquetaHora(9 * 60 + 15)).toBe('09:15')
  })

  it('lista cada 15 minutos e incluye un extra fuera de grilla', () => {
    const opts = opcionesHora(9 * 60 + 6)
    expect(opts).toHaveLength(96 + 1)
    expect(opts.some((o) => o.value === 9 * 60 + 6 && o.label === '09:06')).toBe(true)
  })

  it('al cambiar el inicio conserva la duración', () => {
    const prev = dayjs('2026-10-23T09:00:00')
    const fin = dayjs('2026-10-23T10:30:00')
    const next = aplicarInicio(prev, fin, dayjs('2026-10-24T14:00:00'))
    expect(next.cuando.format('YYYY-MM-DD HH:mm')).toBe('2026-10-24 14:00')
    expect(next.hasta.format('YYYY-MM-DD HH:mm')).toBe('2026-10-24 15:30')
  })

  it('si el fin es antes o igual al inicio, pasa al día siguiente', () => {
    const ini = dayjs('2026-10-23T23:00:00')
    expect(aplicarFinHora(ini, 0).format('YYYY-MM-DD HH:mm')).toBe('2026-10-24 00:00')
  })
})
