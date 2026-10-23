import type { Dayjs } from 'dayjs'
import { DURACION_DEFAULT_MIN } from './agendaLayout'

export const PASO_HORA_MIN = 15

export function minutosDelDia(d: Dayjs): number {
  return d.hour() * 60 + d.minute()
}

export function etiquetaHora(minutos: number): string {
  const m = ((minutos % (24 * 60)) + 24 * 60) % (24 * 60)
  const hh = Math.floor(m / 60)
  const mm = m % 60
  return `${String(hh).padStart(2, '0')}:${String(mm).padStart(2, '0')}`
}

export function opcionesHora(extraMin?: number): { value: number; label: string }[] {
  const opts = Array.from({ length: (24 * 60) / PASO_HORA_MIN }, (_, i) => {
    const value = i * PASO_HORA_MIN
    return { value, label: etiquetaHora(value) }
  })
  if (extraMin != null && extraMin % PASO_HORA_MIN !== 0) {
    const m = Math.max(0, Math.min(24 * 60 - 1, extraMin))
    opts.push({ value: m, label: etiquetaHora(m) })
    opts.sort((a, b) => a.value - b.value)
  }
  return opts
}

export function aplicarInicio(prevIni: Dayjs, prevFin: Dayjs | undefined, nextIni: Dayjs): {
  cuando: Dayjs
  hasta: Dayjs
} {
  const dur =
    prevFin?.isValid() && prevIni.isValid()
      ? Math.max(PASO_HORA_MIN, prevFin.diff(prevIni, 'minute'))
      : DURACION_DEFAULT_MIN
  const cuando = nextIni.second(0).millisecond(0)
  return { cuando, hasta: cuando.add(dur, 'minute') }
}

export function aplicarFinHora(ini: Dayjs, minutos: number): Dayjs {
  let fin = ini.startOf('day').add(minutos, 'minute').second(0).millisecond(0)
  if (!fin.isAfter(ini)) {
    fin = fin.add(1, 'day')
  }
  return fin
}
