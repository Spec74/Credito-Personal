import { Link } from 'react-router-dom'
import { Alert, Spin } from 'antd'
import type { BovedaEstadoDinero, BovedaMedioCapas } from '../../../api/boveda'
import { formatMoney } from '../../../utils/formatMoney'

type Props = {
  data?: BovedaEstadoDinero
  /** Capas del resumen de medios (preferidas si vienen). */
  capas?: BovedaMedioCapas | null
  loading: boolean
  isError?: boolean
  errorMessage?: string
}

type Tile = {
  value: number
  label: string
  to?: string
  hint?: string
  emphasis?: boolean
}

function KpiCard({ value, label, to, hint, emphasis }: Tile) {
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
  const className = [
    'boveda-estado-dinero__card',
    to ? 'boveda-estado-dinero__card--link' : '',
    emphasis ? 'boveda-estado-dinero__card--emphasis' : '',
  ]
    .filter(Boolean)
    .join(' ')
  if (to) {
    return (
      <Link to={to} className={className}>
        {inner}
      </Link>
    )
  }
  return <div className={className}>{inner}</div>
}

/**
 * Dónde está el efectivo operativo: principal · temporal · cajas · total.
 * Cartera (plan/vencida) va aparte: no es liquidez de bóveda.
 */
export function BovedaEstadoDineroPanel({
  data,
  capas,
  loading,
  isError,
  errorMessage,
}: Props) {
  if (loading) {
    return (
      <div className="boveda-estado-dinero boveda-estado-dinero--loading">
        <Spin size="small" />
      </div>
    )
  }

  if (isError && !data && !capas) {
    return (
      <Alert
        type="warning"
        showIcon
        message="No se pudo cargar la liquidez"
        description={errorMessage || 'Intente actualizar la página.'}
      />
    )
  }

  if (!data && !capas) {
    return (
      <Alert
        type="info"
        showIcon
        message="Sin datos de liquidez"
        description="Aún no hay cifras de efectivo para esta oficina."
      />
    )
  }

  const principal =
    capas?.principal ?? data?.montoBovedaPrincipal ?? data?.saldoBoveda ?? 0
  const temporal = capas?.temporal ?? data?.montoBovedaTemporal ?? 0
  const cajas = capas?.cajasPendientes ?? data?.montoCajas ?? 0
  const total =
    data?.efectivoOficina ??
    data?.totalFondo ??
    Number(principal) + Number(temporal) + Number(cajas)

  const tiles: Tile[] = [
    {
      value: Number(principal),
      label: 'En bóveda',
      hint: 'Ledger (cuadre)',
    },
    {
      value: Number(temporal),
      label: 'Temporal',
      hint: Number(temporal) > 0.01 ? 'Turno abierto' : 'Sin temporal',
    },
    {
      value: Number(cajas),
      label: 'En cajas',
      to: '/caja/saldos',
      hint: 'Pendiente TRE',
    },
    {
      value: Number(total),
      label: 'Efectivo oficina',
      hint: 'Suma de capas',
      emphasis: true,
    },
  ]

  const plan = data?.montoPlanPagoPendiente ?? 0
  const vencida = data?.creditoVencido ?? 0

  return (
    <div className="boveda-liquidez" translate="no">
      <div className="boveda-estado-dinero boveda-estado-dinero--liquidez">
        {tiles.map((tile) => (
          <KpiCard key={tile.label} {...tile} />
        ))}
      </div>
      {data ? (
        <div className="boveda-liquidez__cartera">
          <span className="boveda-liquidez__cartera-label">Cartera</span>
          <Link to="/caja/saldos" className="boveda-liquidez__cartera-link">
            Por cobrar {formatMoney(plan)}
          </Link>
          <Link to="/informes/credito-vencido" className="boveda-liquidez__cartera-link">
            Vencida {formatMoney(vencida)}
          </Link>
          <span className="boveda-liquidez__cartera-note">No es efectivo</span>
        </div>
      ) : null}
    </div>
  )
}
