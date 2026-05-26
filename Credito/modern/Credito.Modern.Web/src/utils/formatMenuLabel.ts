/** Convierte "SALDOS CAJA" → "Saldos caja" para lectura en menú. */
export function formatMenuLabel(text: string | null | undefined): string {
  const raw = text?.trim() ?? ''
  if (!raw) {
    return ''
  }
  if (raw.length <= 3) {
    return raw.toUpperCase()
  }
  return raw
    .toLowerCase()
    .split(/\s+/)
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(' ')
}
