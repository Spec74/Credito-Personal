import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Input, Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { SearchOutlined, TeamOutlined } from '@ant-design/icons'
import { fetchCreditosGestorDesembolsados } from '../../../api/cajaDiario'
import type { CreditoGestorPendienteRow } from '../../../api/cajaDiario'
import { CajaModal } from '../../../components/caja/CajaModal'
import { CredixDataTable } from '../../../components/credix'
import { formatMoney } from '../../../utils/formatMoney'

const { Text } = Typography

function matchesQuery(row: CreditoGestorPendienteRow, q: string): boolean {
  const norm = q.trim().toLowerCase()
  if (!norm) {
    return true
  }
  const blob = [
    row.personaCodigo,
    row.personaNombre,
    row.creditoId,
    row.montoCredito,
    row.personaId,
  ]
    .filter((v) => v != null && v !== '')
    .join(' ')
    .toLowerCase()
  return blob.includes(norm)
}

export function CreditosPendientesModal({
  open,
  onClose,
  onSelectCredito,
}: {
  open: boolean
  onClose: () => void
  onSelectCredito: (creditoId: number, personaId: number, label: string) => void
}) {
  const [buscar, setBuscar] = useState('')

  const query = useQuery({
    queryKey: ['caja-creditos-gestor-des'],
    queryFn: fetchCreditosGestorDesembolsados,
    enabled: open,
  })

  const filtrados = useMemo(
    () => (query.data ?? []).filter((r) => matchesQuery(r, buscar)),
    [query.data, buscar],
  )

  const columns: ColumnsType<CreditoGestorPendienteRow> = [
    {
      title: 'Cliente',
      dataIndex: 'personaNombre',
      ellipsis: true,
    },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      align: 'right',
      width: 160,
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
      onCancel={() => {
        setBuscar('')
        onClose()
      }}
      afterOpenChange={(visible) => {
        if (!visible) {
          setBuscar('')
        }
      }}
      footer={null}
      width="min(1180px, 96vw)"
      className="caja-creditos-pendientes-modal"
      rootClassName="caja-creditos-pendientes-modal-root"
      styles={{
        body: {
          padding: 0,
        },
      }}
    >
      <div className="caja-creditos-pendientes-modal__body">
        <div className="caja-creditos-pendientes-modal__toolbar">
          <div className="caja-creditos-pendientes-modal__intro">
            <Text type="secondary">
              Créditos desembolsados (DES) de su gestión. Haga{' '}
              <strong>doble clic</strong> en una fila para cargar el cliente y el
              plan de cuotas en Cobranzas.
            </Text>
            <Tag color="processing">
              {filtrados.length}
              {buscar.trim() ? ` / ${query.data?.length ?? 0}` : ''} crédito(s)
            </Tag>
          </div>

          <div className="caja-creditos-pendientes-modal__search">
            <Input
              allowClear
              size="large"
              autoFocus
              prefix={<SearchOutlined aria-hidden />}
              placeholder="Buscar por cliente, código, n° crédito o monto…"
              value={buscar}
              onChange={(e) => setBuscar(e.target.value)}
              aria-label="Buscar créditos pendientes"
            />
          </div>
        </div>

        <div className="caja-creditos-pendientes-modal__table-wrap">
          <CredixDataTable<CreditoGestorPendienteRow>
            mode="operacion"
            className="caja-creditos-pendientes-table"
            rowKey="creditoId"
            columns={columns}
            dataSource={filtrados}
            loading={query.isLoading}
            locale={{
              emptyText: buscar.trim()
                ? 'Sin coincidencias para la búsqueda'
                : 'No hay créditos pendientes',
            }}
            pagination={{
              defaultPageSize: 20,
              pageSizeOptions: [10, 20, 50, 100],
              showSizeChanger: true,
              showTotal: (t) => `${t} registro(s)`,
            }}
            scroll={{ x: true, y: 'min(52vh, 520px)' }}
            rowClassName={() => 'caja-creditos-pendientes-table__row'}
            onRow={(row) => ({
              onDoubleClick: () => {
                const label = `${row.personaNombre} [${row.personaCodigo}]`
                onSelectCredito(row.creditoId, row.personaId, label)
              },
            })}
          />
        </div>
      </div>
    </CajaModal>
  )
}
