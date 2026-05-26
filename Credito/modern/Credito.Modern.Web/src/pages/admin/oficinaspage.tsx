import { startTransition, useMemo, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Input,
  Modal,
  Space,
  Tag,
  message,
} from 'antd'
import { EditOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { fetchCajaGestores } from '../../api/cajaMaestro'
import {
  activarOficina,
  fetchOficinasGestion,
  type OficinaGestionRow,
} from '../../api/maestrosCrud'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { hasValidCoordinates } from '../../config/googleMaps'
import { useCrudListStats } from '../../hooks/useCrudListStats'
import { mantenimientoOficinasBreadcrumb } from '../../utils/mantenimientoBreadcrumbs'
import { OficinaFormModal } from './OficinaFormModal'

export function OficinasPage() {
  const location = useLocation()
  const isMantenimiento = location.pathname.startsWith('/mantenimiento/')

  const [buscar, setBuscar] = useState('')
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<OficinaGestionRow | null>(null)
  const queryClient = useQueryClient()

  const gestoresQuery = useQuery({
    queryKey: ['caja-gestores'],
    queryFn: fetchCajaGestores,
  })

  const gestorLabelById = useMemo(() => {
    const map = new Map<number, string>()
    for (const g of gestoresQuery.data ?? []) {
      map.set(g.usuarioId, g.nombreCompleto)
    }
    return map
  }, [gestoresQuery.data])

  const query = useQuery({
    queryKey: ['oficinas-gestion', incluirInactivos, buscar],
    queryFn: () => fetchOficinasGestion({ incluirInactivos, buscar }),
  })

  const activar = useMutation({
    mutationFn: activarOficina,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['oficinas-gestion'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error'),
  })

  const rows = useMemo(() => query.data ?? [], [query.data])
  const stats = useCrudListStats(rows, 'oficinas')

  const openCreate = () => {
    setEditing(null)
    startTransition(() => setModalOpen(true))
  }

  const openEdit = (row: OficinaGestionRow) => {
    setEditing(row)
    startTransition(() => setModalOpen(true))
  }

  const handleModalClose = () => {
    setModalOpen(false)
    setEditing(null)
  }

  const handleSaved = () => {
    void queryClient.invalidateQueries({ queryKey: ['oficinas-gestion'] })
    void queryClient.invalidateQueries({ queryKey: ['oficinas'] })
  }

  const columns: ColumnsType<OficinaGestionRow> = [
    { title: 'Id', dataIndex: 'oficinaId', width: 70 },
    { title: 'Denominación', dataIndex: 'denominacion', ellipsis: true },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
    { title: 'Teléfono', dataIndex: 'telefono', width: 110 },
    {
      title: 'Responsable',
      dataIndex: 'usuarioAsignadoId',
      width: 160,
      ellipsis: true,
      render: (id: number) =>
        id > 0 ? gestorLabelById.get(id) ?? `Usuario ${id}` : '—',
    },
    {
      title: 'GPS',
      key: 'gps',
      width: 90,
      render: (_, row) =>
        hasValidCoordinates(row.latitud, row.longitud) ? (
          <Tag color="blue">Sí</Tag>
        ) : (
          '—'
        ),
    },
    {
      title: 'Principal',
      dataIndex: 'indPrincipal',
      width: 90,
      render: (v: boolean) => (v ? <Tag color="blue">Sí</Tag> : '—'),
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 90,
      render: (v: boolean) => (v ? <Tag color="green">Activo</Tag> : <Tag>Inactivo</Tag>),
    },
    {
      title: 'Acciones',
      key: 'acc',
      width: 200,
      render: (_, row) => (
        <Space wrap size="small">
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(row)}>
            Editar
          </Button>
          <Button
            size="small"
            loading={activar.isPending}
            onClick={() =>
              Modal.confirm({
                title: row.estado ? '¿Desactivar oficina?' : '¿Activar oficina?',
                onOk: () => activar.mutateAsync(row.oficinaId),
              })
            }
          >
            {row.estado ? 'Desactivar' : 'Activar'}
          </Button>
        </Space>
      ),
    },
  ]

  const breadcrumb = isMantenimiento
    ? mantenimientoOficinasBreadcrumb()
    : [
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/admin">Administración</Link> },
        { title: 'Oficinas' },
      ]

  return (
    <CredixCrudPage
      title={isMantenimiento ? 'Mantenimiento de oficinas' : 'Oficinas'}
      subtitle="Alta, edición y activación de oficinas (incluye bóveda al crear)."
      stats={stats}
      breadcrumb={breadcrumb}
      actions={
        isMantenimiento ? undefined : (
          <Space wrap size="small">
            <Link to="/admin/usuarios">Usuarios</Link>
            <Link to="/admin/roles">Roles</Link>
            <Link to="/maestros/almacenes">Almacenes</Link>
          </Space>
        )
      }
      toolbar={
        <Space wrap>
          <Input.Search
            allowClear
            placeholder="Buscar denominación"
            style={{ width: 260 }}
            value={buscar}
            onChange={(e) => setBuscar(e.target.value)}
            onSearch={() => void query.refetch()}
          />
          <Checkbox
            checked={incluirInactivos}
            onChange={(e) => setIncluirInactivos(e.target.checked)}
          >
            Incluir inactivos
          </Checkbox>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Nueva oficina
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void query.refetch()}
            loading={query.isFetching}
          >
            Actualizar
          </Button>
        </Space>
      }
      extra={
        <OficinaFormModal
          open={modalOpen}
          editing={editing}
          onClose={handleModalClose}
          onSaved={handleSaved}
        />
      }
    >
      <CredixDataTable<OficinaGestionRow>
        rowKey="oficinaId"
        columns={columns}
        dataSource={rows}
        loading={query.isLoading}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} oficina(s)` }}
        scroll={{ x: 1100 }}
        locale={{ emptyText: 'No hay oficinas' }}
      />
    </CredixCrudPage>
  )
}
