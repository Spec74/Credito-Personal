/** Fechas de originación: misma regla que MVC `ObtenerFechaPrimerPago` + `usp_SimuladorCredito`. */

export type CodigoFormaPago = 'D' | 'S' | 'Q' | 'M'

function toIsoDate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function atLocalNoon(from: Date): Date {
  const d = new Date(from.getTime())
  d.setHours(12, 0, 0, 0)
  return d
}

export function parseIsoDate(iso: string, fallback = new Date()): Date {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso.trim())
  if (!m) return atLocalNoon(fallback)
  return atLocalNoon(new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3])))
}

/** Paridad SQL `DATEADD(MONTH, n, fecha)`: recorta al último día del mes. */
export function addMonthsIso(months: number, from = new Date()): string {
  const src = atLocalNoon(from)
  const day = src.getDate()
  const d = atLocalNoon(from)
  d.setDate(1)
  d.setMonth(d.getMonth() + months)
  const last = new Date(d.getFullYear(), d.getMonth() + 1, 0).getDate()
  d.setDate(Math.min(day, last))
  return toIsoDate(d)
}

export function addDaysIso(days: number, from = new Date()): string {
  const d = atLocalNoon(from)
  d.setDate(d.getDate() + days)
  return toIsoDate(d)
}

/** Emerson: en crédito normal, si cae domingo la cuota pasa al lunes. Prendario no. */
export function aLunesSiDomingo(from: Date): Date {
  const d = atLocalNoon(from)
  if (d.getDay() === 0) {
    d.setDate(d.getDate() + 1)
  }
  return d
}

/** Acepta `Q` / `Quincenal` / etc. para no perder el recálculo si el select manda etiqueta. */
export function codigoFormaPago(raw: string | null | undefined): CodigoFormaPago {
  const t = (raw ?? 'M').trim().toUpperCase()
  if (t === 'D' || t.startsWith('DIAR')) return 'D'
  if (t === 'S' || t.startsWith('SEM')) return 'S'
  if (t === 'Q' || t.startsWith('QUIN')) return 'Q'
  return 'M'
}

export function etiquetaFormaPago(raw: string | null | undefined): string {
  switch (codigoFormaPago(raw)) {
    case 'D':
      return 'diario'
    case 'S':
      return 'semanal'
    case 'Q':
      return 'quincenal'
    default:
      return 'mensual'
  }
}

/**
 * 1.er pago desde hoy (o `from`). Diario +1, semanal +7, quincenal +15, mensual +1 mes.
 * Prendario siempre mensual.
 */
export function fechaPrimerPagoPorModalidad(
  formaPago: string,
  prendario = false,
  from = new Date(),
): string {
  if (prendario) return addMonthsIso(1, from)
  const cruda = (() => {
    switch (codigoFormaPago(formaPago)) {
      case 'D':
        return addDaysIso(1, from)
      case 'S':
        return addDaysIso(7, from)
      case 'Q':
        return addDaysIso(15, from)
      default:
        return addMonthsIso(1, from)
    }
  })()
  return toIsoDate(aLunesSiDomingo(parseIsoDate(cruda, from)))
}

/**
 * Vencimiento = última cuota: misma regla que `usp_SimuladorCredito`.
 * Crédito normal: si la cuota cae domingo, pasa al lunes. Prendario: corrido.
 */
export function fechaVencimientoPorModalidad(
  formaPago: string,
  nroCuotas: number,
  fechaPrimerPagoIso: string,
  prendario = false,
): string {
  const n = Number.isFinite(nroCuotas) && nroCuotas >= 1 ? Math.floor(nroCuotas) : 1
  let d = parseIsoDate(fechaPrimerPagoIso)
  const codigo = codigoFormaPago(formaPago)
  for (let i = 0; i < n; i += 1) {
    if (!prendario) d = aLunesSiDomingo(d)
    if (i === n - 1) break
    if (prendario || codigo === 'M') {
      d = parseIsoDate(addMonthsIso(1, d))
    } else {
      const dias = codigo === 'D' ? 1 : codigo === 'S' ? 7 : 15
      d = parseIsoDate(addDaysIso(dias, d))
    }
  }
  return toIsoDate(d)
}
