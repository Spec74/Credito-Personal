import { useQuery } from '@tanstack/react-query'
import { Table, Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { TeamOutlined } from '@ant-design/icons'
import { fetchCreditosGestorDesembolsados } from '../../../api/cajaDiario'
import type { CreditoGestorPendienteRow } from '../../../api/cajaDiario'
import { CajaModal } from '../../../components/caja/CajaModal'
import { formatMoney } from '../../../utils/formatMoney'

const { Text } = Typography

export function CreditosPendientesModal({
  open,
  onClose,
  onSelectCredito,
}: {
  open: boolean
  onClose: () => void
  onSelectCredito: (creditoId: number, personaId: number, label: string) => void
}) {
  const query = useQuery({
    queryKey: ['caja-creditos-gestor-des'],
    queryFn: fetchCreditosGestorDesembolsados,
    enabled: open,
  })

  const columns: ColumnsType<CreditoGestorPendienteRow> = [
    { title: 'Código', dataIndex: 'personaCodigo', width: 1 },
    { title: 'Cliente', dataIndex: 'personaNombre', ellipsis: true, width: 1 },
    { title: 'Crédito', dataIndex: 'creditoId', width: 1, align: 'center' },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      align: 'right',
      width: 1,
      render: (v: number) => <strong>{formatMoney(v)}</strong>,
    },
  ]

  return (
    <CajaModal
      title={
        <span className="caja-creditos-pendientes-modal__title">
          <TeamOutlined aria-hidden />
          Créditos con cuotas pendientes
        </span>
      }
      open={open}
      onCancel={onClose}
      footer={null}
      width={860}
      className="caja-creditos-pendientes-modal"
    >
      <div className="caja-creditos-pendientes-modal__intro">
        <Text type="secondary">
          Créditos desembolsados (DES) de su gestión. Haga{' '}
          <strong>doble clic</strong> en una fila para cargar el cliente y el plan
          de cuotas en Cobranzas.
        </Text>
        <Tag color="processing">
          {query.data?.length ?? 0} crédito(s)
        </Tag>
      </div>

      <Table<CreditoGestorPendienteRow>
        className="caja-creditos-pendientes-table credix-table"
        rowKey="creditoId"
        columns={columns}
        dataSource={query.data ?? []}
        loading={query.isLoading}
        size="small"
        bordered
        tableLayout="auto"
        pagination={{
          defaultPageSize: 15,
          showSizeChanger: true,
          showTotal: (t) => `${t} registro(s)`,
        }}
        scroll={{ x: 'max-content' }}
        rowClassName={() => 'caja-creditos-pendientes-table__row'}
        onRow={(row) => ({
          onDoubleClick: () => {
            const label = `${row.personaNombre} [${row.personaCodigo}]`
            onSelectCredito(row.creditoId, row.personaId, label)
          },
        })}
      />
    </CajaModal>
  )
}
