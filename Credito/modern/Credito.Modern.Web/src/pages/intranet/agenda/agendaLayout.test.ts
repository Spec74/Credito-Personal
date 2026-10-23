import { describe, expect, it } from 'vitest'
import dayjs from 'dayjs'
import 'dayjs/locale/es'

dayjs.locale('es')
import type { AgendaEvento } from '../../../api/intranet'
import {
  caeEnDia,
  diasDeVista,
  finEvento,
  inicioSemanaLunes,
  layoutDia,
  snapMinutos,
  tituloPeriodo,
} from './agendaLayout'

function ev(ini: string, fin: string, id = 1, todoElDia = false): AgendaEvento {
  return {
    eventoId: id,
    titulo: `E${id}`,
    notas: null,
    fechaIni: ini,
    fechaFin: fin,
    todoElDia,
    recordatorioMinutos: 30,
    alcance: 'PERSONAL',
    oficinaId: null,
    usuarioId: 1,
    esMio: true,
  }
}

describe('agendaLayout', () => {
  it('la semana empieza el lunes', () => {
    const jueves = dayjs('2026-10-22')
    expect(inicioSemanaLunes(jueves).format('YYYY-MM-DD')).toBe('2026-10-19')
  })

  it('arma 7 días y 3 días', () => {
    expect(diasDeVista('semana', dayjs('2026-10-23')).map((d) => d.date())).toEqual([
      19, 20, 21, 22, 23, 24, 25,
    ])
    expect(diasDeVista('tres', dayjs('2026-10-23')).map((d) => d.date())).toEqual([23, 24, 25])
  })

  it('ajusta al cuarto de hora / media hora', () => {
    expect(snapMinutos(44)).toBe(30)
    expect(snapMinutos(50)).toBe(60)
  })

  it('si no hay fin, dura 60 minutos', () => {
    const e = ev('2026-10-23T09:00:00', '2026-10-23T09:00:00')
    e.fechaFin = null
    expect(finEvento(e).format('HH:mm')).toBe('10:00')
  })

  it('coloca solapes en columnas distintas', () => {
    const laid = layoutDia(
      [
        ev('2026-10-23T09:00:00', '2026-10-23T10:00:00', 1),
        ev('2026-10-23T09:30:00', '2026-10-23T10:30:00', 2),
      ],
      60,
    )
    expect(new Set(laid.map((b) => b.col)).size).toBe(2)
    expect(laid.every((b) => b.cols === 2)).toBe(true)
    expect(laid.find((b) => b.evento.eventoId === 1)?.top).toBe(9 * 60)
  })

  it('un evento de todo el día cubre el rango de fechas', () => {
    const e = ev('2026-10-23T00:00:00', '2026-10-24T23:59:00', 3, true)
    expect(caeEnDia(e, dayjs('2026-10-23'))).toBe(true)
    expect(caeEnDia(e, dayjs('2026-10-24'))).toBe(true)
    expect(caeEnDia(e, dayjs('2026-10-25'))).toBe(false)
  })

  it('titula el periodo en español', () => {
    expect(tituloPeriodo('mes', dayjs('2026-10-23')).toLowerCase()).toContain('octubre')
  })
})
