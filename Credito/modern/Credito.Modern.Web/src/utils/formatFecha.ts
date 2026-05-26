/** ISO o fecha API → DD/MM/YYYY (solo día; sin hora en pantalla). */
export function formatFecha(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }
  const d = value.slice(0, 10)
  const [y, m, day] = d.split('-')
  if (!y || !m || !day) {
    return value
  }
  return `${day}/${m}/${y}`
}

/** ISO con hora → DD/MM/YYYY HH:mm (operaciones de caja, registro, aprobación con timestamp). */
export function formatFechaHora(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }
  const trimmed = value.trim()
  const datePart = trimmed.slice(0, 10)
  const [y, m, day] = datePart.split('-')
  if (!y || !m || !day) {
    return value
  }
  const timeMatch = trimmed.match(/T(\d{2}):(\d{2})| (\d{2}):(\d{2})/)
  if (!timeMatch) {
    return formatFecha(trimmed)
  }
  const hh = timeMatch[1] ?? timeMatch[3]
  const mm = timeMatch[2] ?? timeMatch[4]
  if (hh === '00' && mm === '00') {
    return formatFecha(trimmed)
  }
  return `${day}/${m}/${y} ${hh}:${mm}`
}

/** True si el valor trae hora distinta de medianoche (export CSV/PDF y UI). */
export function hasInformeTime(value: string | null | undefined): boolean {
  if (!value) {
    return false
  }
  const t = value.trim()
  const m = t.match(/T(\d{2}):(\d{2})(?::(\d{2}))?| (\d{2}):(\d{2})/)
  if (!m) {
    return false
  }
  const hh = m[1] ?? m[4]
  const mm = m[2] ?? m[5]
  const ss = m[3] ?? '00'
  return !(hh === '00' && mm === '00' && (ss === '00' || ss === undefined))
}

/**
 * Columnas de informe: fecha de negocio → solo día; timestamps → día y hora.
 * Alineado con CSV (`yyyy-MM-dd` vs `yyyy-MM-dd HH:mm:ss`) en exportadores modernos.
 */
export function formatInformeFecha(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }
  return hasInformeTime(value) ? formatFechaHora(value) : formatFecha(value)
}
