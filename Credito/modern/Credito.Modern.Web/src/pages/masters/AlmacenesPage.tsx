import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Switch,
  Tag,
  message,
} from 'antd'
import { EditOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { fetchOficinas } from '../../api/oficinas'
import {
  activarAlmacen,
  fetchAlmacenesGestion,
  guardarAlmacen,
  type AlmacenGestionRow,
} from '../../api/maestrosCrud'
import { useAuth } from '../../auth/useAuth'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { useCrudListStats } from '../../hooks/useCrudListStats'

export function AlmacenesPage() {
  const { session } = useAuth()
  const [oficinaId, setOficinaId] = useState<number | undefined>(session?.oficinaId)
  const [filtro, setFiltro] = useState('')
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<AlmacenGestionRow | null>(null)
  const [form] = Form.useForm<{
    denominacion: string
    descripcion?: string
    oficinaId: number
    estado: boolean
  }>()
  const queryClient = useQueryClient()

  const oficinasQuery = useQuery({
    queryKey: ['oficinas'],
    queryFn: fetchOficinas,
  })

  const almacenesQuery = useQuery({
    queryKey: ['almacenes-gestion', oficinaId, incluirInactivos],
    queryFn: () => fetchAlmacenesGestion(incluirInactivos, oficinaId),
  })

  const guardar = useMutation({
    mutationFn: guardarAlmacen,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Almacén guardado')
      setModalOpen(false)
      void queryClient.invalidateQueries({ queryKey: ['almacenes-gestion'] })
      void queryClient.invalidateQueries({ queryKey: ['almacenes'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),
  })

  const activar = useMutation({
    mutationFn: activarAlmacen,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['almacenes-gestion'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error'),
  })

  const filtradas = useMemo(() => {
    const lista = almacenesQuery.data ?? []
    const q = filtro.trim().toLowerCase()
    if (!q) return lista
    return lista.filter(
      (a) =>
        a.denominacion.toLowerCase().includes(q) ||
        String(a.almacenId).includes(q) ||
        (a.oficinaDenominacion ?? '').toLowerCase().includes(q),
    )
  }, [almacenesQuery.data, filtro])

  const stats = useCrudListStats(filtradas, 'almacenes')

  const openCreate = () => {
    setEditing(null)
    form.setFieldsValue({
      denominacion: '',
      descripcion: '',
      oficinaId: oficinaId ?? oficinasQuery.data?.[0]?.oficinaId ?? 0,
      estado: true,
    })
    setModalOpen(true)
  }

  const openEdit = (row: AlmacenGestionRow) => {
    setEditing(row)
    form.setFieldsValue({
      denominacion: row.denominacion,
      descripcion: row.descripcion ?? '',
      oficinaId: row.oficinaId,
      estado: row.estado,
    })
    setModalOpen(true)
  }

  const columns: ColumnsType<AlmacenGestionRow> = [
    { title: 'Id', dataIndex: 'almacenId', width: 80 },
    { title: 'Oficina', dataIndex: 'oficinaDenominacion', width: 140 },
    { title: 'Almacén', dataIndex: 'denominacion' },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
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
                title: row.estado ? '¿Desactivar almacén?' : '¿Activar almacén?',
                onOk: () => activar.mutateAsync(row.almacenId),
              })
            }
          >
            {row.estado ? 'Desactivar' : 'Activar'}
          </Button>
        </Space>
      ),
    },
  ]

  return (
    <CredixCrudPage
      title="Almacenes"
      subtitle="Alta, edición y activación de almacenes por oficina."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/maestros">Maestros</Link> },
        { title: 'Almacenes' },
      ]}
      actions={
        <Space wrap size="small">
          <Link to="/mantenimiento/oficinas">Oficinas</Link>
          <Link to="/maestros/articulos">Artículos</Link>
        </Space>
      }
      toolbar={
        <Space wrap>
          <InputNumber
            min={1}
            placeholder="Oficina"
            value={oficinaId}
            onChange={(v) => setOficinaId(v ?? undefined)}
            style={{ width: 120 }}
          />
          <Button type="link" onClick={() => setOficinaId(undefined)}>
            Todas
          </Button>
          <Input.Search
            allowClear
            placeholder="Buscar almacén"
            style={{ width: '100%', maxWidth: 280 }}
            value={filtro}
            onChange={(e) => setFiltro(e.target.value)}
          />
          <Checkbox
            checked={incluirInactivos}
            onChange={(e) => setIncluirInactivos(e.target.checked)}
          >
            Incluir inactivos
          </Checkbox>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Nuevo almacén
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void almacenesQuery.refetch()}
            loading={almacenesQuery.isFetching}
          >
            Actualizar
          </Button>
        </Space>
      }
      extra={
        <Modal
          title={editing ? `Editar almacén #${editing.almacenId}` : 'Nuevo almacén'}
          open={modalOpen}
          onCancel={() => setModalOpen(false)}
          onOk={() => form.submit()}
          confirmLoading={guardar.isPending}
          destroyOnClose
        >
          <Form
            form={form}
            layout="vertical"
            onFinish={(v) =>
              guardar.mutate({
                almacenId: editing?.almacenId ?? 0,
                oficinaId: v.oficinaId,
                denominacion: v.denominacion.trim(),
                descripcion: v.descripcion?.trim() || null,
                estado: v.estado,
              })
            }
          >
            <Form.Item name="oficinaId" label="Oficina" rules={[{ required: true }]}>
              <Select
                options={(oficinasQuery.data ?? []).map((o) => ({
                  value: o.oficinaId,
                  label: o.denominacion ?? `Oficina ${o.oficinaId}`,
                }))}
              />
            </Form.Item>
            <Form.Item name="denominacion" label="Denominación" rules={[{ required: true }]}>
              <Input />
            </Form.Item>
            <Form.Item name="descripcion" label="Descripción">
              <Input.TextArea rows={2} />
            </Form.Item>
            <Form.Item name="estado" label="Activo" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Form>
        </Modal>
      }
    >
      <CredixDataTable<AlmacenGestionRow>
        rowKey="almacenId"
        columns={columns}
        dataSource={filtradas}
        loading={almacenesQuery.isLoading}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} almacén(es)` }}
        locale={{ emptyText: 'No hay almacenes' }}
      />
    </CredixCrudPage>
  )
}
