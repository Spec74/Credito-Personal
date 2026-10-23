/** ISO → “Hace 5 min / En 2 h / Ayer”. Para campana y bandeja, no para caja. */
export function formatRelativo(value: string | null | undefined, nowMs = Date.now()): string {
  if (!value) {
    return '—'
  }
  const t = Date.parse(value)
  if (Number.isNaN(t)) {
    return formatFecha(value)
  }
  const diff = nowMs - t
  const abs = Math.abs(diff)
  const min = Math.round(abs / 60_000)
  if (min < 1) {
    return diff >= 0 ? 'Ahora' : 'En instantes'
  }
  if (min < 60) {
    return diff >= 0 ? `Hace ${min} min` : `En ${min} min`
  }
  const h = Math.round(min / 60)
  if (h < 24) {
    return diff >= 0 ? `Hace ${h} h` : `En ${h} h`
  }
  const d = Math.round(h / 24)
  if (d === 1) {
    return diff >= 0 ? 'Ayer' : 'Mañana'
  }
  if (d < 7) {
    return diff >= 0 ? `Hace ${d} días` : `En ${d} días`
  }
  return formatFecha(value)
}

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

/**
 * Valor de `<input type="datetime-local">` → mascara legacy `dd/MM/yyyy HH:mm`
 * (varchar(16) en MovimientoCajaExtension.FechaTransferencia).
 */
export function toLegacyFechaTransferencia(
  value: string | null | undefined,
): string | undefined {
  if (!value?.trim()) {
    return undefined
  }
  const trimmed = value.trim()
  const m = trimmed.match(
    /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})(?::\d{2})?/,
  )
  if (!m) {
    // Ya viene en formato legacy u otro; truncar a 16 chars
    return trimmed.slice(0, 16)
  }
  const [, y, mo, d, hh, mm] = m
  return `${d}/${mo}/${y} ${hh}:${mm}`
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
