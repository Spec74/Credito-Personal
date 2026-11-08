import { Alert, Spin, Typography } from 'antd'
import { formatMoney } from '../../../utils/formatMoney'
import { moneyGreater } from '../../../utils/money'
import { medioNombrePorItemId } from '../../../utils/tipoPago'
import { TipoPagoOptionLabel } from './TipoPagoBrand'

const { Text } = Typography

type Props = {
  /** Remesa (sucursal→Central) o fondeo (Central→sucursal). */
  modo: 'remesa' | 'fondeo'
  tipoPagoDebitoId: number
  tipoPagoCreditoId: number
  disponible: number | null
  disponibleLoading: boolean
  monto: number | null
}

/**
 * Ruta de medios en remesa/fondeo: muestra origen (débito local) → destino,
 * saldo disponible del ledger y alerta si el monto no alcanza.
 */
export function BovedaRedTransferRuta({
  modo,
  tipoPagoDebitoId,
  tipoPagoCreditoId,
  disponible,
  disponibleLoading,
  monto,
}: Props) {
  const nombreDebito =
    medioNombrePorItemId(tipoPagoDebitoId) ?? `Medio #${tipoPagoDebitoId}`
  const nombreCredito =
    medioNombrePorItemId(tipoPagoCreditoId) ?? `Medio #${tipoPagoCreditoId}`
  const montoNum = typeof monto === 'number' && Number.isFinite(monto) ? monto : null
  const insuficiente =
    disponible != null && montoNum != null && montoNum > 0 && moneyGreater(montoNum, disponible)
  const familiaCruzada = tipoPagoDebitoId !== tipoPagoCreditoId

  return (
    <div className="boveda-red-ruta" role="status" aria-live="polite">
      <div className="boveda-red-ruta__header">
        <Text strong className="boveda-red-ruta__title">
          {modo === 'remesa' ? 'Qué se descuenta aquí' : 'Qué sale de Central'}
        </Text>
        {disponibleLoading ? (
          <Spin size="small" />
        ) : disponible != null ? (
          <Text
            className={
              insuficiente
                ? 'boveda-red-ruta__saldo boveda-red-ruta__saldo--warn'
                : 'boveda-red-ruta__saldo'
            }
            translate="no"
          >
            Disponible S/ {formatMoney(disponible)}
          </Text>
        ) : null}
      </div>

      <div className="boveda-red-ruta__flow">
        <div className="boveda-red-ruta__lado">
          <span className="boveda-red-ruta__lado-label">
            {modo === 'remesa' ? 'Origen (esta sede)' : 'Origen (Central)'}
          </span>
          <TipoPagoOptionLabel denominacion={nombreDebito} />
        </div>
        <span className="boveda-red-ruta__arrow" aria-hidden>
          →
        </span>
        <div className="boveda-red-ruta__lado">
          <span className="boveda-red-ruta__lado-label">
            {modo === 'remesa' ? 'Destino (Central)' : 'Destino (sucursal)'}
          </span>
          <TipoPagoOptionLabel denominacion={nombreCredito} />
        </div>
      </div>

      {familiaCruzada ? (
        <Text type="secondary" className="boveda-red-ruta__hint">
          {modo === 'remesa'
            ? 'La sede debita el genérico de familia; Central acredita la billetera elegida.'
            : 'Central debita la billetera; la sucursal acredita el genérico de familia.'}
        </Text>
      ) : (
        <Text type="secondary" className="boveda-red-ruta__hint">
          Transferencia 1:1 del mismo medio en ambas bóvedas.
        </Text>
      )}

      {insuficiente ? (
        <Alert
          type="error"
          showIcon
          className="boveda-red-ruta__alert"
          message="Saldo insuficiente en el medio de origen"
          description={
            disponible != null
              ? `Disponible S/ ${formatMoney(disponible)}. Reduzca el monto o elija otro medio destino cuya familia tenga saldo.`
              : undefined
          }
        />
      ) : null}
    </div>
  )
}
