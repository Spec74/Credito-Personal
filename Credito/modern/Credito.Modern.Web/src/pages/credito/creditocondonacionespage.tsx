import { useMemo } from 'react'
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
import { CredixCrudPage, CredixDataTable, type CredixStatItem } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'

export function CreditoCondonacionesPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
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

  const rows = listQuery.data ?? []
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
      { title: 'Usuario', dataIndex: 'nombreUsuario', width: 140, ellipsis: true },
      {
        title: 'Estado',
        width: 110,
        render: () => <Tag color="gold">Pendiente</Tag>,
      },
      {
        title: '',
        width: 56,
        render: (_: unknown, row) => (
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
    >
      {listQuery.isError ? (
        <Alert
          type="error"
          showIcon
          message={listQuery.error instanceof ApiError ? listQuery.error.message : 'No se pudo cargar la bandeja.'}
        />
      ) : (
        <CredixDataTable<CondonacionPendiente>
          mode="operacion"
          rowKey="id"
          loading={listQuery.isFetching}
          columns={columns}
          dataSource={rows}
          pagination={false}
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
