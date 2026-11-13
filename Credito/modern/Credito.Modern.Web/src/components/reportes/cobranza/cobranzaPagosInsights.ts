import type { CobranzaPagosResumen, CobranzaPagosRow } from '../../../api/cobranzaPagos'
import { formatMoney } from '../../../utils/formatMoney'
import { sumMoney } from '../../../utils/money'

export type CobranzaSegmento = 'todos' | 'mora' | 'al_dia' | 'sin_saldo' | 'mora_grave'

export type CobranzaDecisionTone = 'danger' | 'warning' | 'success' | 'info'

export type CobranzaDecision = {
  id: string
  tone: CobranzaDecisionTone
  titulo: string
  mensaje: string
  to?: string
  cta?: string
}

export type CobranzaInsights = {
  recuperacionPct: number | null
  clientesMora: number
  clientesAlDia: number
  clientesSinSaldo: number
  clientesMoraGrave: number
  saldoMora: number
  saldoAlDia: number
  pctClientesMora: number
  pctSaldoMora: number
  moraPromedioDias: number
  decisions: CobranzaDecision[]
}

function enMora(row: CobranzaPagosRow): boolean {
  return (row.diasAtrazoMora ?? 0) > 0
}

function moraGrave(row: CobranzaPagosRow): boolean {
  return (row.diasAtrazoMora ?? 0) >= 30
}

function sinSaldo(row: CobranzaPagosRow): boolean {
  return (row.saldo ?? 0) <= 0.009
}

export function filterCobranzaSegmento(
  rows: CobranzaPagosRow[],
  segmento: CobranzaSegmento,
): CobranzaPagosRow[] {
  switch (segmento) {
    case 'mora':
      return rows.filter(enMora)
    case 'mora_grave':
      return rows.filter(moraGrave)
    case 'al_dia':
      return rows.filter((r) => !enMora(r) && !sinSaldo(r))
    case 'sin_saldo':
      return rows.filter(sinSaldo)
    default:
      return rows
  }
}

/** Ordena mora primero (días desc), luego saldo desc — cola de visita. */
export function sortCobranzaPrioridad(rows: CobranzaPagosRow[]): CobranzaPagosRow[] {
  return [...rows].sort((a, b) => {
    const ma = a.diasAtrazoMora ?? 0
    const mb = b.diasAtrazoMora ?? 0
    if (mb !== ma) return mb - ma
    return (b.saldo ?? 0) - (a.saldo ?? 0)
  })
}

export function buildCobranzaInsights(
  rows: CobranzaPagosRow[],
  resumen: CobranzaPagosResumen,
): CobranzaInsights {
  const clientesMora = rows.filter(enMora).length
  const clientesMoraGrave = rows.filter(moraGrave).length
  const clientesSinSaldo = rows.filter(sinSaldo).length
  const clientesAlDia = rows.filter((r) => !enMora(r) && !sinSaldo(r)).length

  const saldoMora = sumMoney(rows.filter(enMora).map((r) => r.saldo ?? 0))
  const saldoAlDia = sumMoney(rows.filter((r) => !enMora(r) && !sinSaldo(r)).map((r) => r.saldo ?? 0))

  const base = resumen.totalPagado + resumen.totalSaldo
  const recuperacionPct =
    base > 0 ? Math.round((resumen.totalPagado / base) * 1000) / 10 : null

  const pctClientesMora =
    resumen.totalClientes > 0
      ? Math.round((clientesMora / resumen.totalClientes) * 1000) / 10
      : 0
  const pctSaldoMora =
    resumen.totalSaldo > 0 ? Math.round((saldoMora / resumen.totalSaldo) * 1000) / 10 : 0

  const diasSum = rows.filter(enMora).reduce((acc, r) => acc + (r.diasAtrazoMora ?? 0), 0)
  const moraPromedioDias =
    clientesMora > 0 ? Math.round((diasSum / clientesMora) * 10) / 10 : 0

  const decisions: CobranzaDecision[] = []

  if (clientesMoraGrave > 0) {
    decisions.push({
      id: 'mora-grave',
      tone: 'danger',
      titulo: 'Mora ≥ 30 días',
      mensaje: `${clientesMoraGrave} cliente(s) con atraso severo. Priorice visita o acuerdo de pago hoy.`,
      to: '/informes/cobro-diario?soloMora=1',
      cta: 'Abrir cobro diario',
    })
  } else if (clientesMora > 0) {
    decisions.push({
      id: 'mora',
      tone: 'warning',
      titulo: 'Clientes en mora',
      mensaje: `${clientesMora} en mora (${pctClientesMora}% de la cartera) · S/ ${formatMoney(saldoMora)} de saldo en riesgo.`,
      to: '/informes/cobro-diario?soloMora=1',
      cta: 'Ver mora',
    })
  }

  if (pctSaldoMora >= 25 && resumen.totalSaldo > 0) {
    decisions.push({
      id: 'saldo-riesgo',
      tone: 'warning',
      titulo: 'Saldo concentrado en mora',
      mensaje: `${pctSaldoMora}% del saldo pendiente está en clientes morosos. Contenga el deterioro.`,
    })
  }

  if (recuperacionPct != null && recuperacionPct < 55) {
    decisions.push({
      id: 'recup-baja',
      tone: 'info',
      titulo: 'Recuperación baja',
      mensaje: `Solo ${recuperacionPct}% del monto total está cobrado. Refuerce la cola de campo.`,
      to: '/caja/cobro',
      cta: 'Cobro campo',
    })
  } else if (recuperacionPct != null && recuperacionPct >= 85 && clientesMora === 0) {
    decisions.push({
      id: 'sana',
      tone: 'success',
      titulo: 'Cartera saludable',
      mensaje: `Recuperación ${recuperacionPct}% y sin clientes en mora en este corte.`,
    })
  }

  if (decisions.length === 0 && resumen.totalClientes > 0) {
    decisions.push({
      id: 'estable',
      tone: 'success',
      titulo: 'Sin alertas críticas',
      mensaje: 'Use los filtros En mora / Al día para afinar la visita del día.',
    })
  }

  return {
    recuperacionPct,
    clientesMora,
    clientesAlDia,
    clientesSinSaldo,
    clientesMoraGrave,
    saldoMora,
    saldoAlDia,
    pctClientesMora,
    pctSaldoMora,
    moraPromedioDias,
    decisions: decisions.slice(0, 3),
  }
}
