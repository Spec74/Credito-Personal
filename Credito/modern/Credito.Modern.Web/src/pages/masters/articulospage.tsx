import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AutoComplete,
  Button,
  Checkbox,
  Col,
  Drawer,
  Form,
  Input,
  InputNumber,
  Row,
  Select,
  Space,
  Switch,
  Tag,
  Typography,
  Upload,
  message,
} from 'antd'
import { EditOutlined, PlusOutlined, ReloadOutlined, UploadOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { fetchModelos } from '../../api/modelos'
import { fetchTiposArticulo } from '../../api/tiposArticulo'
import {
  buscarArticulos,
  eliminarImagenArticulo,
  fetchArticuloDetalle,
  fetchArticuloImagenBlobUrl,
  fetchArticuloImagenes,
  fetchArticulosGestion,
  guardarArticulo,
  subirImagenArticulo,
  type ArticuloGestionRow,
} from '../../api/articulosCrud'
import { ApiError } from '../../api/errors'
import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { useCrudListStats } from '../../hooks/useCrudListStats'

const { Paragraph, Text, Title } = Typography

function ArticuloImagenThumb({
  articuloId,
  nombre,
  onDeleted,
}: {
  articuloId: number
  nombre: string
  onDeleted: () => void
}) {
  const [src, setSrc] = useState<string | null>(null)
  const eliminar = useMutation({
    mutationFn: () => eliminarImagenArticulo(articuloId, nombre),
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo eliminar')
        return
      }
      message.success('Imagen eliminada')
      onDeleted()
    },
  })

  useEffect(() => {
    let url: string | null = null
    void fetchArticuloImagenBlobUrl(articuloId, nombre)
      .then((u) => {
        url = u
        setSrc(u)
      })
      .catch(() => setSrc(null))
    return () => {
      if (url) URL.revokeObjectURL(url)
    }
  }, [articuloId, nombre])

  return (
    <Space direction="vertical" align="center">
      {src ? (
        <a href={src} target="_blank" rel="noreferrer">
          <img src={src} alt={nombre} style={{ maxHeight: 72, maxWidth: 100 }} />
        </a>
      ) : (
        <Text type="secondary">{nombre}</Text>
      )}
      <Button size="small" danger loading={eliminar.isPending} onClick={() => eliminar.mutate()}>
        Eliminar
      </Button>
    </Space>
  )
}

export function ArticulosPage() {
  const [modeloId, setModeloId] = useState<number | undefined>()
  const [tipoArticuloId, setTipoArticuloId] = useState<number | undefined>()
  const [filtro, setFiltro] = useState('')
  const [incluirInactivos, setIncluirInactivos] = useState(true)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [articuloId, setArticuloId] = useState(0)
  const [buscarTerm, setBuscarTerm] = useState('')
  const [form] = Form.useForm()
  const queryClient = useQueryClient()

  const modelosQuery = useQuery({ queryKey: ['modelos'], queryFn: () => fetchModelos() })
  const tiposQuery = useQuery({ queryKey: ['tipos-articulo'], queryFn: fetchTiposArticulo })

  const articulosQuery = useQuery({
    queryKey: ['articulos-gestion', modeloId, tipoArticuloId, incluirInactivos],
    queryFn: () => fetchArticulosGestion(incluirInactivos, modeloId, tipoArticuloId),
  })

  const imagenesQuery = useQuery({
    queryKey: ['articulo-imagenes', articuloId],
    queryFn: () => fetchArticuloImagenes(articuloId),
    enabled: articuloId >= 1,
  })

  const buscarQuery = useQuery({
    queryKey: ['articulos-buscar', buscarTerm],
    queryFn: () => buscarArticulos(buscarTerm, !incluirInactivos),
    enabled: buscarTerm.trim().length >= 2,
  })

  const guardar = useMutation({
    mutationFn: guardarArticulo,
    onSuccess: async (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo guardar')
        return
      }
      message.success('Artículo guardado')
      const id = res.id ?? articuloId
      if (id && id >= 1) setArticuloId(id)
      void queryClient.invalidateQueries({ queryKey: ['articulos-gestion'] })
      void queryClient.invalidateQueries({ queryKey: ['articulos'] })
    },
    onError: (e: unknown) =>
      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),
  })

  const subirImg = useMutation({
    mutationFn: (file: File) => subirImagenArticulo(articuloId, file),
    onSuccess: (res) => {
      if (!res.success) {
        message.error(res.mensaje ?? 'No se pudo subir')
        return
      }
      message.success('Imagen subida')
      void imagenesQuery.refetch()
    },
  })

  const filtradas = useMemo(() => {
    const lista = articulosQuery.data ?? []
    const q = filtro.trim().toLowerCase()
    if (!q) return lista
    return lista.filter(
      (a) =>
        a.denominacion.toLowerCase().includes(q) ||
        a.codArticulo.toLowerCase().includes(q) ||
        String(a.articuloId).includes(q),
    )
  }, [articulosQuery.data, filtro])

  const stats = useCrudListStats(filtradas, 'artículos')

  const cargarDetalle = async (id: number) => {
    const d = await fetchArticuloDetalle(id)
    setArticuloId(id)
    form.setFieldsValue({
      modeloId: d.modeloId ?? undefined,
      tipoArticuloId: d.tipoArticuloId ?? undefined,
      codArticulo: d.codArticulo,
      denominacion: d.denominacion,
      descripcion: d.descripcion ?? '',
      monto: d.monto ?? 0,
      descuento: d.descuento ?? 0,
      indPerecible: d.indPerecible ?? false,
      indImportado: d.indImportado ?? false,
      indCanjeable: d.indCanjeable ?? false,
      estado: d.estado,
    })
    setDrawerOpen(true)
    void queryClient.invalidateQueries({ queryKey: ['articulo-imagenes', id] })
  }

  const abrirNuevo = () => {
    setArticuloId(0)
    form.setFieldsValue({
      modeloId: modeloId ?? modelosQuery.data?.[0]?.modeloId,
      tipoArticuloId: tipoArticuloId ?? tiposQuery.data?.[0]?.tipoArticuloId,
      codArticulo: '',
      denominacion: '',
      descripcion: '',
      monto: 0,
      descuento: 0,
      indPerecible: false,
      indImportado: false,
      indCanjeable: false,
      estado: true,
    })
    setDrawerOpen(true)
  }

  const columns: ColumnsType<ArticuloGestionRow> = [
    { title: 'Id', dataIndex: 'articuloId', width: 70 },
    { title: 'Código', dataIndex: 'codArticulo', width: 100 },
    { title: 'Artículo', dataIndex: 'denominacion', ellipsis: true },
    { title: 'Modelo', dataIndex: 'modeloDenominacion', width: 120, ellipsis: true },
    { title: 'Precio', dataIndex: 'monto', width: 90, align: 'right' },
    {
      title: 'Estado',
      dataIndex: 'estado',
      width: 90,
      render: (v: boolean) => (v ? <Tag color="green">Activo</Tag> : <Tag>Inactivo</Tag>),
    },
    {
      title: '',
      key: 'acc',
      width: 90,
      render: (_, row) => (
        <Button size="small" icon={<EditOutlined />} onClick={() => void cargarDetalle(row.articuloId)}>
          Editar
        </Button>
      ),
    },
  ]

  return (
    <CredixCrudPage
      title="Artículos"
      subtitle="Gestión de artículos, precios en lista y carga de imágenes."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/maestros">Maestros</Link> },
        { title: 'Artículos' },
      ]}
      actions={
        <Space wrap size="small">
          <Link to="/maestros/marcas">Marcas</Link>
          <Link to="/maestros/modelos">Modelos</Link>
          <Link to="/maestros/tipos-articulo">Tipos</Link>
        </Space>
      }
      toolbar={
        <Space wrap>
          <AutoComplete
            style={{ minWidth: 280 }}
            placeholder="Buscar artículo por nombre..."
            value={buscarTerm}
            onChange={setBuscarTerm}
            onSelect={(v) => void cargarDetalle(Number(v))}
            options={(buscarQuery.data ?? []).map((a) => ({
              value: String(a.articuloId),
              label: `${a.articuloId} — ${a.denominacion}`,
            }))}
          />
          <Select
            allowClear
            placeholder="Modelo"
            style={{ minWidth: 180 }}
            value={modeloId}
            onChange={setModeloId}
            options={(modelosQuery.data ?? []).map((m) => ({
              value: m.modeloId,
              label: m.denominacion ?? `Modelo ${m.modeloId}`,
            }))}
          />
          <Select
            allowClear
            placeholder="Tipo"
            style={{ minWidth: 180 }}
            value={tipoArticuloId}
            onChange={setTipoArticuloId}
            options={(tiposQuery.data ?? []).map((t) => ({
              value: t.tipoArticuloId,
              label: t.denominacion,
            }))}
          />
          <Input.Search
            allowClear
            placeholder="Filtrar lista"
            style={{ width: '100%', maxWidth: 220 }}
            value={filtro}
            onChange={(e) => setFiltro(e.target.value)}
          />
          <Checkbox
            checked={incluirInactivos}
            onChange={(e) => setIncluirInactivos(e.target.checked)}
          >
            Incluir inactivos
          </Checkbox>
          <Button type="primary" icon={<PlusOutlined />} onClick={abrirNuevo}>
            Nuevo artículo
          </Button>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void articulosQuery.refetch()}
            loading={articulosQuery.isFetching}
          >
            Actualizar
          </Button>
        </Space>
      }
      extra={
        <Drawer
          title={articuloId >= 1 ? `Artículo #${articuloId}` : 'Nuevo artículo'}
          width={560}
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          extra={
            <Button
              type="primary"
              loading={guardar.isPending}
              onClick={() => form.submit()}
            >
              Guardar
            </Button>
          }
        >
          <Form
            form={form}
            layout="vertical"
            onFinish={(v) =>
              guardar.mutate({
                articuloId,
                modeloId: v.modeloId,
                tipoArticuloId: v.tipoArticuloId,
                codArticulo: (v.codArticulo ?? '').trim(),
                denominacion: v.denominacion.trim(),
                descripcion: v.descripcion?.trim() || null,
                monto: v.monto,
                descuento: v.descuento,
                indPerecible: v.indPerecible,
                indImportado: v.indImportado,
                indCanjeable: v.indCanjeable,
                estado: v.estado,
              })
            }
          >
            <Form.Item name="tipoArticuloId" label="Tipo artículo" rules={[{ required: true }]}>
              <Select
                options={(tiposQuery.data ?? []).map((t) => ({
                  value: t.tipoArticuloId,
                  label: t.denominacion,
                }))}
              />
            </Form.Item>
            <Form.Item name="modeloId" label="Modelo" rules={[{ required: true }]}>
              <Select
                options={(modelosQuery.data ?? []).map((m) => ({
                  value: m.modeloId,
                  label: m.denominacion,
                }))}
                onChange={(mid) => {
                  const label = modelosQuery.data?.find((m) => m.modeloId === mid)?.denominacion
                  if (label && articuloId < 1) {
                    const actual = form.getFieldValue('denominacion') as string
                    if (!actual?.trim()) form.setFieldValue('denominacion', label)
                  }
                }}
              />
            </Form.Item>
            <Form.Item name="codArticulo" label="Código artículo">
              <Input maxLength={20} />
            </Form.Item>
            <Form.Item name="denominacion" label="Denominación" rules={[{ required: true }]}>
              <Input maxLength={200} />
            </Form.Item>
            <Form.Item name="descripcion" label="Descripción">
              <Input.TextArea rows={2} maxLength={250} />
            </Form.Item>
            <Row gutter={12}>
              <Col span={12}>
                <Form.Item name="monto" label="Precio" rules={[{ required: true }]}>
                  <InputNumber min={0} precision={2} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="descuento" label="Descuento">
                  <InputNumber min={0} precision={2} style={{ width: '100%' }} />
                </Form.Item>
              </Col>
            </Row>
            <Space wrap>
              <Form.Item name="indPerecible" valuePropName="checked">
                <Checkbox>Perecible</Checkbox>
              </Form.Item>
              <Form.Item name="indImportado" valuePropName="checked">
                <Checkbox>Importado</Checkbox>
              </Form.Item>
              <Form.Item name="indCanjeable" valuePropName="checked">
                <Checkbox>Canjeable</Checkbox>
              </Form.Item>
              <Form.Item name="estado" label="Activo" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Space>
          </Form>

          {articuloId >= 1 ? (
            <>
              <Title level={5} style={{ marginTop: 24 }}>
                Imágenes
              </Title>
              <Upload
                showUploadList={false}
                beforeUpload={(file) => {
                  subirImg.mutate(file)
                  return false
                }}
              >
                <Button icon={<UploadOutlined />} loading={subirImg.isPending}>
                  Subir imagen
                </Button>
              </Upload>
              <Space wrap style={{ marginTop: 16 }}>
                {(imagenesQuery.data?.archivos ?? []).map((nombre) => (
                  <ArticuloImagenThumb
                    key={nombre}
                    articuloId={articuloId}
                    nombre={nombre}
                    onDeleted={() => void imagenesQuery.refetch()}
                  />
                ))}
              </Space>
            </>
          ) : (
            <Paragraph type="secondary" style={{ marginTop: 16 }}>
              Guarde el artículo primero para poder subir imágenes.
            </Paragraph>
          )}
        </Drawer>
      }
    >
      <CredixDataTable<ArticuloGestionRow>
        rowKey="articuloId"
        columns={columns}
        dataSource={filtradas}
        loading={articulosQuery.isLoading}
        pagination={{ pageSize: 20, showTotal: (t) => `${t} artículo(s)` }}
        locale={{ emptyText: 'No hay artículos' }}
      />
    </CredixCrudPage>
  )
}
