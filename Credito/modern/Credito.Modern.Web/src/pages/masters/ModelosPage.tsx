import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Checkbox,
  Form,
  Input,
  Modal,
  Select,
  Space,
  Switch,
  Tag,
  message,
} from 'antd'
import { EditOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { fetchMarcas } from '../../api/marcas'
import {
  activarModelo,
  fetchModelosGestion,
  guardarModelo,
  type ModeloGestionRow,
} from '../../api/maestrosCrud'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { useCrudListStats } from '../../hooks/useCrudListStats'

export function ModelosPage() {
  const [marcaId, setMarcaId] = useState<number | undefined>()
  const [filtro, setFiltro] = useState('')
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<ModeloGestionRow | null>(null)
  const [form] = Form.useForm<{
    denominacion: string
    marcaId: number
    estado: boolean
  }>()
  const queryClient = useQueryClient()

  const marcasQuery = useQuery({
    queryKey: ['marcas'],
    queryFn: fetchMarcas,
  })

  const modelosQuery = useQuery({
    queryKey: ['modelos-gestion', marcaId, incluirInactivos],
    queryFn: () => fetchModelosGestion(incluirInactivos, marcaId),
  })

  const guardar = useMutation({
    mutationFn: guardarModelo,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Modelo guardado')
      setModalOpen(false)
      void queryClient.invalidateQueries({ queryKey: ['modelos-gestion'] })
      void queryClient.invalidateQueries({ queryKey: ['modelos'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),
  })

  const activar = useMutation({
    mutationFn: activarModelo,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'Error')
        return
      }
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['modelos-gestion'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error'),
  })

  const filtradas = useMemo(() => {
    const lista = modelosQuery.data ?? []
    const q = filtro.trim().toLowerCase()
    if (!q) return lista
    return lista.filter(
      (m) =>
        m.denominacion?.toLowerCase().includes(q) ||
        String(m.modeloId).includes(q) ||
        (m.marcaDenominacion ?? '').toLowerCase().includes(q),
    )
  }, [modelosQuery.data, filtro])

  const stats = useCrudListStats(filtradas, 'modelos')

  const openCreate = () => {
    setEditing(null)
    form.setFieldsValue({
      denominacion: '',
      marcaId: marcaId ?? marcasQuery.data?.[0]?.marcaId ?? 0,
      estado: true,
    })
    setModalOpen(true)
  }

  const openEdit = (row: ModeloGestionRow) => {
    setEditing(row)
    form.setFieldsValue({
      denominacion: row.denominacion,
      marcaId: row.marcaId ?? 0,
      estado: row.estado,
    })
    setModalOpen(true)
  }

  const columns: ColumnsType<ModeloGestionRow> = [
    { title: 'Id', dataIndex: 'modeloId', width: 90 },
    { title: 'Modelo', dataIndex: 'denominacion' },
    { title: 'Marca', dataIndex: 'marcaDenominacion', width: 160 },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 110,
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
                title: row.estado ? '¿Desactivar modelo?' : '¿Activar modelo?',
                onOk: () => activar.mutateAsync(row.modeloId),
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
      title="Modelos"
      subtitle="Alta, edición y activación de modelos por marca."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/maestros">Maestros</Link> },
        { title: 'Modelos' },
      ]}
      actions={
        <Space wrap size="small">
          <Link to="/maestros/marcas">Marcas</Link>
          <Link to="/maestros/tipos-articulo">Tipos</Link>
          <Link to="/maestros/articulos">Artículos</Link>
        </Space>
      }
      toolbar={
        <Space wrap>
          <Select
            allowClear
            placeholder="Filtrar por marca"
            style={{ minWidth: 220 }}
            value={marcaId}
            onChange={(v) => setMarcaId(v)}
            loading={marcasQuery.isLoading}
            options={(marcasQuery.data ?? []).map((m) => ({
              value: m.marcaId,
              label: m.denominacion ?? `Marca ${m.marcaId}`,
            }))}
          />
          <Input.Search
            allowClear
            placeholder="Buscar modelo"
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
            Nuevo modelo
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void modelosQuery.refetch()}
            loading={modelosQuery.isFetching}
          >
            Actualizar
          </Button>
        </Space>
      }
      extra={
        <Modal
          title={editing ? `Editar modelo #${editing.modeloId}` : 'Nuevo modelo'}
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
                modeloId: editing?.modeloId ?? 0,
                marcaId: v.marcaId,
                denominacion: v.denominacion.trim(),
                estado: v.estado,
              })
            }
          >
            <Form.Item name="marcaId" label="Marca" rules={[{ required: true }]}>
              <Select
                options={(marcasQuery.data ?? []).map((m) => ({
                  value: m.marcaId,
                  label: m.denominacion,
                }))}
              />
            </Form.Item>
            <Form.Item name="denominacion" label="Denominación" rules={[{ required: true }]}>
              <Input />
            </Form.Item>
            <Form.Item name="estado" label="Activo" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Form>
        </Modal>
      }
    >
      <CredixDataTable<ModeloGestionRow>
        rowKey="modeloId"
        columns={columns}
        dataSource={filtradas}
        loading={modelosQuery.isLoading}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} modelo(s)` }}
        locale={{ emptyText: 'No hay modelos' }}
      />
    </CredixCrudPage>
  )
}
