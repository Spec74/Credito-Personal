import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Modal, Tag, message } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { DeleteOutlined } from '@ant-design/icons'
import {
  eliminarCondonacionPendiente,
  fetchCondonacionesPendientes,
  type CondonacionPendiente,
} from '../../api/creditoCondonacion'
import { ApiError } from '../../api/errors'
import {
  CredixCrudPage,
  CredixCrudToolbar,
  CredixDataTable,
  type CredixStatItem,
} from '../../components/credix'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { formatMoney } from '../../utils/formatMoney'
import { filterTableRows } from '../../utils/tableClientFilter'

function condonacionRowText(row: CondonacionPendiente): string {
  return [
    row.nombreCliente,
    row.creditoId,
    row.personaId,
    row.nombreUsuario,
    row.moraCondonacion,
    row.totalPago,
    row.montoCredito,
    row.fecha,
  ].join(' ')
}

function formatFechaCorta(iso: string) {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleDateString('es-PE')
}

export function CreditoCondonacionesPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [buscar, setBuscar] = useState('')
  const buscarDebounced = useDebouncedValue(buscar, 200)

  const listQuery = useQuery({
    queryKey: ['condonaciones-pendientes'],
    queryFn: fetchCondonacionesPendientes,
    staleTime: 15_000,
  })

  const eliminar = useMutation({
    mutationFn: eliminarCondonacionPendiente,
    onSuccess: () => {
      message.success('Solicitud eliminada')
      void queryClient.invalidateQueries({ queryKey: ['condonaciones-pendientes'] })
    },
    onError: (e: unknown) => {
      message.error(e instanceof ApiError ? e.message : 'No se pudo eliminar la solicitud')
    },
  })

  const rows = useMemo(() => listQuery.data ?? [], [listQuery.data])
  const filtrados = useMemo(
    () => filterTableRows(rows, buscarDebounced, condonacionRowText),
    [rows, buscarDebounced],
  )

  const stats: CredixStatItem[] = useMemo(
    () => [
      { label: 'Pendientes', value: String(rows.length) },
      {
        label: 'Mora a condonar',
        value: `S/ ${formatMoney(rows.reduce((acc, r) => acc + r.moraCondonacion, 0))}`,
      },
    ],
    [rows],
  )

  const columns: ColumnsType<CondonacionPendiente> = useMemo(
    () => [
      {
        title: 'Cliente',
        dataIndex: 'nombreCliente',
        ellipsis: true,
        render: (v: string, row) => (
          <Button
            type="link"
            onClick={() =>
              navigate(`/credito/consulta?personaId=${row.personaId}&creditoId=${row.creditoId}`)
            }
          >
            {v || `Crédito ${row.creditoId}`}
          </Button>
        ),
      },
      {
        title: 'Monto crédito',
        dataIndex: 'montoCredito',
        width: 130,
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Mora condonación',
        dataIndex: 'moraCondonacion',
        width: 150,
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Total pago',
        dataIndex: 'totalPago',
        width: 130,
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      { title: 'Usuario', dataIndex: 'nombreUsuario', width: 130, ellipsis: true },
      {
        title: 'Solicitado',
        dataIndex: 'fecha',
        width: 120,
        render: (v: string) => formatFechaCorta(v),
      },
      {
        title: 'Estado',
        width: 100,
        render: () => <Tag color="gold">Pendiente</Tag>,
      },
      {
        title: '',
        width: 148,
        render: (_: unknown, row) => (
          <div style={{ display: 'flex', gap: 4, justifyContent: 'flex-end' }}>
            <Button
              type="link"
              size="small"
              onClick={(e) => {
                e.stopPropagation()
                navigate(
                  `/credito/consulta?personaId=${row.personaId}&creditoId=${row.creditoId}`,
                )
              }}
            >
              Aprobar
            </Button>
            <Button
              type="text"
              danger
              icon={<DeleteOutlined />}
              aria-label="Eliminar solicitud"
              onClick={(e) => {
                e.stopPropagation()
                Modal.confirm({
                  title: '¿Eliminar la solicitud de condonación?',
                  okText: 'Sí',
                  cancelText: 'No',
                  onOk: () => eliminar.mutateAsync(row.id),
                })
              }}
            />
          </div>
        ),
      },
    ],
    [eliminar, navigate],
  )

  return (
    <CredixCrudPage
      title="Solicitudes de condonación"
      subtitle="Bandeja de mora a condonar aún no aprobada. El administrador aprueba en la ficha del crédito."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Condonaciones' },
      ]}
      stats={stats}
      toolbar={
        <CredixCrudToolbar
          value={buscar}
          onChange={setBuscar}
          placeholder="Cliente, crédito, usuario…"
          hint="Doble clic en fila abre la ficha del crédito."
          filteredCount={filtrados.length}
          totalCount={rows.length}
          loading={listQuery.isFetching}
          onRefresh={() => void listQuery.refetch()}
        />
      }
    >
      {listQuery.isError ? (
        <Alert
          type="error"
          showIcon
          message={
            listQuery.error instanceof ApiError
              ? listQuery.error.message
              : 'No se pudo cargar la bandeja.'
          }
        />
      ) : (
        <CredixDataTable<CondonacionPendiente>
          mode="operacion"
          rowKey="id"
          loading={listQuery.isFetching}
          columns={columns}
          dataSource={filtrados}
          pagination={{ pageSize: 20, showSizeChanger: false, size: 'small' }}
          locale={{ emptyText: 'No hay solicitudes de condonación pendientes.' }}
          onRow={(row) => ({
            onDoubleClick: () =>
              navigate(`/credito/consulta?personaId=${row.personaId}&creditoId=${row.creditoId}`),
          })}
        />
      )}
    </CredixCrudPage>
  )
}
