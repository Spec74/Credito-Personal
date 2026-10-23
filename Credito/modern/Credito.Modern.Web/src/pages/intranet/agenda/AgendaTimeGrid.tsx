import { useEffect, useMemo, useRef } from 'react'
import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import type { AgendaEvento } from '../../../api/intranet'
import { alcanceAgenda } from '../intranetLabels'
import { HORA_PX, HORA_PX_MOVIL, caeEnDia, layoutDia, yAMinutos } from './agendaLayout'

const HORAS = Array.from({ length: 24 }, (_, i) => i)

export function AgendaTimeGrid({
  dias,
  eventos,
  now,
  movil,
  onCrear,
  onAbrir,
}: {
  dias: Dayjs[]
  eventos: AgendaEvento[]
  now: Dayjs
  movil: boolean
  onCrear: (cuando: Dayjs, todoElDia: boolean) => void
  onAbrir: (e: AgendaEvento) => void
}) {
  const horaPx = movil ? HORA_PX_MOVIL : HORA_PX
  const scrollRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const el = scrollRef.current
    if (!el) return
    const minutes = now.hour() * 60 + now.minute()
    el.scrollTop = Math.max(0, (minutes / 60) * horaPx - 72)
  }, [horaPx, dias[0]?.format('YYYY-MM-DD')])

  return (
    <div className="credix-gcal-grid">
      <div className="credix-gcal-grid__head">
        <div className="credix-gcal-gutter" />
        {dias.map((d) => {
          const hoy = d.isSame(now, 'day')
          return (
            <div key={d.format('YYYY-MM-DD')} className={`credix-gcal-dhead${hoy ? ' is-today' : ''}`}>
              <span>{d.format('ddd')}</span>
              <strong>{d.date()}</strong>
            </div>
          )
        })}
      </div>

      <div className="credix-gcal-allday">
        <div className="credix-gcal-gutter">
          <span>Todo el día</span>
        </div>
        {dias.map((d) => {
          const all = eventos.filter((e) => e.todoElDia && caeEnDia(e, d))
          return (
            <div
              key={d.format('YYYY-MM-DD')}
              className="credix-gcal-allday__col"
              onClick={() => onCrear(d.startOf('day'), true)}
            >
              {all.map((e) => (
                <button
                  key={e.eventoId}
                  type="button"
                  className={`credix-gcal-pill${e.alcance === 'OFICINA' ? ' is-sede' : ''}`}
                  onClick={(ev) => {
                    ev.stopPropagation()
                    onAbrir(e)
                  }}
                >
                  {e.titulo}
                </button>
              ))}
            </div>
          )
        })}
      </div>

      <div className="credix-gcal-grid__scroll" ref={scrollRef}>
        <div className="credix-gcal-hours" style={{ height: 24 * horaPx }}>
          {HORAS.map((h) => (
            <div key={h} className="credix-gcal-hour" style={{ height: horaPx }}>
              {h === 0 ? null : <span>{`${String(h).padStart(2, '0')}:00`}</span>}
            </div>
          ))}
        </div>
        <div className="credix-gcal-days" style={{ height: 24 * horaPx }}>
          {dias.map((d) => (
            <DayColumn
              key={d.format('YYYY-MM-DD')}
              dia={d}
              eventos={eventos.filter((e) => !e.todoElDia && caeEnDia(e, d))}
              now={now}
              horaPx={horaPx}
              onCrear={onCrear}
              onAbrir={onAbrir}
            />
          ))}
        </div>
      </div>
    </div>
  )
}

function DayColumn({
  dia,
  eventos,
  now,
  horaPx,
  onCrear,
  onAbrir,
}: {
  dia: Dayjs
  eventos: AgendaEvento[]
  now: Dayjs
  horaPx: number
  onCrear: (cuando: Dayjs, todoElDia: boolean) => void
  onAbrir: (e: AgendaEvento) => void
}) {
  const bloques = useMemo(() => layoutDia(eventos, horaPx), [eventos, horaPx])
  const esHoy = dia.isSame(now, 'day')
  const nowTop = esHoy ? ((now.hour() * 60 + now.minute()) / 60) * horaPx : null

  return (
    <div
      className={`credix-gcal-col${esHoy ? ' is-today' : ''}`}
      style={{ height: 24 * horaPx }}
      onClick={(e) => {
        if ((e.target as HTMLElement).closest('.credix-gcal-block')) return
        const rect = e.currentTarget.getBoundingClientRect()
        const mins = yAMinutos(e.clientY - rect.top, horaPx)
        onCrear(dia.startOf('day').add(mins, 'minute'), false)
      }}
    >
      {HORAS.map((h) => (
        <div key={h} className="credix-gcal-slot" style={{ height: horaPx }} />
      ))}
      {nowTop != null ? (
        <div className="credix-gcal-now" style={{ top: nowTop }}>
          <i />
        </div>
      ) : null}
      {bloques.map((b) => (
        <button
          key={b.evento.eventoId}
          type="button"
          className={`credix-gcal-block${b.evento.alcance === 'OFICINA' ? ' is-sede' : ''}`}
          style={{
            top: b.top,
            height: b.height,
            left: `calc(${(b.col / b.cols) * 100}% + 2px)`,
            width: `calc(${100 / b.cols}% - 4px)`,
          }}
          onClick={(e) => {
            e.stopPropagation()
            onAbrir(b.evento)
          }}
        >
          <strong>{b.evento.titulo}</strong>
          <span>
            {dayjs(b.evento.fechaIni).format('HH:mm')} · {alcanceAgenda(b.evento)}
          </span>
        </button>
      ))}
    </div>
  )
}
