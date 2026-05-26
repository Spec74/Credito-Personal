import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Form,
  Input,
  Modal,
  Space,
  Switch,
  Tag,
  message,
} from 'antd'
import { EditOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  activarTipoArticulo,
  fetchTiposArticuloGestion,
  guardarTipoArticulo,
  type TipoArticuloGestionRow,
} from '../../api/maestrosCrud'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { useCrudListStats } from '../../hooks/useCrudListStats'

export function TipoArticuloPage() {
  const [filtro, setFiltro] = useState('')
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<TipoArticuloGestionRow | null>(null)
  const [form] = Form.useForm<{
    denominacion: string
    descripcion?: string
    estado: boolean
  }>()
  const queryClient = useQueryClient()

  const tiposQuery = useQuery({
    queryKey: ['tipos-articulo-gestion', incluirInactivos],
    queryFn: () => fetchTiposArticuloGestion(incluirInactivos),
  })

  const guardar = useMutation({
    mutationFn: guardarTipoArticulo,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Tipo guardado')
      setModalOpen(false)
      void queryClient.invalidateQueries({ queryKey: ['tipos-articulo-gestion'] })
      void queryClient.invalidateQueries({ queryKey: ['tipos-articulo'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),
  })

  const activar = useMutation({
    mutationFn: activarTipoArticulo,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo cambiar estado')
        return
      }
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['tipos-articulo-gestion'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error'),
  })

  const filtrados = useMemo(() => {
    const lista = tiposQuery.data ?? []
    const q = filtro.trim().toLowerCase()
    if (!q) return lista
    return lista.filter(
      (t) =>
        (t.denominacion ?? '').toLowerCase().includes(q) ||
        (t.descripcion?.toLowerCase().includes(q) ?? false) ||
        String(t.tipoArticuloId).includes(q),
    )
  }, [tiposQuery.data, filtro])

  const stats = useCrudListStats(filtrados, 'tipos de artículo')

  const openCreate = () => {
    setEditing(null)
    form.setFieldsValue({ denominacion: '', descripcion: '', estado: true })
    setModalOpen(true)
  }

  const openEdit = (row: TipoArticuloGestionRow) => {
    setEditing(row)
    form.setFieldsValue({
      denominacion: row.denominacion ?? '',
      descripcion: row.descripcion ?? '',
      estado: row.estado,
    })
    setModalOpen(true)
  }

  const columns: ColumnsType<TipoArticuloGestionRow> = [
    { title: 'Id', dataIndex: 'tipoArticuloId', width: 80 },
    { title: 'Tipo', dataIndex: 'denominacion' },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 100,
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
                title: row.estado ? '¿Desactivar tipo?' : '¿Activar tipo?',
                onOk: () => activar.mutateAsync(row.tipoArticuloId),
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
      title="Tipos de artículo"
      subtitle="Alta, edición y activación de tipos de artículo."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/maestros">Maestros</Link> },
        { title: 'Tipos de artículo' },
      ]}
      actions={
        <Space wrap size="small">
          <Link to="/maestros/marcas">Marcas</Link>
          <Link to="/maestros/modelos">Modelos</Link>
          <Link to="/maestros/articulos">Artículos</Link>
        </Space>
      }
      toolbar={
        <Space wrap>
          <Input.Search
            allowClear
            placeholder="Buscar por nombre o id"
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
            Nuevo tipo
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void tiposQuery.refetch()}
            loading={tiposQuery.isFetching}
          >
            Actualizar
          </Button>
        </Space>
      }
      extra={
        <Modal
          title={editing ? `Editar tipo #${editing.tipoArticuloId}` : 'Nuevo tipo'}
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
                tipoArticuloId: editing?.tipoArticuloId ?? 0,
                denominacion: v.denominacion.trim(),
                descripcion: v.descripcion?.trim() || null,
                estado: v.estado,
              })
            }
          >
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
      <CredixDataTable<TipoArticuloGestionRow>
        rowKey="tipoArticuloId"
        columns={columns}
        dataSource={filtrados}
        loading={tiposQuery.isLoading}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} tipo(s)` }}
        locale={{ emptyText: 'No hay tipos de artículo' }}
      />
    </CredixCrudPage>
  )
}
