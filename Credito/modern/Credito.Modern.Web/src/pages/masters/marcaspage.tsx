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
import { DownloadOutlined, EditOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import {
  activarMarca,
  fetchMarcasGestion,
  guardarMarca,
  type MarcaGestionRow,
} from '../../api/maestrosCrud'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { useCrudListStats } from '../../hooks/useCrudListStats'

function downloadCsv(rows: MarcaGestionRow[]) {
  const header = 'MarcaId,Denominacion,Estado'
  const lines = rows.map(
    (r) =>
      `${r.marcaId},"${(r.denominacion ?? '').replace(/"/g, '""')}",${r.estado ? 1 : 0}`,
  )
  const blob = new Blob([`\uFEFF${header}\n${lines.join('\n')}`], {
    type: 'text/csv;charset=utf-8',
  })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'marcas.csv'
  a.click()
  URL.revokeObjectURL(url)
}

export function MarcasPage() {
  const [filtro, setFiltro] = useState('')
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<MarcaGestionRow | null>(null)
  const [form] = Form.useForm<{ denominacion: string; estado: boolean }>()
  const queryClient = useQueryClient()

  const marcasQuery = useQuery({
    queryKey: ['marcas-gestion', incluirInactivos],
    queryFn: () => fetchMarcasGestion(incluirInactivos),
  })

  const guardar = useMutation({
    mutationFn: guardarMarca,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Marca guardada')
      setModalOpen(false)
      setEditing(null)
      void queryClient.invalidateQueries({ queryKey: ['marcas-gestion'] })
      void queryClient.invalidateQueries({ queryKey: ['marcas'] })
    },
    onError: (e: unknown) => {
      message.error(e instanceof ApiError ? e.message : 'Error al guardar')
    },
  })

  const activar = useMutation({
    mutationFn: activarMarca,
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo cambiar estado')
        return
      }
      message.success('Estado actualizado')
      void queryClient.invalidateQueries({ queryKey: ['marcas-gestion'] })
      void queryClient.invalidateQueries({ queryKey: ['marcas'] })
    },
    onError: (e: unknown) => {
      message.error(e instanceof ApiError ? e.message : 'Error al activar/desactivar')
    },
  })

  const filtradas = useMemo(() => {
    const lista = marcasQuery.data ?? []
    const q = filtro.trim().toLowerCase()
    if (!q) return lista
    return lista.filter(
      (m) =>
        m.denominacion?.toLowerCase().includes(q) ||
        String(m.marcaId).includes(q),
    )
  }, [marcasQuery.data, filtro])

  const stats = useCrudListStats(filtradas, 'marcas')

  const openCreate = () => {
    setEditing(null)
    form.setFieldsValue({ denominacion: '', estado: true })
    setModalOpen(true)
  }

  const openEdit = (row: MarcaGestionRow) => {
    setEditing(row)
    form.setFieldsValue({
      denominacion: row.denominacion ?? '',
      estado: row.estado,
    })
    setModalOpen(true)
  }

  const columns: ColumnsType<MarcaGestionRow> = [
    {
      title: 'Id',
      dataIndex: 'marcaId',
      width: 90,
      sorter: (a, b) => a.marcaId - b.marcaId,
    },
    {
      title: 'Marca',
      dataIndex: 'denominacion',
      sorter: (a, b) =>
        (a.denominacion ?? '').localeCompare(b.denominacion ?? '', 'es'),
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 110,
      render: (activo: boolean) =>
        activo ? <Tag color="green">Activo</Tag> : <Tag>Inactivo</Tag>,
    },
    {
      title: 'Acciones',
      key: 'acciones',
      width: 200,
      render: (_, row) => (
        <Space wrap size="small">
          <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(row)}>
            Editar
          </Button>
          <Button
            size="small"
            loading={activar.isPending}
            onClick={() => {
              Modal.confirm({
                title: row.estado ? '¿Desactivar marca?' : '¿Activar marca?',
                onOk: () => activar.mutateAsync(row.marcaId),
              })
            }}
          >
            {row.estado ? 'Desactivar' : 'Activar'}
          </Button>
        </Space>
      ),
    },
  ]

  return (
    <CredixCrudPage
      title="Marcas"
      subtitle="Alta, edición y activación de marcas."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/maestros">Maestros</Link> },
        { title: 'Marcas' },
      ]}
      actions={
        <Space wrap size="small">
          <Link to="/maestros/modelos">Modelos</Link>
          <Link to="/maestros/tipos-articulo">Tipos</Link>
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
            Nueva marca
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void marcasQuery.refetch()}
            loading={marcasQuery.isFetching}
          >
            Actualizar
          </Button>
          <Button
            icon={<DownloadOutlined />}
            disabled={filtradas.length === 0}
            onClick={() => downloadCsv(filtradas)}
          >
            CSV
          </Button>
        </Space>
      }
      extra={
        <Modal
          title={editing ? `Editar marca #${editing.marcaId}` : 'Nueva marca'}
          open={modalOpen}
          onCancel={() => setModalOpen(false)}
          onOk={() => form.submit()}
          confirmLoading={guardar.isPending}
          destroyOnClose
        >
          <Form
            form={form}
            layout="vertical"
            onFinish={(values) => {
              guardar.mutate({
                marcaId: editing?.marcaId ?? 0,
                denominacion: values.denominacion.trim(),
                estado: values.estado,
              })
            }}
          >
            <Form.Item
              name="denominacion"
              label="Denominación"
              rules={[{ required: true, message: 'Obligatorio' }]}
            >
              <Input maxLength={200} />
            </Form.Item>
            <Form.Item name="estado" label="Activo" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Form>
        </Modal>
      }
    >
      <CredixDataTable<MarcaGestionRow>
        rowKey="marcaId"
        columns={columns}
        dataSource={filtradas}
        loading={marcasQuery.isLoading}
        pagination={{
          pageSize: 20,
          showTotal: (total) => `${total} marca(s)`,
        }}
        locale={{ emptyText: 'No hay marcas' }}
      />
    </CredixCrudPage>
  )
}
