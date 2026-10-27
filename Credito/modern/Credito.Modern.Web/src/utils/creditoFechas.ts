export function addMonthsIso(months: number, from = new Date()): string {
  const d = new Date(from)
  d.setHours(0, 0, 0, 0)
  d.setMonth(d.getMonth() + months)
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

export function addDaysIso(days: number, from = new Date()): string {
  const d = new Date(from)
  d.setHours(0, 0, 0, 0)
  d.setDate(d.getDate() + days)
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

export function fechaPrimerPagoPorModalidad(formaPago: string, prendario = false): string {
  if (prendario) return addMonthsIso(1)
  switch ((formaPago ?? 'M').toUpperCase()) {
    case 'D':
      return addDaysIso(1)
    case 'S':
      return addDaysIso(7)
    case 'Q':
      return addDaysIso(15)
    default:
      return addMonthsIso(1)
  }
}
