export function formatMoney(value: number | null | undefined): string {
  if (value == null) {
    return '—'
  }
  return value.toLocaleString('es-PE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
}
