import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import { caeEnDia, inicioSemanaLunes } from './agendaLayout'
import type { AgendaEvento } from '../../../api/intranet'

const DOW = ['L', 'M', 'X', 'J', 'V', 'S', 'D']

export function AgendaMiniMonth({
  value,
  eventos,
  onChange,
}: {
  value: Dayjs
  eventos: AgendaEvento[]
  onChange: (d: Dayjs) => void
}) {
  const start = inicioSemanaLunes(value.startOf('month'))
  const days = Array.from({ length: 42 }, (_, i) => start.add(i, 'day'))
  const hoy = dayjs()

  return (
    <div className="credix-gcal-mini">
      <div className="credix-gcal-mini__nav">
        <button type="button" onClick={() => onChange(value.subtract(1, 'month'))} aria-label="Mes anterior">
          ‹
        </button>
        <strong>{value.format('MMMM YYYY')}</strong>
        <button type="button" onClick={() => onChange(value.add(1, 'month'))} aria-label="Mes siguiente">
          ›
        </button>
      </div>
      <div className="credix-gcal-mini__dow">
        {DOW.map((n) => (
          <span key={n}>{n}</span>
        ))}
      </div>
      <div className="credix-gcal-mini__grid">
        {days.map((d) => {
          const activo = d.isSame(value, 'day')
          const esHoy = d.isSame(hoy, 'day')
          const fuera = d.month() !== value.month()
          const tiene = eventos.some((e) => caeEnDia(e, d))
          return (
            <button
              key={d.format('YYYY-MM-DD')}
              type="button"
              className={`${fuera ? 'is-out' : ''} ${esHoy ? 'is-today' : ''} ${activo ? 'is-active' : ''}`}
              onClick={() => onChange(d)}
            >
              {d.date()}
              {tiene ? <i /> : null}
            </button>
          )
        })}
      </div>
    </div>
  )
}
