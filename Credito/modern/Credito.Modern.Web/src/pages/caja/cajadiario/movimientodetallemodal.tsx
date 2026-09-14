import { useQuery } from '@tanstack/react-query'
import { Alert, Descriptions, Spin } from 'antd'
import { CajaModal } from '../../../components/caja/CajaModal'
import { fetchMovimientoCajaDetalleOv } from '../../../api/cajaDiario'
import type { RptSaldosCajaRow } from '../../../types/api'
import { formatMoney } from '../../../utils/formatMoney'
import { formatFechaHora } from '../../../utils/formatFecha'

/** Paridad doble clic arqueo → MostrarDetalleMovCaja + MostrarDetalleOvMovCaja. */
export function MovimientoDetalleModal({
  movement,
  oficinaId,
  onClose,
}: {
  movement: RptSaldosCajaRow | null
  oficinaId: number
  onClose: () => void
}) {
  const ovQuery = useQuery({
    queryKey: ['mov-caja-detalle-ov', oficinaId, movement?.movimientoCajaId],
    queryFn: () =>
      fetchMovimientoCajaDetalleOv(oficinaId, movement!.movimientoCajaId),
    enabled: movement != null && oficinaId > 0,
  })

  return (
    <CajaModal
      title={`Detalle movimiento ${movement?.movimientoCajaId ?? ''}`}
      open={movement != null}
      onCancel={onClose}
      footer={null}
      width={560}
    >
      {movement ? (
        <>
          <Descriptions column={1} size="small" bordered className="caja-mov-detalle">
            <Descriptions.Item label="Nro. movimiento">
              {movement.movimientoCajaId}
            </Descriptions.Item>
            <Descriptions.Item label="Operación">
              {movement.operacion}
              {movement.estadoActivo === false ? ' (ANULADO)' : ''}
            </Descriptions.Item>
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

          <div style={{ marginTop: 16 }}>
            <Spin spinning={ovQuery.isLoading}>
              {ovQuery.data && ovQuery.data.lineas.length > 0 ? (
                <Alert
                  type="info"
                  showIcon
                  message="Detalle orden de venta"
                  description={
                    <ul style={{ margin: 0, paddingLeft: 18 }}>
                      {ovQuery.data.lineas.map((l) => (
                        <li key={l}>{l}</li>
                      ))}
                    </ul>
                  }
                />
              ) : ovQuery.isFetched &&
                (ovQuery.data?.operacion === 'INI' ||
                  ovQuery.data?.operacion === 'CON' ||
                  ovQuery.data?.operacion === 'CUO') ? (
                <Alert
                  type="info"
                  showIcon={false}
                  message="Sin líneas de orden de venta asociadas"
                />
              ) : null}
            </Spin>
          </div>
        </>
      ) : null}
    </CajaModal>
  )
}
