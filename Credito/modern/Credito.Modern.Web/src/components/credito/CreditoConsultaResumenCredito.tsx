import { useQuery } from '@tanstack/react-query'
import { Descriptions, Spin, Tag, Typography } from 'antd'
import { fetchRptEstadoCredito } from '../../api/creditoPlanes'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { getCreditoEstadoMeta } from '../../utils/creditoEstados'

const { Text } = Typography

type Props = {
  creditoId: number
}

/**
 * Campos de solo lectura del crédito activo (paridad filas `txtc*` / cabecera en Creditos.cshtml).
 * GAD adelantado (ADE): MontoGastosAdm y CentralRiesgo se cobran juntos en caja (CxC),
 * no en la columna G.A. del plan (queda 0).
 */
export function CreditoConsultaResumenCredito({ creditoId }: Props) {
  const query = useQuery({
    queryKey: ['rpt-estado-credito-resumen', creditoId],
    queryFn: () => fetchRptEstadoCredito(creditoId),
    enabled: creditoId > 0,
    staleTime: creditoStaleTime.operacion,
  })

  const c = query.data?.cabecera
  const estadoMeta = getCreditoEstadoMeta(c?.estado)
  const gadPuro = c?.montoGastosAdm ?? 0
  const centralRiesgo = c?.centralRiesgo ?? 0
  const tipoGa = (c?.tipoGastoAdm ?? '').toUpperCase()
  const gaAdelantado = tipoGa === 'ADE' || tipoGa === ''
  const totalGadCaja = gadPuro + centralRiesgo

  return (
    <section className="credito-consulta-resumen" aria-label="Resumen del crédito">
      <Text strong className="credito-consulta-resumen__title">
        Datos del crédito
      </Text>
      <Spin spinning={query.isLoading}>
        {c ? (
          <Descriptions
            className="credito-consulta-resumen__desc"
            bordered
            size="small"
            column={{ xs: 1, sm: 2, md: 3, lg: 4 }}
          >
            <Descriptions.Item label="Producto">{c.producto}</Descriptions.Item>
            <Descriptions.Item label="Estado">
              {estadoMeta ? (
                <Tag>{`${estadoMeta.codigo} · ${estadoMeta.label}`}</Tag>
              ) : (
                c.estado
              )}
            </Descriptions.Item>
            <Descriptions.Item label="Modalidad">{c.modalidad}</Descriptions.Item>
            <Descriptions.Item label="Analista">{c.analista || '—'}</Descriptions.Item>
            <Descriptions.Item label="Monto crédito">
              {formatMoney(c.montoCredito)}
            </Descriptions.Item>
            <Descriptions.Item label="Cuotas">{c.numeroCuotas}</Descriptions.Item>
            <Descriptions.Item label="Interés %">{c.interes}%</Descriptions.Item>
            <Descriptions.Item label="Gastos adm. (puro)">
              {formatMoney(gadPuro)}
              {gaAdelantado ? (
                <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
                  Adelantado · no va en la columna G.A. del plan
                </Text>
              ) : (
                <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
                  En cuota · se reparte en el plan
                </Text>
              )}
            </Descriptions.Item>
            <Descriptions.Item label="Central de riesgo">
              {centralRiesgo > 0 ? formatMoney(centralRiesgo) : '—'}
            </Descriptions.Item>
            {gaAdelantado && totalGadCaja > 0 ? (
              <Descriptions.Item label="A cobrar en caja (GAD)">
                {formatMoney(totalGadCaja)}
                <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
                  {centralRiesgo > 0
                    ? `GAD ${formatMoney(gadPuro)} + central riesgo ${formatMoney(centralRiesgo)}`
                    : 'Solo GAD puro'}
                </Text>
              </Descriptions.Item>
            ) : null}
            <Descriptions.Item label="1er pago">
              {formatFecha(c.fechaPrimerPago)}
            </Descriptions.Item>
            <Descriptions.Item label="Vencimiento">
              {formatFecha(c.fechaVencimiento)}
            </Descriptions.Item>
            <Descriptions.Item label="Total plan">
              {formatMoney(c.total)}
            </Descriptions.Item>
          </Descriptions>
        ) : (
          <Text type="secondary">Seleccione un crédito para ver el resumen.</Text>
        )}
      </Spin>
    </section>
  )
}
