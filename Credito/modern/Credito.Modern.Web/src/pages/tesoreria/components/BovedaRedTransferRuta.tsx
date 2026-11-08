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
  /** Ledger bóveda (paridad USP) — lo único remesable/fondeable. */
  disponibleBoveda: number | null
  pendienteCaja?: number | null
  disponibleFamiliaBoveda?: number | null
  disponibleLoading: boolean
  monto: number | null
}

/**
 * Ruta de medios en remesa/fondeo: origen → destino, saldo de bóveda vs caja pendiente.
 * Nunca mezcla caja en el disponible remesable (evita descuadre / falso positivo).
 */
export function BovedaRedTransferRuta({
  modo,
  tipoPagoDebitoId,
  tipoPagoCreditoId,
  disponibleBoveda,
  pendienteCaja = null,
  disponibleFamiliaBoveda = null,
  disponibleLoading,
  monto,
}: Props) {
  const nombreDebito =
    medioNombrePorItemId(tipoPagoDebitoId) ?? `Medio #${tipoPagoDebitoId}`
  const nombreCredito =
    medioNombrePorItemId(tipoPagoCreditoId) ?? `Medio #${tipoPagoCreditoId}`
  const montoNum = typeof monto === 'number' && Number.isFinite(monto) ? monto : null
  const insuficiente =
    disponibleBoveda != null &&
    montoNum != null &&
    montoNum > 0 &&
    moneyGreater(montoNum, disponibleBoveda)
  const familiaCruzada = tipoPagoDebitoId !== tipoPagoCreditoId
  const hayCajaPendiente =
    pendienteCaja != null && Number.isFinite(pendienteCaja) && moneyGreater(pendienteCaja, 0)
  const hayLegadoFamilia =
    disponibleBoveda != null &&
    disponibleFamiliaBoveda != null &&
    moneyGreater(disponibleFamiliaBoveda, disponibleBoveda)

  const descripcionInsuficiente = (() => {
    if (disponibleBoveda == null) return undefined
    const partes = [
      `En bóveda solo hay S/ ${formatMoney(disponibleBoveda)} de ${nombreDebito} (lo que se puede remesar/fondear).`,
    ]
    if (hayCajaPendiente) {
      partes.push(
        `Además hay S/ ${formatMoney(pendienteCaja!)} en caja diaria pendiente de pasar a bóveda (cierre/TRE). Ese monto se ve en Medios, pero aún no está en el ledger de bóveda.`,
      )
    }
    if (hayLegadoFamilia) {
      partes.push(
        `Hay S/ ${formatMoney(disponibleFamiliaBoveda!)} en la familia en bóveda (incluye tipados). Unifique con «Entre medios» al genérico antes de remesar.`,
      )
    }
    partes.push('Reduzca el monto, elija otro medio con saldo en bóveda, o cierre cajas si el dinero sigue en caja.')
    return partes.join(' ')
  })()

  return (
    <div className="boveda-red-ruta" role="status" aria-live="polite">
      <div className="boveda-red-ruta__header">
        <Text strong className="boveda-red-ruta__title">
          {modo === 'remesa' ? 'Qué se descuenta aquí' : 'Qué sale de Central'}
        </Text>
        {disponibleLoading ? (
          <Spin size="small" />
        ) : disponibleBoveda != null ? (
          <Text
            className={
              insuficiente
                ? 'boveda-red-ruta__saldo boveda-red-ruta__saldo--warn'
                : 'boveda-red-ruta__saldo'
            }
            translate="no"
          >
            En bóveda S/ {formatMoney(disponibleBoveda)}
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

      {hayCajaPendiente && !insuficiente ? (
        <Alert
          type="info"
          showIcon
          className="boveda-red-ruta__alert"
          message={`S/ ${formatMoney(pendienteCaja!)} de ${nombreDebito} sigue en caja`}
          description="Medios muestra bóveda + caja pendiente. La remesa solo mueve el ledger de bóveda. Cierre las cajas diarias (TRE) para pasar ese saldo a bóveda y luego remese."
        />
      ) : null}

      {hayCajaPendiente && insuficiente ? (
        <Alert
          type="warning"
          showIcon
          className="boveda-red-ruta__alert"
          message="El saldo de Medios aún no está en bóveda"
          description={`Hay S/ ${formatMoney(pendienteCaja!)} en caja pendiente de TRE. Por eso «En bóveda» es S/ ${formatMoney(disponibleBoveda ?? 0)} aunque Medios muestre más.`}
        />
      ) : null}

      {insuficiente ? (
        <Alert
          type="error"
          showIcon
          className="boveda-red-ruta__alert"
          message="Saldo insuficiente en bóveda (medio de origen)"
          description={descripcionInsuficiente}
        />
      ) : null}
    </div>
  )
}
