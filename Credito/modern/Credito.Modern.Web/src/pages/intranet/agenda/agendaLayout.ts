import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import type { AgendaEvento } from '../../../api/intranet'

export const HORA_PX = 52
export const HORA_PX_MOVIL = 44
export const SNAP_MIN = 30
export const DURACION_DEFAULT_MIN = 60

export type VistaAgenda = 'dia' | 'semana' | 'tres' | 'mes' | 'agenda'

export type BloqueAgenda = {
  evento: AgendaEvento
  top: number
  height: number
  col: number
  cols: number
}

export function inicioSemanaLunes(d: Dayjs): Dayjs {
  const dow = d.day()
  const offset = dow === 0 ? 6 : dow - 1
  return d.startOf('day').subtract(offset, 'day')
}

export function diasDeVista(vista: VistaAgenda, ancla: Dayjs): Dayjs[] {
  if (vista === 'dia') return [ancla.startOf('day')]
  if (vista === 'tres') {
    const start = ancla.startOf('day')
    return [start, start.add(1, 'day'), start.add(2, 'day')]
  }
  if (vista === 'semana') {
    const start = inicioSemanaLunes(ancla)
    return Array.from({ length: 7 }, (_, i) => start.add(i, 'day'))
  }
  return [ancla.startOf('day')]
}

export function moverVista(vista: VistaAgenda, ancla: Dayjs, dir: -1 | 1): Dayjs {
  if (vista === 'mes') return ancla.add(dir, 'month')
  if (vista === 'semana') return ancla.add(dir, 'week')
  if (vista === 'tres') return ancla.add(dir * 3, 'day')
  return ancla.add(dir, 'day')
}

export function tituloPeriodo(vista: VistaAgenda, ancla: Dayjs): string {
  if (vista === 'mes') return ancla.format('MMMM YYYY')
  if (vista === 'semana') {
    const dias = diasDeVista('semana', ancla)
    const a = dias[0]
    const b = dias[6]
    if (a.month() === b.month()) return `${a.format('D')} – ${b.format('D [de] MMMM YYYY')}`
    return `${a.format('D MMM')} – ${b.format('D MMM YYYY')}`
  }
  if (vista === 'tres') {
    const dias = diasDeVista('tres', ancla)
    return `${dias[0].format('D MMM')} – ${dias[2].format('D MMM YYYY')}`
  }
  if (ancla.isSame(dayjs(), 'day')) return `Hoy, ${ancla.format('D [de] MMMM')}`
  return ancla.format('dddd D [de] MMMM YYYY')
}

export function minutosDesdeMedianoche(value: string | Dayjs): number {
  const d = typeof value === 'string' ? dayjs(value) : value
  return d.hour() * 60 + d.minute()
}

export function snapMinutos(min: number, snap = SNAP_MIN): number {
  const capped = Math.max(0, Math.min(24 * 60 - snap, min))
  return Math.round(capped / snap) * snap
}

export function finEvento(e: AgendaEvento): Dayjs {
  if (e.todoElDia) {
    return (e.fechaFin ? dayjs(e.fechaFin) : dayjs(e.fechaIni)).endOf('day')
  }
  if (e.fechaFin) {
    const fin = dayjs(e.fechaFin)
    if (fin.isAfter(dayjs(e.fechaIni))) return fin
  }
  return dayjs(e.fechaIni).add(DURACION_DEFAULT_MIN, 'minute')
}

export function caeEnDia(e: AgendaEvento, dia: Dayjs): boolean {
  const ini = dayjs(e.fechaIni).startOf('day')
  const fin = finEvento(e).startOf('day')
  const d = dia.startOf('day')
  return !d.isBefore(ini, 'day') && !d.isAfter(fin, 'day')
}

export function layoutDia(eventos: AgendaEvento[], horaPx = HORA_PX): BloqueAgenda[] {
  const timed = eventos
    .filter((e) => !e.todoElDia)
    .map((evento) => {
      const start = minutosDesdeMedianoche(evento.fechaIni)
      const end = Math.max(start + 15, minutosDesdeMedianoche(finEvento(evento)))
      return { evento, start, end }
    })
    .sort((a, b) => a.start - b.start || b.end - a.end)

  type Node = (typeof timed)[number] & { col: number; cols: number }
  const nodes: Node[] = timed.map((t) => ({ ...t, col: 0, cols: 1 }))

  let group: Node[] = []
  let groupEnd = -1

  const cerrarGrupo = () => {
    if (group.length === 0) return
    const used = Math.max(1, ...group.map((n) => n.col + 1))
    for (const n of group) n.cols = used
    group = []
    groupEnd = -1
  }

  for (const node of nodes) {
    if (group.length > 0 && node.start >= groupEnd) cerrarGrupo()
    const ocupadas = new Set(
      group.filter((g) => g.end > node.start && g.start < node.end).map((g) => g.col),
    )
    let col = 0
    while (ocupadas.has(col)) col += 1
    node.col = col
    group.push(node)
    groupEnd = Math.max(groupEnd, node.end)
  }
  cerrarGrupo()

  return nodes.map((n) => ({
    evento: n.evento,
    top: (n.start / 60) * horaPx,
    height: Math.max(((n.end - n.start) / 60) * horaPx, 18),
    col: n.col,
    cols: n.cols,
  }))
}

export function yAMinutos(offsetY: number, horaPx: number): number {
  return snapMinutos((offsetY / horaPx) * 60)
}
