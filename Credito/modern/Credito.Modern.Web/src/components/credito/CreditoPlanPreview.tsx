import { Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { CredixDataTable } from '../credix'
import type { SimuladorCreditoCuota } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

const COLUMNS: ColumnsType<SimuladorCreditoCuota> = [
  { title: 'N.º', dataIndex: 'numero', width: 56, align: 'right' },
  {
    title: 'Fecha',
    dataIndex: 'fechaPago',
    width: 110,
    render: (v: string | null) => formatFecha(v),
  },
  {
    title: 'Cuota',
    dataIndex: 'cuota',
    width: 110,
    align: 'right',
    render: (v: number | null) => formatMoney(v ?? 0),
  },
  {
    title: 'Saldo',
    dataIndex: 'saldo',
    width: 110,
    align: 'right',
    render: (v: number | null) => formatMoney(v ?? 0),
  },
]

export function CreditoPlanPreview({
  cuotas,
  loading,
}: {
  cuotas: SimuladorCreditoCuota[]
  loading?: boolean
}) {
  if (!loading && cuotas.length === 0) {
    return null
  }

  const primera = cuotas[0]?.cuota
  return (
    <div className="credito-plan-preview">
      {primera != null ? (
        <Text type="secondary" className="credito-plan-preview__lead">
          Cuota referencial {formatMoney(primera)} · {cuotas.length} pago
          {cuotas.length === 1 ? '' : 's'}
        </Text>
      ) : null}
      <CredixDataTable<SimuladorCreditoCuota>
        mode="operacion"
        rowKey={(_, i) => String(i)}
        columns={COLUMNS}
        dataSource={cuotas}
        loading={loading}
        pagination={false}
        size="small"
        scroll={{ x: 420 }}
      />
    </div>
  )
}
