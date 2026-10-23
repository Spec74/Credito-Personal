import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import type { AgendaEvento } from '../../../api/intranet'
import { caeEnDia, inicioSemanaLunes } from './agendaLayout'

const DOW = ['L', 'M', 'X', 'J', 'V', 'S', 'D']

export function AgendaMonthView({
  ancla,
  eventos,
  onCrear,
  onAbrir,
  onIrDia,
  crearAlClic,
}: {
  ancla: Dayjs
  eventos: AgendaEvento[]
  onCrear: (cuando: Dayjs, todoElDia: boolean) => void
  onAbrir: (e: AgendaEvento) => void
  onIrDia: (d: Dayjs) => void
  crearAlClic: boolean
}) {
  const start = inicioSemanaLunes(ancla.startOf('month'))
  const end = inicioSemanaLunes(ancla.endOf('month')).add(6, 'day')
  const days: Dayjs[] = []
  for (let d = start; !d.isAfter(end, 'day'); d = d.add(1, 'day')) days.push(d)
  const weeks = Array.from({ length: days.length / 7 }, (_, i) => days.slice(i * 7, i * 7 + 7))
  const hoy = dayjs()

  return (
    <div className="credix-gcal-month">
      <div className="credix-gcal-month__dow">
        {DOW.map((n) => (
          <span key={n}>{n}</span>
        ))}
      </div>
      {weeks.map((week) => (
        <div key={week[0].format('YYYY-MM-DD')} className="credix-gcal-month__week">
          {week.map((d) => {
            const list = eventos.filter((e) => caeEnDia(e, d))
            const extra = list.length > 3 ? list.length - 3 : 0
            const visible = list.slice(0, 3)
            const fuera = d.month() !== ancla.month()
            const esHoy = d.isSame(hoy, 'day')
            return (
              <div
                key={d.format('YYYY-MM-DD')}
                className={`credix-gcal-mcell${fuera ? ' is-out' : ''}${esHoy ? ' is-today' : ''}`}
                onClick={() => (crearAlClic ? onCrear(d.hour(9).minute(0), false) : onIrDia(d))}
              >
                <button
                  type="button"
                  className="credix-gcal-mcell__num"
                  onClick={(e) => {
                    e.stopPropagation()
                    onIrDia(d)
                  }}
                >
                  {d.date()}
                </button>
                {visible.map((e) => (
                  <button
                    key={e.eventoId}
                    type="button"
                    className={`credix-gcal-mchip${e.alcance === 'OFICINA' ? ' is-sede' : ''}${e.todoElDia ? ' is-all' : ''}`}
                    onClick={(ev) => {
                      ev.stopPropagation()
                      onAbrir(e)
                    }}
                  >
                    {e.todoElDia ? e.titulo : `${dayjs(e.fechaIni).format('HH:mm')} ${e.titulo}`}
                  </button>
                ))}
                {extra > 0 ? (
                  <button
                    type="button"
                    className="credix-gcal-mchip is-more"
                    onClick={(e) => {
                      e.stopPropagation()
                      onIrDia(d)
                    }}
                  >
                    +{extra} más
                  </button>
                ) : null}
              </div>
            )
          })}
        </div>
      ))}
    </div>
  )
}
