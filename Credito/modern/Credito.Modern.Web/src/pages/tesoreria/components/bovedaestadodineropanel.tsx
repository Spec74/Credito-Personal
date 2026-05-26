import { Link } from 'react-router-dom'
import { Spin } from 'antd'
import type { BovedaEstadoDinero } from '../../../api/boveda'
import { formatMoney } from '../../../utils/formatMoney'

type Props = {
  data?: BovedaEstadoDinero
  loading: boolean
}

type KpiProps = {
  value: number
  label: string
  to?: string
  total?: boolean
}

function KpiCard({ value, label, to, total }: KpiProps) {
  const inner = (
    <>
      <span className="boveda-estado-dinero__value">{formatMoney(value)}</span>
      <span className="boveda-estado-dinero__label">{label}</span>
    </>
  )
  const className = total
    ? 'boveda-estado-dinero__card boveda-estado-dinero__card--total'
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

export function BovedaEstadoDineroPanel({ data, loading }: Props) {
  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: 24 }}>
        <Spin />
      </div>
    )
  }
  if (!data) {
    return null
  }

  return (
    <div className="boveda-estado-dinero">
      <KpiCard value={data.saldoBoveda} label="Bóveda" />
      <KpiCard value={data.montoCajaChica} label="Caja chica" to="/caja/caja-chica" />
      <KpiCard value={data.montoCajas} label="Cajas diarias" to="/caja/saldos" />
      <KpiCard value={data.montoPlanPagoPendiente} label="Créditos (plan pago)" />
      <KpiCard
        value={data.creditoVencido}
        label="Créditos vencidos"
        to="/informes/credito-vencido"
      />
      <KpiCard
        value={data.vencidoMenor60}
        label="Vencido sin mora (&lt;60)"
        to="/informes/credito-vencido"
      />
      <KpiCard
        value={data.vencidoMayor60}
        label="Vencido con mora (&gt;60)"
        to="/informes/credito-vencido"
      />
      <KpiCard
        value={data.vencidoIrrecuperable}
        label="Irrecuperable"
        to="/informes/credito-vencido"
      />
      <KpiCard value={data.totalFondo} label="Total fondo S/." total />
    </div>
  )
}
