import { Link } from 'react-router-dom'
import { Spin } from 'antd'
import type { BovedaEstadoDinero } from '../../../api/boveda'
import { formatMoney } from '../../../utils/formatMoney'

type Props = {
  data?: BovedaEstadoDinero
  loading: boolean
}

type Tile = {
  value: number
  label: string
  to?: string
  hint?: string
}

function KpiCard({ value, label, to, hint }: Tile) {
  const inner = (
    <>
      <span className="boveda-estado-dinero__value" translate="no">
        {formatMoney(value)}
      </span>
      <span className="boveda-estado-dinero__label" translate="no">
        {label}
      </span>
      {hint ? (
        <span className="boveda-estado-dinero__hint" translate="no">
          {hint}
        </span>
      ) : null}
    </>
  )
  const className = to
    ? 'boveda-estado-dinero__card boveda-estado-dinero__card--link'
    : 'boveda-estado-dinero__card'
  if (to) {
    return (
      <Link to={to} className={className}>
        {inner}
      </Link>
    )
  }
  return <div className={className}>{inner}</div>
}

/** Dinero fuera de esta bóveda. El saldo y el total van en las stats de página. */
export function BovedaEstadoDineroPanel({ data, loading }: Props) {
  if (loading) {
    return (
      <div className="boveda-estado-dinero boveda-estado-dinero--loading">
        <Spin size="small" />
      </div>
    )
  }
  if (!data) {
    return null
  }

  const tiles: Tile[] = [
    {
      value: data.efectivoOficina ?? data.totalFondo,
      label: 'Efectivo de oficina',
      hint: 'Bóveda + cajas + chica',
    },
    {
      value: data.montoCajaChica,
      label: 'Caja chica',
      to: '/caja/caja-chica',
      hint: 'Abrir caja chica',
    },
    {
      value: data.montoCajas,
      label: 'Cajas diarias',
      to: '/caja/saldos',
      hint: 'Efectivo de caja y analista',
    },
    {
      value: data.montoPlanPagoPendiente,
      label: 'Por cobrar (plan)',
      hint: 'No es efectivo',
    },
    {
      value: data.creditoVencido,
      label: 'Cartera vencida',
      to: '/informes/credito-vencido',
      hint: 'No es efectivo',
    },
  ]

  return (
    <div className="boveda-estado-dinero" translate="no">
      {tiles.map((tile) => (
        <KpiCard key={tile.label} {...tile} />
      ))}
    </div>
  )
}
