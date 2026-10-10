import type { ReactNode } from 'react'
import { Button, Tooltip } from 'antd'
import { MontoCobrarInput } from './MontoCobrarInput'
import {
  EnvironmentOutlined,
  PhoneOutlined,
  WhatsAppOutlined,
} from '@ant-design/icons'
import type { CreditoGestorPendienteRow } from '../../../api/cajaDiario'
import { formatMoney } from '../../../utils/formatMoney'
import { formatFecha } from '../../../utils/formatFecha'
import { FechaHoraDigitalField } from './FechaHoraDigitalField'

const TIPOS_PAGO_DIGITAL = new Set([2, 3, 4, 5])

export type CobroBloqueFieldEdit = {
  montoPagar: number
  tipoPagoId: number
  fechaHoraTrans: string
  cuotasSeleccionadas: number[]
}

type TipoPagoOpt = { value: number; label: string }

type Props = {
  row: CreditoGestorPendienteRow
  edit: CobroBloqueFieldEdit
  tipoPagoOptions: TipoPagoOpt[]
  onPatch: (patch: Partial<CobroBloqueFieldEdit>) => void
  cuotasSlot: ReactNode
}

function digitsPhone(raw: string | null | undefined): string {
  return (raw ?? '').replace(/\D/g, '')
}

function waHref(digits: string): string {
  const n = digits.startsWith('51') ? digits : `51${digits}`
  return `https://wa.me/${n}`
}

function shortTipoLabel(label: string): string {
  const t = label.trim()
  if (t.length <= 10) return t
  const first = t.split(/\s+/)[0] ?? t
  return first.length <= 12 ? first : `${first.slice(0, 10)}…`
}

/**
 * Tarjeta de cobro en campo: un cliente = una captura rápida
 * (monto con atajos, método táctil, contacto a un toque).
 */
export function CobroBloqueFieldCard({
  row,
  edit,
  tipoPagoOptions,
  onPatch,
  cuotasSlot,
}: Props) {
  const enMora = row.diasAtrazo > 0
  const conCobro = edit.montoPagar > 0
  const sug = Math.max(0, row.cuotaSugerida || 0)
  const deuda = Math.max(0, row.deudaPendiente || 0)
  const phone = digitsPhone(row.celular)
  const digital = TIPOS_PAGO_DIGITAL.has(edit.tipoPagoId)

  const setMonto = (montoPagar: number) => {
    const capped = Math.min(Math.max(0, montoPagar), deuda > 0 ? deuda : montoPagar)
    onPatch({ montoPagar: capped, cuotasSeleccionadas: [] })
  }

  return (
    <article
      className={[
        'cobro-bloque-card',
        enMora ? 'cobro-bloque-card--mora' : '',
        conCobro ? 'cobro-bloque-card--cobro' : '',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <header className="cobro-bloque-card__head">
        <div className="cobro-bloque-card__identity">
          <div className="cobro-bloque-card__badges">
            {enMora ? (
              <span className="cobro-bloque-card__badge cobro-bloque-card__badge--mora">
                Mora {row.diasAtrazo}d
              </span>
            ) : (
              <span className="cobro-bloque-card__badge">Al día</span>
            )}
            {conCobro ? (
              <span className="cobro-bloque-card__badge cobro-bloque-card__badge--ok">
                Cobro tipado
              </span>
            ) : null}
          </div>
          <h3 className="cobro-bloque-card__name">{row.personaNombre}</h3>
          <p className="cobro-bloque-card__sub">
            Crédito {row.creditoId} · venc. {formatFecha(row.fechaVencimiento)}
          </p>
        </div>
        <div className="cobro-bloque-card__deuda">
          <span>Deuda</span>
          <strong>S/ {formatMoney(deuda)}</strong>
        </div>
      </header>

      {(phone || row.direccion) && (
        <div className="cobro-bloque-card__contacts" role="group" aria-label="Contacto">
          {phone ? (
            <>
              <a className="cobro-bloque-card__contact" href={`tel:${phone}`}>
                <PhoneOutlined />
                <span>{phone}</span>
              </a>
              <a
                className="cobro-bloque-card__contact cobro-bloque-card__contact--wa"
                href={waHref(phone)}
                target="_blank"
                rel="noreferrer"
              >
                <WhatsAppOutlined />
                <span>WhatsApp</span>
              </a>
            </>
          ) : null}
          {row.direccion ? (
            <a
              className="cobro-bloque-card__contact cobro-bloque-card__contact--dir"
              href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(row.direccion)}`}
              target="_blank"
              rel="noreferrer"
              title={row.direccion}
            >
              <EnvironmentOutlined />
              <span>{row.direccion}</span>
            </a>
          ) : null}
        </div>
      )}

      <div className="cobro-bloque-card__pay">
        <div className="cobro-bloque-card__chips" role="group" aria-label="Montos rápidos">
          <Button
            type={edit.montoPagar > 0 && Math.abs(edit.montoPagar - sug) < 0.005 ? 'primary' : 'default'}
            disabled={sug <= 0}
            onClick={() => setMonto(sug)}
          >
            {sug > 0 ? `Cuota S/ ${formatMoney(sug)}` : 'Sin cuota'}
          </Button>
          <Button
            type={
              edit.montoPagar > 0 && deuda > 0 && Math.abs(edit.montoPagar - deuda) < 0.005
                ? 'primary'
                : 'default'
            }
            disabled={deuda <= 0}
            onClick={() => setMonto(deuda)}
          >
            Deuda
          </Button>
          <Tooltip title="Quitar monto">
            <Button
              disabled={edit.montoPagar <= 0}
              onClick={() => setMonto(0)}
              aria-label="Limpiar monto"
            >
              0
            </Button>
          </Tooltip>
        </div>

        <label className="cobro-bloque-card__monto-label">
          <span>Monto a cobrar</span>
          <MontoCobrarInput
            className="cobro-bloque-card__monto"
            min={0}
            max={deuda > 0 ? deuda : undefined}
            step={0.01}
            value={edit.montoPagar}
            size="large"
            controls={false}
            inputMode="decimal"
            prefix="S/"
            onChange={setMonto}
          />
        </label>

        <div className="cobro-bloque-card__metodos" role="radiogroup" aria-label="Forma de pago">
          {tipoPagoOptions.map((opt) => {
            const active = edit.tipoPagoId === opt.value
            return (
              <button
                key={opt.value}
                type="button"
                role="radio"
                aria-checked={active}
                className={[
                  'cobro-bloque-card__metodo',
                  active ? 'cobro-bloque-card__metodo--active' : '',
                ]
                  .filter(Boolean)
                  .join(' ')}
                onClick={() => onPatch({ tipoPagoId: opt.value })}
              >
                {shortTipoLabel(opt.label)}
              </button>
            )
          })}
        </div>

        {digital ? (
          <div className="cobro-bloque-card__voucher">
            <span className="cobro-bloque-card__voucher-label">Hora del voucher</span>
            <FechaHoraDigitalField
              value={edit.fechaHoraTrans}
              status={!edit.fechaHoraTrans ? 'error' : undefined}
              onChange={(fechaHoraTrans) => onPatch({ fechaHoraTrans })}
            />
          </div>
        ) : null}
      </div>

      <details className="cobro-bloque-card__cuotas">
        <summary>Seleccionar cuotas (opcional)</summary>
        {cuotasSlot}
      </details>
    </article>
  )
}
