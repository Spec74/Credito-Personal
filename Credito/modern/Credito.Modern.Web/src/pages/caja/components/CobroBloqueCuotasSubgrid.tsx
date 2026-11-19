import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Alert, Checkbox, Spin, Typography } from 'antd'
import { fetchEstadoPlanPago } from '../../../api/creditoPlanes'
import { formatFecha } from '../../../utils/formatFecha'
import { formatMoney } from '../../../utils/formatMoney'
import { roundMoney, sumMoney } from '../../../utils/money'
import type { EstadoPlanPagoCuota } from '../../../types/api'

const { Text } = Typography

export function esCuotaPendiente(c: EstadoPlanPagoCuota): boolean {
  const e = (c.estado ?? '').trim().toUpperCase()
  return e !== 'PAG' && e !== 'CAN'
}

export function montoCuotaPendiente(c: EstadoPlanPagoCuota): number {
  return Math.max(
    0,
    sumMoney([
      Number(c.cuota) || 0,
      Number(c.cargo) || 0,
      -(Number(c.descuento) || 0),
      -(Number(c.pagoLibre) || 0),
      Number(c.importeMora) || 0,
    ]),
  )
}

/** Subgrill de cuotas pendientes para cobro en bloque (campo / caja). */
export function CobroBloqueCuotasSubgrid({
  creditoId,
  deudaMax,
  selected,
  onChange,
}: {
  creditoId: number
  deudaMax: number
  selected: number[]
  onChange: (planPagoIds: number[], monto: number) => void
}) {
  const cuotasQuery = useQuery({
    queryKey: ['cobro-bloque-cuotas', creditoId],
    queryFn: () => fetchEstadoPlanPago(creditoId),
  })

  const pendientes = useMemo(
    () => (cuotasQuery.data ?? []).filter(esCuotaPendiente),
    [cuotasQuery.data],
  )

  if (cuotasQuery.isLoading) {
    return (
      <div className="cobro-bloque-subgrid cobro-bloque-subgrid--loading">
        <Spin size="small" /> <Text type="secondary">Cargando cuotas…</Text>
      </div>
    )
  }

  if (cuotasQuery.isError) {
    return (
      <Alert
        type="warning"
        showIcon
        message="No se pudieron cargar las cuotas"
        description="Puede digitar el monto libre en la fila principal."
      />
    )
  }

  if (pendientes.length === 0) {
    return (
      <Text type="secondary" className="cobro-bloque-subgrid">
        Sin cuotas pendientes seleccionables.
      </Text>
    )
  }

  return (
    <div className="cobro-bloque-subgrid">
      <Text type="secondary" className="cobro-bloque-subgrid__hint">
        Máx. S/ {formatMoney(deudaMax)}
      </Text>
      <div className="cobro-bloque-subgrid__list">
        {pendientes.map((c) => {
          const monto = montoCuotaPendiente(c)
          const checked = selected.includes(c.planPagoId)
          return (
            <label key={c.planPagoId} className="cobro-bloque-subgrid__item">
              <Checkbox
                checked={checked}
                onChange={(ev) => {
                  const next = ev.target.checked
                    ? [...selected, c.planPagoId]
                    : selected.filter((id) => id !== c.planPagoId)
                  const suma = sumMoney(
                    pendientes
                      .filter((p) => next.includes(p.planPagoId))
                      .map((p) => montoCuotaPendiente(p)),
                  )
                  onChange(next, roundMoney(Math.min(suma, deudaMax)))
                }}
              />
              <span>
                Cuota {c.numero} · venc. {formatFecha(c.fechaVencimiento)} · S/{' '}
                {formatMoney(monto)}
                {(c.diasAtrazo ?? 0) > 0 ? ` · ${c.diasAtrazo}d atraso` : ''}
              </span>
            </label>
          )
        })}
      </div>
    </div>
  )
}
