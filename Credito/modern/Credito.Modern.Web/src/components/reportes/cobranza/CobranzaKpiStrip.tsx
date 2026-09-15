import { formatMoney } from '../../../utils/formatMoney'
import type { CobranzaPagosResumen } from '../../../api/cobranzaPagos'

const KPI = [
  { key: 'clientes', label: 'Clientes', accent: 'clientes' },
  { key: 'credito', label: 'Total crédito', accent: 'credito', money: true },
  { key: 'pagado', label: 'Total pagado', accent: 'pagado', money: true },
  { key: 'saldo', label: 'Saldo pendiente', accent: 'saldo', money: true },
] as const

export function CobranzaKpiStrip({ resumen }: { resumen: CobranzaPagosResumen }) {
  const values: Record<(typeof KPI)[number]['key'], string | number> = {
    clientes: resumen.totalClientes,
    credito: formatMoney(resumen.totalCredito),
    pagado: formatMoney(resumen.totalPagado),
    saldo: formatMoney(resumen.totalSaldo),
  }

  return (
    <div className="credix-cobranza-kpi" role="region" aria-label="Resumen de cobranza">
      {KPI.map((item) => (
        <div
          key={item.key}
          className={`credix-cobranza-kpi__item credix-cobranza-kpi__item--${item.accent}`}
        >
          <span className="credix-cobranza-kpi__label">{item.label}</span>
          <span
            className={
              item.key === 'saldo' && resumen.totalSaldo > 0
                ? 'credix-cobranza-kpi__value credix-cobranza-kpi__value--alert'
                : 'credix-cobranza-kpi__value'
            }
          >
            {values[item.key]}
          </span>
        </div>
      ))}
    </div>
  )
}
