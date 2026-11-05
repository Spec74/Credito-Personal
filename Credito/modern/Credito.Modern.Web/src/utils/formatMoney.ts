import { roundMoney } from './money'

export function formatMoney(value: number | null | undefined): string {
  if (value == null) {
    return '—'
  }
  return roundMoney(value).toLocaleString('es-PE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })
}
