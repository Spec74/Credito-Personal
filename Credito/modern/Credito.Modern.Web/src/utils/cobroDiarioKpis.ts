import type { CredixStatItem } from '../components/credix'
import type { RptCobroDiarioRow } from '../types/api'
import { formatMoney } from './formatMoney'
import { sumMoney } from './money'

export type CobroDiarioKpiOptions = {
  /** Denominación de caja del gestor (MVC ReportParameter Caja). */
  caja?: string
  /** En morosidad gestor: omitir saldo vencido total y destacar solo mora. */
  soloMorosidad?: boolean
  /** Filas seleccionadas para ruta WA (solo cobro diario). */
  seleccionados?: number
}

/** KPIs alineados al bloque de resumen de <c>rptCobroDiario.rdlc</c>. */
export function buildCobroDiarioKpiExtras(
  rows: RptCobroDiarioRow[],
  options?: CobroDiarioKpiOptions,
): CredixStatItem[] {
  if (rows.length === 0) {
    return []
  }

  const saldoCartera = sumMoney(rows.map((r) => r.saldo ?? 0))
  const enAtraso = rows.filter((r) => (r.diasAtrazo ?? 0) > 0 || (r.mora ?? 0) > 0)
  const saldoMoroso = sumMoney(enAtraso.map((r) => r.saldo ?? 0))
  const moraAcumulada = sumMoney(rows.map((r) => r.mora ?? 0))

  const extras: CredixStatItem[] = [
    { value: rows.length, label: 'N° créditos' },
  ]

  if (!options?.soloMorosidad) {
    extras.push({ value: formatMoney(saldoCartera), label: 'Saldo cartera' })
  }

  extras.push(
    { value: enAtraso.length, label: 'En atraso', tone: 'red' },
    { value: formatMoney(saldoMoroso), label: 'Saldo en atraso', tone: 'red' },
    { value: formatMoney(moraAcumulada), label: 'Mora acumulada', tone: 'red' },
  )

  if (options?.caja != null && options.caja !== '') {
    extras.push({ value: options.caja, label: 'Caja' })
  }

  if (options?.seleccionados != null && options.seleccionados > 0) {
    extras.push({ value: options.seleccionados, label: 'Seleccionados' })
  }

  return extras
}
