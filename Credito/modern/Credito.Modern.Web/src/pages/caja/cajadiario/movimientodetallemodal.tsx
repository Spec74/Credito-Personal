import { Descriptions } from 'antd'
import { CajaModal } from '../../../components/caja/CajaModal'
import type { RptSaldosCajaRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { formatFechaHora } from '../../../utils/formatFecha'

/** Paridad doble clic arqueo → MostrarDetalleMovCaja (datos del movimiento). */
export function MovimientoDetalleModal({
  movement,
  onClose,
}: {
  movement: RptSaldosCajaRow | null
  onClose: () => void
}) {
  return (
    <CajaModal
      title={`Detalle movimiento ${movement?.movimientoCajaId ?? ''}`}
      open={movement != null}
      onCancel={onClose}
      footer={null}
      width={520}
    >
      {movement ? (
        <Descriptions column={1} size="small" bordered className="caja-mov-detalle">
          <Descriptions.Item label="Nro. movimiento">
            {movement.movimientoCajaId}
          </Descriptions.Item>
          <Descriptions.Item label="Operación">{movement.operacion}</Descriptions.Item>
          <Descriptions.Item label="Fecha">
            {formatFechaHora(movement.fechaReg)}
          </Descriptions.Item>
          <Descriptions.Item label="Cliente">
            {movement.cliente ?? '—'}
            {movement.codigo ? ` (${movement.codigo})` : ''}
          </Descriptions.Item>
          <Descriptions.Item label="Tipo pago">{movement.tipoPago}</Descriptions.Item>
          <Descriptions.Item label="Importe">
            <span className={movement.indEntrada ? 'caja-monto-entrada' : 'caja-monto-salida'}>
              {movement.indEntrada ? '+' : '-'}
              {formatMoney(movement.importePago)}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Glosa">{movement.glosa ?? '—'}</Descriptions.Item>
        </Descriptions>
      ) : null}
    </CajaModal>
  )
}
