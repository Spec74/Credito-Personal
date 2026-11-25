import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Input, Space, Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { SearchOutlined, TeamOutlined } from '@ant-design/icons'
import { fetchCreditosGestorDesembolsados } from '../../../api/cajaDiario'
import type { CreditoGestorPendienteRow } from '../../../api/cajaDiario'
import { CajaModal } from '../../../components/caja/CajaModal'
import { CredixDataTable } from '../../../components/credix'
import { formatFecha } from '../../../utils/formatFecha'
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
    row.deudaPendiente,
    row.cuotaSugerida,
    row.personaId,
  ]
    .filter((v) => v != null && v !== '')
    .join(' ')
    .toLowerCase()
  return blob.includes(norm)
}

function sortGestorValue(a: CreditoGestorPendienteRow, b: CreditoGestorPendienteRow): number {
  // Mora / atraso primero → luego mayor deuda → próximo vencimiento.
  if (b.diasAtrazo !== a.diasAtrazo) return b.diasAtrazo - a.diasAtrazo
  if (b.deudaPendiente !== a.deudaPendiente) return b.deudaPendiente - a.deudaPendiente
  return String(a.fechaVencimiento).localeCompare(String(b.fechaVencimiento))
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
    queryFn: () => fetchCreditosGestorDesembolsados(),
    enabled: open,
  })

  const filtrados = useMemo(() => {
    const rows = (query.data ?? []).filter((r) => matchesQuery(r, buscar))
    return [...rows].sort(sortGestorValue)
  }, [query.data, buscar])

  const totales = useMemo(() => {
    const deuda = filtrados.reduce((s, r) => s + (r.deudaPendiente || 0), 0)
    const mora = filtrados.reduce((s, r) => s + (r.importeMora || 0), 0)
    const enMora = filtrados.filter((r) => r.diasAtrazo > 0).length
    return { deuda, mora, enMora }
  }, [filtrados])

  const columns: ColumnsType<CreditoGestorPendienteRow> = [
    {
      title: 'Cliente',
      key: 'cliente',
      ellipsis: true,
      render: (_, r) => (
        <div className="caja-creditos-pendientes-table__cliente">
          <strong>{r.personaNombre}</strong>
          <span className="caja-creditos-pendientes-table__meta">
            {r.personaCodigo ? `[${r.personaCodigo}] · ` : ''}#{r.creditoId}
          </span>
        </div>
      ),
    },
    {
      title: 'Desembolso',
      dataIndex: 'montoCredito',
      align: 'right',
      width: 110,
      render: (v: number) => (
        <span className="caja-creditos-pendientes-table__muted">{formatMoney(v)}</span>
      ),
    },
    {
      title: 'Deuda',
      dataIndex: 'deudaPendiente',
      align: 'right',
      width: 120,
      sorter: (a, b) => a.deudaPendiente - b.deudaPendiente,
      render: (v: number) => (
        <strong className="caja-creditos-pendientes-table__deuda">{formatMoney(v)}</strong>
      ),
    },
    {
      title: 'A cobrar',
      dataIndex: 'cuotaSugerida',
      align: 'right',
      width: 110,
      render: (v: number) => (
        <span className="caja-creditos-pendientes-table__sugerida">{formatMoney(v)}</span>
      ),
    },
    {
      title: 'Mora',
      dataIndex: 'importeMora',
      align: 'right',
      width: 96,
      render: (v: number) =>
        v > 0 ? (
          <Tag color="error" className="caja-creditos-pendientes-table__tag">
            {formatMoney(v)}
          </Tag>
        ) : (
          <Text type="secondary">—</Text>
        ),
    },
    {
      title: 'Días',
      dataIndex: 'diasAtrazo',
      align: 'center',
      width: 72,
      sorter: (a, b) => a.diasAtrazo - b.diasAtrazo,
      render: (v: number) =>
        v > 0 ? (
          <Tag color="volcano" className="caja-creditos-pendientes-table__tag">
            {v}
          </Tag>
        ) : (
          <Tag color="success" className="caja-creditos-pendientes-table__tag">
            0
          </Tag>
        ),
    },
    {
      title: 'Vence',
      dataIndex: 'fechaVencimiento',
      width: 108,
      render: (v: string) => formatFecha(v),
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
              Cartera DES de su gestión. <strong>Deuda</strong> = capital pendiente + mora.
              {' '}<strong>A cobrar</strong> = cuota sugerida hoy. Doble clic carga Cobranzas.
            </Text>
            <Space size={6} wrap>
              <Tag color="processing">
                {filtrados.length}
                {buscar.trim() ? ` / ${query.data?.length ?? 0}` : ''} crédito(s)
              </Tag>
              {totales.enMora > 0 ? (
                <Tag color="volcano">{totales.enMora} en mora</Tag>
              ) : null}
              <Tag>
                Deuda Σ {formatMoney(totales.deuda)}
              </Tag>
              {totales.mora > 0 ? (
                <Tag color="error">Mora Σ {formatMoney(totales.mora)}</Tag>
              ) : null}
            </Space>
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
            scroll={{ x: 900, y: 'min(52vh, 520px)' }}
            rowClassName={(r) =>
              [
                'caja-creditos-pendientes-table__row',
                r.diasAtrazo > 0 ? 'caja-creditos-pendientes-table__row--mora' : '',
              ]
                .filter(Boolean)
                .join(' ')
            }
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
