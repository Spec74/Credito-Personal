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

  Space,

  Switch,

  Tag,

  message,

} from 'antd'

import { DownloadOutlined, EditOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'

import type { ColumnsType } from 'antd/es/table'

import {

  activarListaPrecio,

  fetchListaPreciosGestion,

  guardarListaPrecio,

  type ListaPrecioGestionRow,

} from '../../api/maestrosCrud'

import { downloadTableCsv } from '../../utils/downloadTableCsv'

import { formatMoney } from '../../utils/formatMoney'

import { ApiError } from '../../api/errors'

import { CredixCrudPage, CredixDataTable } from '../../components/credix'
import { useCrudListStats } from '../../hooks/useCrudListStats'



function downloadCsv(rows: ListaPrecioGestionRow[]) {

  downloadTableCsv('lista-precios.csv', [

    [

      'ListaPrecioId',

      'ArticuloId',

      'Monto',

      'Descuento',

      'Puntos',

      'PuntosCanje',

      'Estado',

    ],

    ...rows.map((r) => [

      String(r.listaPrecioId),

      String(r.articuloId),

      String(r.monto),

      String(r.descuento),

      r.puntos != null ? String(r.puntos) : '',

      r.puntosCanje != null ? String(r.puntosCanje) : '',

      r.estado ? '1' : '0',

    ]),

  ])

}



export function ListaPreciosPage() {

  const [articuloId, setArticuloId] = useState<number | undefined>()

  const [filtro, setFiltro] = useState('')

  const [incluirInactivos, setIncluirInactivos] = useState(true)

  const [modalOpen, setModalOpen] = useState(false)

  const [editing, setEditing] = useState<ListaPrecioGestionRow | null>(null)

  const [form] = Form.useForm<{

    articuloId: number

    monto: number

    descuento: number

    puntos?: number

    puntosCanje?: number

    estado: boolean

  }>()

  const queryClient = useQueryClient()



  const listaQuery = useQuery({

    queryKey: ['lista-precios-gestion', articuloId, incluirInactivos],

    queryFn: () => fetchListaPreciosGestion(incluirInactivos, articuloId),

  })



  const guardar = useMutation({

    mutationFn: guardarListaPrecio,

    onSuccess: (res) => {

      if (!res.success) {

        message.error(res.mensaje ?? 'No se pudo guardar')

        return

      }

      message.success('Precio guardado')

      setModalOpen(false)

      void queryClient.invalidateQueries({ queryKey: ['lista-precios-gestion'] })

      void queryClient.invalidateQueries({ queryKey: ['lista-precios'] })

    },

    onError: (e: unknown) =>

      message.error(e instanceof ApiError ? e.message : 'Error al guardar'),

  })



  const activar = useMutation({

    mutationFn: activarListaPrecio,

    onSuccess: (res) => {

      if (!res.success) {

        message.error(res.mensaje ?? 'Error')

        return

      }

      message.success('Estado actualizado')

      void queryClient.invalidateQueries({ queryKey: ['lista-precios-gestion'] })

    },

    onError: (e: unknown) =>

      message.error(e instanceof ApiError ? e.message : 'Error'),

  })



  const filtradas = useMemo(() => {

    const lista = listaQuery.data ?? []

    const q = filtro.trim().toLowerCase()

    if (!q) return lista

    return lista.filter(

      (p) =>

        String(p.listaPrecioId).includes(q) ||

        String(p.articuloId).includes(q),

    )

  }, [listaQuery.data, filtro])

  const stats = useCrudListStats(filtradas, 'precios en lista')



  const columns: ColumnsType<ListaPrecioGestionRow> = [

    { title: 'Id lista', dataIndex: 'listaPrecioId', width: 90 },

    { title: 'Artículo', dataIndex: 'articuloId', width: 90 },

    {

      title: 'Monto',

      dataIndex: 'monto',

      width: 110,

      align: 'right',

      render: formatMoney,

    },

    {

      title: 'Descuento',

      dataIndex: 'descuento',

      width: 100,

      align: 'right',

      render: formatMoney,

    },

    { title: 'Puntos', dataIndex: 'puntos', width: 80, align: 'right' },

    { title: 'Puntos canje', dataIndex: 'puntosCanje', width: 100, align: 'right' },

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

        <Space>

          <Button

            size="small"

            icon={<EditOutlined />}

            onClick={() => {

              setEditing(row)

              form.setFieldsValue({

                articuloId: row.articuloId,

                monto: row.monto,

                descuento: row.descuento,

                puntos: row.puntos ?? undefined,

                puntosCanje: row.puntosCanje ?? undefined,

                estado: row.estado,

              })

              setModalOpen(true)

            }}

          >

            Editar

          </Button>

          <Button

            size="small"

            onClick={() =>

              Modal.confirm({

                title: row.estado ? '¿Desactivar?' : '¿Activar?',

                onOk: () => activar.mutateAsync(row.listaPrecioId),

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

      title="Lista de precios"

      subtitle="Precios, descuentos y puntos por artículo."

      stats={stats}

      breadcrumb={[

        { title: <Link to="/inicio">Inicio</Link> },

        { title: <Link to="/ventas">Ventas</Link> },

        { title: 'Lista de precios' },

      ]}

      actions={<Link to="/maestros/articulos">Ver artículos</Link>}

      toolbar={

        <Space wrap>

          <InputNumber

            min={1}

            placeholder="Artículo id"

            value={articuloId}

            onChange={(v) => setArticuloId(v ?? undefined)}

            style={{ width: 120 }}

          />

          <Button type="link" onClick={() => setArticuloId(undefined)}>

            Todos

          </Button>

          <Input.Search

            allowClear

            placeholder="Buscar id lista o artículo"

            style={{ width: 240 }}

            value={filtro}

            onChange={(e) => setFiltro(e.target.value)}

          />

          <Checkbox

            checked={incluirInactivos}

            onChange={(e) => setIncluirInactivos(e.target.checked)}

          >

            Incluir inactivos

          </Checkbox>

          <Button

            type="primary"

            icon={<PlusOutlined />}

            onClick={() => {

              setEditing(null)

              form.setFieldsValue({

                articuloId: articuloId ?? 0,

                monto: 0,

                descuento: 0,

                puntos: 0,

                puntosCanje: 0,

                estado: true,

              })

              setModalOpen(true)

            }}

          >

            Nuevo precio

          </Button>

          <Button

            icon={<ReloadOutlined />}

            onClick={() => void listaQuery.refetch()}

            loading={listaQuery.isFetching}

          >

            Actualizar

          </Button>

          <Button

            icon={<DownloadOutlined />}

            disabled={filtradas.length === 0}

            onClick={() => downloadCsv(filtradas)}

          >

            Exportar CSV

          </Button>

        </Space>

      }

      extra={

        <Modal

          title={editing ? `Editar #${editing.listaPrecioId}` : 'Nuevo precio'}

          open={modalOpen}

          onCancel={() => setModalOpen(false)}

          onOk={() => form.submit()}

          confirmLoading={guardar.isPending}

        >

          <Form

            form={form}

            layout="vertical"

            onFinish={(v) =>

              guardar.mutate({

                listaPrecioId: editing?.listaPrecioId ?? 0,

                articuloId: v.articuloId,

                monto: v.monto,

                descuento: v.descuento,

                puntos: v.puntos ?? null,

                puntosCanje: v.puntosCanje ?? null,

                estado: v.estado,

              })

            }

          >

            <Form.Item name="articuloId" label="Artículo id" rules={[{ required: true }]}>

              <InputNumber min={1} style={{ width: '100%' }} />

            </Form.Item>

            <Form.Item name="monto" label="Monto" rules={[{ required: true }]}>

              <InputNumber min={0} precision={2} style={{ width: '100%' }} />

            </Form.Item>

            <Form.Item name="descuento" label="Descuento">

              <InputNumber min={0} precision={2} style={{ width: '100%' }} />

            </Form.Item>

            <Form.Item name="puntos" label="Puntos">

              <InputNumber min={0} style={{ width: '100%' }} />

            </Form.Item>

            <Form.Item name="puntosCanje" label="Puntos canje">

              <InputNumber min={0} style={{ width: '100%' }} />

            </Form.Item>

            <Form.Item name="estado" label="Activo" valuePropName="checked">

              <Switch />

            </Form.Item>

          </Form>

        </Modal>

      }

    >

      <CredixDataTable<ListaPrecioGestionRow>

        rowKey="listaPrecioId"

        columns={columns}

        dataSource={filtradas}

        loading={listaQuery.isLoading}

        pagination={{ pageSize: 20, showTotal: (t) => `${t} precio(s)` }}

        scroll={{ x: 800 }}

        size="small"

      />

    </CredixCrudPage>

  )

}

