import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ReloadOutlined } from '@ant-design/icons'
import { Button, Input, Modal, Segmented, Spin, Table, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchDashboardClientesMora,
  type DashboardClienteMoraRow,
  type DashboardMoraTipo,
} from '../../api/dashboard'
import { ApiError } from '../../api/errors'
import { formatMoney } from '../../utils/formatMoney'

const FILTROS: { label: string; value: DashboardMoraTipo }[] = [
  { label: 'Todos', value: 'TODOS' },
  { label: 'Sin pagar', value: 'SIN_PAGO' },
  { label: 'Nunca pagaron', value: 'NUNCA_PAGO' },
  { label: 'Dejaron de pagar', value: 'DEJO_PAGAR' },
  { label: 'Pagan con atraso', value: 'PAGA_CON_ATRASO' },
]

type Props = {
  open: boolean
  tipoInicial: DashboardMoraTipo
  onClose: () => void
}

export function DashboardClientesMoraModal({ open, tipoInicial, onClose }: Props) {
  const [tipo, setTipo] = useState<DashboardMoraTipo>(tipoInicial)
  const [busqueda, setBusqueda] = useState('')

  useEffect(() => {
    if (open) {
      setTipo(tipoInicial)
      setBusqueda('')
    }
  }, [open, tipoInicial])

  const query = useQuery({
    queryKey: ['dashboard-analista-clientes-mora', tipo],
    queryFn: () => fetchDashboardClientesMora(tipo),
    enabled: open,
    staleTime: 60_000,
  })

  const filtrados = useMemo(() => {
    const q = busqueda.trim().toLowerCase()
    const rows = query.data ?? []
    if (!q) return rows
    return rows.filter((r) => r.nombreCompleto.toLowerCase().includes(q))
  }, [busqueda, query.data])

  const columns: ColumnsType<DashboardClienteMoraRow> = [
    {
      title: 'Cliente',
      dataIndex: 'nombreCompleto',
      key: 'nombre',
      render: (nombre: string, row) => (
        <div>
          <strong className="dash-mora-client-name">{nombre}</strong>
          <div className="dash-mora-client-id">Persona #{row.personaId}</div>
        </div>
      ),
    },
    {
      title: 'Estado',
      dataIndex: 'codigoClasificacion',
      key: 'estado',
      width: 140,
      render: (codigo: string, row) => (
        <span className={`dash-mora-status is-${statusClass(codigo)}`}>{row.clasificacion}</span>
      ),
    },
    {
      title: 'Créditos',
      dataIndex: 'creditosMora',
      key: 'creditos',
      width: 90,
      align: 'center',
    },
    {
      title: 'Saldo mora',
      dataIndex: 'saldoMora',
      key: 'saldo',
      width: 120,
      align: 'right',
      render: (v: number) => `S/ ${formatMoney(v)}`,
    },
    {
      title: '1ª cuota vencida',
      dataIndex: 'primeraCuotaVencida',
      key: 'primera',
      width: 130,
      render: (v: string | null) => formatFechaCorta(v),
    },
    {
      title: 'Último pago',
      dataIndex: 'fechaUltimoPago',
      key: 'ultimo',
      width: 120,
      render: (v: string | null) => (v ? formatFechaCorta(v) : 'Sin pagos'),
    },
    {
      title: 'Días',
      dataIndex: 'diasAtraso',
      key: 'dias',
      width: 70,
      align: 'center',
    },
    {
      title: '',
      key: 'accion',
      width: 110,
      align: 'center',
      render: (_, row) => (
        <Link
          to={`/credito/persona/${row.personaId}`}
          target="_blank"
          rel="noopener noreferrer"
          className="dash-mora-profile-link"
        >
          Ver cliente
        </Link>
      ),
    },
  ]

  return (
    <Modal
      open={open}
      onCancel={onClose}
      title="Clientes en mora"
      width={980}
      footer={null}
      destroyOnHidden
      className="dash-mora-modal"
    >
      <Typography.Paragraph type="secondary" style={{ marginTop: 0 }}>
        {query.isFetching
          ? 'Consultando cartera morosa…'
          : `${filtrados.length} cliente${filtrados.length === 1 ? '' : 's'} encontrado${filtrados.length === 1 ? '' : 's'}`}
      </Typography.Paragraph>

      <div className="dash-mora-toolbar">
        <Segmented
          options={FILTROS}
          value={tipo}
          onChange={(v) => setTipo(v as DashboardMoraTipo)}
        />
        <div className="dash-mora-tools">
          <Input
            allowClear
            placeholder="Buscar cliente…"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            style={{ width: 220 }}
          />
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void query.refetch()}
            loading={query.isFetching}
          >
            Refrescar
          </Button>
        </div>
      </div>

      {query.isError ? (
        <Typography.Text type="danger">{errMsg(query.error)}</Typography.Text>
      ) : query.isLoading ? (
        <div className="dash-mora-loading">
          <Spin />
        </div>
      ) : (
        <Table
          size="small"
          rowKey={(r) => r.personaId}
          columns={columns}
          dataSource={filtrados}
          pagination={{ pageSize: 12, showSizeChanger: false }}
          scroll={{ x: 900 }}
          locale={{ emptyText: 'No hay clientes en mora para este filtro.' }}
        />
      )}
    </Modal>
  )
}

function statusClass(codigo: string) {
  switch (codigo) {
    case 'NUNCA_PAGO':
      return 'never'
    case 'DEJO_PAGAR':
      return 'stopped'
    case 'PAGA_CON_ATRASO':
      return 'paying'
    default:
      return 'neutral'
  }
}

function formatFechaCorta(iso: string | null) {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleDateString('es-PE')
}

function errMsg(error: unknown) {
  if (error instanceof ApiError) return error.message
  if (error instanceof Error) return error.message
  return 'No se pudo cargar la mora.'
}
