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
                <Tag color={estadoMeta.color}>{`${estadoMeta.codigo} - ${estadoMeta.label}`}</Tag>
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
            <Descriptions.Item label="Gastos adm.">
              {formatMoney(c.montoGastosAdm)}
            </Descriptions.Item>
            <Descriptions.Item label="1er pago">
              {formatFecha(c.fechaPrimerPago)}
            </Descriptions.Item>
            <Descriptions.Item label="Vencimiento">
              {formatFecha(c.fechaVencimiento)}
            </Descriptions.Item>
            <Descriptions.Item label="Total plan">
              {formatMoney(c.total)}
            </Descriptions.Item>
            <Descriptions.Item label="Cliente" span={2}>
              {c.cliente}
            </Descriptions.Item>
          </Descriptions>
        ) : (
          <Text type="secondary">Seleccione un crédito para ver el resumen.</Text>
        )}
      </Spin>
    </section>
  )
}
