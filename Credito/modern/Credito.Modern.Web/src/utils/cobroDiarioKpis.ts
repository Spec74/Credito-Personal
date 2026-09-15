import type { CredixStatItem } from '../components/credix'
import type { RptCobroDiarioRow } from '../types/api'
import { formatMoney } from './formatMoney'

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

  const saldoVencido = rows.reduce((s, r) => s + (r.saldo ?? 0), 0)
  const saldoMoroso = rows
    .filter((r) => (r.mora ?? 0) > 0)
    .reduce((s, r) => s + (r.saldo ?? 0), 0)
  const moraAcumulada = rows.reduce((s, r) => s + (r.mora ?? 0), 0)

  const extras: CredixStatItem[] = [
    { value: rows.length, label: 'N° clientes' },
  ]

  if (!options?.soloMorosidad) {
    extras.push({ value: formatMoney(saldoVencido), label: 'Saldo vencido' })
  }

  extras.push(
    { value: formatMoney(saldoMoroso), label: 'Saldo moroso', tone: 'red' },
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
