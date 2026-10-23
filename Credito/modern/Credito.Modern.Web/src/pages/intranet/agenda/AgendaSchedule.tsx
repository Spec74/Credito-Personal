import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import { Empty } from 'antd'
import type { AgendaEvento } from '../../../api/intranet'
import { alcanceAgenda, recordatorioLabel } from '../intranetLabels'
import { caeEnDia, finEvento } from './agendaLayout'

function etiquetaDia(d: Dayjs) {
  if (d.isSame(dayjs(), 'day')) return 'Hoy'
  if (d.isSame(dayjs().add(1, 'day'), 'day')) return 'Mañana'
  if (d.isSame(dayjs().subtract(1, 'day'), 'day')) return 'Ayer'
  return d.format('dddd')
}

export function AgendaSchedule({
  desde,
  dias,
  eventos,
  onAbrir,
  onCrear,
}: {
  desde: Dayjs
  dias: number
  eventos: AgendaEvento[]
  onAbrir: (e: AgendaEvento) => void
  onCrear: (cuando: Dayjs, todoElDia: boolean) => void
}) {
  const grupos: { dia: Dayjs; items: AgendaEvento[] }[] = []
  for (let i = 0; i < dias; i += 1) {
    const dia = desde.startOf('day').add(i, 'day')
    const items = eventos
      .filter((e) => caeEnDia(e, dia))
      .sort((a, b) => {
        if (a.todoElDia !== b.todoElDia) return a.todoElDia ? -1 : 1
        return a.fechaIni.localeCompare(b.fechaIni)
      })
    if (items.length > 0) grupos.push({ dia, items })
  }

  if (grupos.length === 0) {
    return (
      <div className="credix-gcal-schedule">
        <Empty
          description={
            <span>
              Nada en los próximos {dias} días.{' '}
              <button type="button" className="credix-gcal-link" onClick={() => onCrear(dayjs().hour(9).minute(0), false)}>
                Agendar
              </button>
            </span>
          }
        />
      </div>
    )
  }

  return (
    <div className="credix-gcal-schedule">
      {grupos.map(({ dia, items }) => (
        <section key={dia.format('YYYY-MM-DD')} className="credix-gcal-sgroup">
          <header>
            <strong>{etiquetaDia(dia)}</strong>
            <span>{dia.format('D [de] MMMM')}</span>
            <button type="button" className="credix-gcal-link" onClick={() => onCrear(dia.hour(9).minute(0), false)}>
              +
            </button>
          </header>
          <ul>
            {items.map((e) => (
              <li key={`${e.eventoId}-${dia.format('YYYY-MM-DD')}`}>
                <button type="button" className="credix-agenda-card" onClick={() => onAbrir(e)}>
                  <div className="credix-agenda-card__time">
                    {e.todoElDia
                      ? 'Todo el día'
                      : `${dayjs(e.fechaIni).format('HH:mm')}–${finEvento(e).format('HH:mm')}`}
                  </div>
                  <div className="credix-agenda-card__body">
                    <strong>{e.titulo}</strong>
                    <div className="credix-chip-row">
                      <span className={`credix-chip${e.alcance === 'OFICINA' ? ' credix-chip--warn' : ' credix-chip--brand'}`}>
                        {alcanceAgenda(e)}
                      </span>
                      {recordatorioLabel(e.recordatorioMinutos) ? (
                        <span className="credix-chip">{recordatorioLabel(e.recordatorioMinutos)}</span>
                      ) : null}
                      {e.notas ? <span className="credix-chip">{e.notas}</span> : null}
                    </div>
                  </div>
                  <i className={`credix-gcal-swatch${e.alcance === 'OFICINA' ? ' is-sede' : ''}`} />
                </button>
              </li>
            ))}
          </ul>
        </section>
      ))}
    </div>
  )
}
