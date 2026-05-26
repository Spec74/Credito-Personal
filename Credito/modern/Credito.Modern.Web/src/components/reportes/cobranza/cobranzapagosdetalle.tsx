import { useMemo, useState } from 'react'
import { Button } from 'antd'
import { DownOutlined, UpOutlined } from '@ant-design/icons'
import { countPagosLista, parsePagoBadge } from './cobranzaPagosUtils'

const MANY_CUOTAS = 24

/** Panel bajo la fila: historial de cuotas (expandir/ocultar por fila). */
export function CobranzaPagosDetalle({
  pagosLista,
  diasAtrazoMora,
  cliente,
}: {
  pagosLista: string[]
  diasAtrazoMora?: number | null
  cliente?: string | null
}) {
  const [alturaCompleta, setAlturaCompleta] = useState(false)

  const badges = useMemo(
    () => pagosLista.map((pago, i) => ({ ...parsePagoBadge(pago, i), key: `${i}-${pago}` })),
    [pagosLista],
  )

  if (pagosLista.length === 0) {
    return (
      <div className="credix-cobranza-detalle credix-cobranza-detalle--empty">
        <p>Sin cuotas registradas para este crédito.</p>
      </div>
    )
  }

  const { pagos, impagos, total } = countPagosLista(pagosLista)
  const muchas = total >= MANY_CUOTAS
  const gridClass = [
    'credix-cobranza-detalle__grid',
    muchas && !alturaCompleta ? 'credix-cobranza-detalle__grid--compact' : '',
    muchas && alturaCompleta ? 'credix-cobranza-detalle__grid--expanded' : '',
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <div className="credix-cobranza-detalle">
      <div className="credix-cobranza-detalle__head">
        <div className="credix-cobranza-detalle__head-text">
          <strong className="credix-cobranza-detalle__title">Historial de cuotas</strong>
          {cliente ? <span className="credix-cobranza-detalle__cliente">{cliente}</span> : null}
          <span className="credix-cobranza-detalle__count">{total} cuota{total === 1 ? '' : 's'}</span>
        </div>
        <ul className="credix-cobranza-detalle__meta">
          <li>
            <span className="credix-cobranza-detalle__meta-label">Pagadas</span>
            <span className="credix-cobranza-detalle__meta-value credix-cobranza-detalle__meta-value--ok">
              {pagos}
            </span>
          </li>
          <li>
            <span className="credix-cobranza-detalle__meta-label">Pendientes</span>
            <span className="credix-cobranza-detalle__meta-value credix-cobranza-detalle__meta-value--fail">
              {impagos}
            </span>
          </li>
          <li>
            <span className="credix-cobranza-detalle__meta-label">Días mora</span>
            <span
              className={
                diasAtrazoMora && diasAtrazoMora > 0
                  ? 'credix-cobranza-detalle__meta-value credix-cobranza-detalle__meta-value--fail'
                  : 'credix-cobranza-detalle__meta-value'
              }
            >
              {diasAtrazoMora ?? 0}
            </span>
          </li>
        </ul>
      </div>

      <div className={gridClass} role="list" aria-label="Cuotas">
        {badges.map((badge) => (
          <div
            key={badge.key}
            role="listitem"
            className={
              badge.impago
                ? 'credix-cobranza-cuota-chip credix-cobranza-cuota-chip--fail'
                : 'credix-cobranza-cuota-chip credix-cobranza-cuota-chip--ok'
            }
            title={badge.fecha ? `${badge.monto} · ${badge.fecha}` : badge.monto}
          >
            <span className="credix-cobranza-cuota-chip__n">#{badge.index}</span>
            <span className="credix-cobranza-cuota-chip__monto">{badge.monto}</span>
            {badge.fecha ? (
              <span className="credix-cobranza-cuota-chip__fecha">{badge.fecha}</span>
            ) : null}
          </div>
        ))}
      </div>

      {muchas ? (
        <div className="credix-cobranza-detalle__foot">
          <span className="credix-cobranza-detalle__scroll-hint">
            {alturaCompleta
              ? 'Vista ampliada activa'
              : 'Vista en cuadrícula con desplazamiento — evita alargar toda la tabla'}
          </span>
          <Button
            type="link"
            size="small"
            className="credix-cobranza-detalle__toggle"
            icon={alturaCompleta ? <UpOutlined /> : <DownOutlined />}
            onClick={() => setAlturaCompleta((v) => !v)}
          >
            {alturaCompleta ? 'Reducir altura' : `Ampliar las ${total} cuotas`}
          </Button>
        </div>
      ) : total > 12 ? (
        <p className="credix-cobranza-detalle__scroll-hint credix-cobranza-detalle__scroll-hint--solo">
          Desplácese dentro del recuadro si no ve todas las cuotas.
        </p>
      ) : null}
    </div>
  )
}
