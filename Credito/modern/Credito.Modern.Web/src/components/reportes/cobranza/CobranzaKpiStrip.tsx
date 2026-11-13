import { formatMoney } from '../../../utils/formatMoney'
import type { CobranzaPagosResumen } from '../../../api/cobranzaPagos'
import type { CobranzaInsights } from './cobranzaPagosInsights'

export function CobranzaKpiStrip({
  resumen,
  insights,
}: {
  resumen: CobranzaPagosResumen
  insights: CobranzaInsights
}) {
  const recup = insights.recuperacionPct
  const items = [
    {
      key: 'clientes',
      label: 'Clientes',
      value: String(resumen.totalClientes),
      meta: `${insights.clientesMora} en mora`,
      accent: 'clientes',
      alert: insights.clientesMora > 0,
    },
    {
      key: 'recup',
      label: 'Recuperación',
      value: recup == null ? '—' : `${recup.toFixed(1)} %`,
      meta: 'Pagado / (pagado + saldo)',
      accent: 'pagado',
      alert: recup != null && recup < 55,
    },
    {
      key: 'pagado',
      label: 'Total pagado',
      value: formatMoney(resumen.totalPagado),
      meta: `Crédito ${formatMoney(resumen.totalCredito)}`,
      accent: 'pagado',
      alert: false,
    },
    {
      key: 'saldo',
      label: 'Saldo pendiente',
      value: formatMoney(resumen.totalSaldo),
      meta:
        insights.saldoMora > 0
          ? `S/ ${formatMoney(insights.saldoMora)} en mora (${insights.pctSaldoMora}%)`
          : 'Sin saldo en mora',
      accent: 'saldo',
      alert: resumen.totalSaldo > 0 && insights.pctSaldoMora >= 20,
    },
    {
      key: 'mora',
      label: 'Mora promedio',
      value: insights.clientesMora > 0 ? `${insights.moraPromedioDias} d` : '0 d',
      meta:
        insights.clientesMoraGrave > 0
          ? `${insights.clientesMoraGrave} con ≥ 30 días`
          : 'Sin mora grave',
      accent: 'mora',
      alert: insights.clientesMoraGrave > 0,
    },
  ] as const

  return (
    <div className="credix-cobranza-kpi" role="region" aria-label="Resumen de cobranza">
      {items.map((item) => (
        <div
          key={item.key}
          className={`credix-cobranza-kpi__item credix-cobranza-kpi__item--${item.accent}`}
        >
          <span className="credix-cobranza-kpi__label">{item.label}</span>
          <span
            className={
              item.alert
                ? 'credix-cobranza-kpi__value credix-cobranza-kpi__value--alert'
                : 'credix-cobranza-kpi__value'
            }
          >
            {item.value}
          </span>
          <span className="credix-cobranza-kpi__meta">{item.meta}</span>
        </div>
      ))}
    </div>
  )
}
