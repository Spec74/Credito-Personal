import { useMemo, useState } from 'react'

import { Link } from 'react-router-dom'

import { useMutation, useQuery } from '@tanstack/react-query'

import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons'

import {

  Alert,

  Button,

  Form,

  Input,

  InputNumber,

  Space,

  Spin,

  message,

} from 'antd'

import type { ColumnsType } from 'antd/es/table'

import { buscarClientes } from '../../api/clientes'

import {

  fetchArticuloVentaRapida,

  fetchCajaDiarioVentaRapida,

  realizarPedido,

  type ArticuloVentaRapida,

  type PedidoLinea,

} from '../../api/ventas'

import { ApiError } from '../../api/errors'

import { useAuth } from '../../auth/useAuth'

import { CredixDataTable, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'

import type { ClienteBuscarItem } from '../../types/api'

import { formatMoney } from '../../utils/formatMoney'



type LineaCarrito = PedidoLinea & {

  key: string

  codArticulo: string

  denominacion: string

  precioUnit: number

}



function errMsg(e: unknown): string {

  return e instanceof ApiError ? e.message : 'Error desconocido'

}



export function VentaRapidaPage() {

  const { session } = useAuth()

  const oficinaId = session?.oficinaId ?? 0

  const [codigo, setCodigo] = useState('')

  const [cantidad, setCantidad] = useState(1)

  const [descuento, setDescuento] = useState(0)

  const [carrito, setCarrito] = useState<LineaCarrito[]>([])

  const [personaId, setPersonaId] = useState<number | null>(null)

  const [clienteLabel, setClienteLabel] = useState('')

  const [terminoCliente, setTerminoCliente] = useState('')



  const caja = useQuery({

    queryKey: ['caja-diario-venta-rapida', oficinaId],

    queryFn: () => fetchCajaDiarioVentaRapida(oficinaId),

    enabled: oficinaId > 0,

    retry: false,

  })



  const sinCaja =

    caja.isError && caja.error instanceof ApiError && caja.error.status === 404



  const buscarArticulo = useMutation({

    mutationFn: (c: string) => fetchArticuloVentaRapida(oficinaId, c),

  })



  const busquedaCliente = useMutation({

    mutationFn: (t: string) => buscarClientes(t),

  })



  const cobrar = useMutation({

    mutationFn: () =>

      realizarPedido({

        oficinaId,

        cajaDiarioId: caja.data!.cajaDiarioId,

        personaId: personaId!,

        pedidos: carrito.map(({ articuloId, cantidad: c, descuento: d }) => ({

          articuloId,

          cantidad: c,

          descuento: d,

        })),

      }),

    onSuccess: (r) => {

      message.success(`Venta registrada — orden #${r.ordenVentaId}`)

      setCarrito([])

      setPersonaId(null)

      setClienteLabel('')

    },

    onError: (e) => message.error(errMsg(e)),

  })



  const agregarAlCarrito = async () => {

    const c = codigo.trim()

    if (!c) {

      message.warning('Indique código de artículo')

      return

    }

    if (cantidad < 1) {

      message.warning('Cantidad mínima 1')

      return

    }

    try {

      const art: ArticuloVentaRapida = await buscarArticulo.mutateAsync(c)

      if (art.stock < cantidad) {

        message.error(`Stock insuficiente (${art.stock} disponible)`)

        return

      }

      const precio = art.precioVenta ?? 0

      setCarrito((prev) => [

        ...prev,

        {

          key: `${art.articuloId}-${Date.now()}`,

          articuloId: art.articuloId,

          codArticulo: art.codArticulo,

          denominacion: art.denominacion,

          cantidad,

          descuento,

          precioUnit: precio,

        },

      ])

      setCodigo('')

      setCantidad(1)

      setDescuento(0)

    } catch (e) {

      message.error(errMsg(e))

    }

  }



  const columns: ColumnsType<LineaCarrito> = [

    { title: 'Código', dataIndex: 'codArticulo', width: 90 },

    { title: 'Artículo', dataIndex: 'denominacion', ellipsis: true },

    { title: 'Cant.', dataIndex: 'cantidad', width: 60 },

    {

      title: 'P. unit.',

      dataIndex: 'precioUnit',

      width: 90,

      align: 'right',

      render: formatMoney,

    },

    {

      title: 'Desc.',

      dataIndex: 'descuento',

      width: 80,

      align: 'right',

      render: formatMoney,

    },

    {

      title: '',

      key: 'act',

      width: 48,

      render: (_, row) => (

        <Button

          type="text"

          danger

          icon={<DeleteOutlined />}

          onClick={() => setCarrito((p) => p.filter((x) => x.key !== row.key))}

        />

      ),

    },

  ]



  const total = carrito.reduce(

    (s, l) => s + l.precioUnit * l.cantidad - l.descuento,

    0,

  )

  const stats = useMemo((): CredixStatItem[] => [
    { value: carrito.length, label: 'Líneas carrito' },
    { value: formatMoney(total), label: 'Total carrito' },
    {
      value: personaId ? (clienteLabel || `#${personaId}`) : 'Sin cliente',
      label: 'Cliente',
      tone: personaId ? 'green' : 'default',
    },
  ], [carrito.length, total, personaId, clienteLabel])



  return (

    <CredixPage

      title="Venta rápida"

      subtitle="Registro de pedido al contado con series y cobro en caja."

      stats={stats}

      breadcrumb={[

        { title: <Link to="/inicio">Inicio</Link> },

        { title: <Link to="/ventas">Ventas</Link> },

        { title: 'Venta rápida' },

      ]}

    >

      {oficinaId < 1 ? (

        <Alert type="warning" showIcon message="Sesión sin oficina válida." />

      ) : caja.isLoading ? (

        <CredixPanel>

          <Spin />

        </CredixPanel>

      ) : sinCaja ? (

        <Alert

          type="warning"

          showIcon

          message="No tiene caja diario abierta"

          description={

            <>

              Asigne o abra caja en{' '}

              <Link to="/caja/asignar">Asignar caja</Link> o{' '}

              <Link to="/caja/diario">Caja diario</Link>.

            </>

          }

        />

      ) : caja.isError ? (

        <Alert type="error" showIcon message={errMsg(caja.error)} />

      ) : caja.data ? (

        <>

          <CredixPanel>

            <p style={{ marginBottom: 0 }}>

              Caja: <strong>{caja.data.cajaDenominacion}</strong> · Diario #

              {caja.data.cajaDiarioId}

            </p>

          </CredixPanel>



          <CredixPanel title="Cliente">

            <Space direction="vertical" style={{ width: '100%' }}>

              <Space wrap>

                <Input

                  placeholder="Buscar cliente (mín. 2 caracteres)"

                  value={terminoCliente}

                  onChange={(e) => setTerminoCliente(e.target.value)}

                  style={{ width: 280 }}

                />

                <Button

                  icon={<SearchOutlined />}

                  onClick={() => {

                    const t = terminoCliente.trim()

                    if (t.length >= 2) busquedaCliente.mutate(t)

                  }}

                  loading={busquedaCliente.isPending}

                >

                  Buscar

                </Button>

              </Space>

              {personaId ? (

                <Alert

                  type="success"

                  showIcon

                  message={`Cliente: ${clienteLabel} (persona #${personaId})`}

                />

              ) : null}

              {busquedaCliente.data?.length ? (

                <CredixDataTable<ClienteBuscarItem>

                  rowKey="personaId"

                  pagination={false}

                  dataSource={busquedaCliente.data}

                  columns={[

                    { title: 'Cliente', dataIndex: 'label' },

                    {

                      title: '',

                      width: 90,

                      render: (_, row) => (

                        <Button

                          size="small"

                          type="link"

                          onClick={() => {

                            setPersonaId(row.personaId)

                            setClienteLabel(row.label)

                            busquedaCliente.reset()

                          }}

                        >

                          Elegir

                        </Button>

                      ),

                    },

                  ]}

                />

              ) : null}

            </Space>

          </CredixPanel>



          <CredixPanel title="Artículos">

            <Space wrap align="end">

              <Form layout="inline">

                <Form.Item label="Código">

                  <Input

                    value={codigo}

                    onChange={(e) => setCodigo(e.target.value)}

                    onPressEnter={() => void agregarAlCarrito()}

                    style={{ width: 140 }}

                  />

                </Form.Item>

                <Form.Item label="Cant.">

                  <InputNumber

                    min={1}

                    value={cantidad}

                    onChange={(v) => setCantidad(v ?? 1)}

                  />

                </Form.Item>

                <Form.Item label="Desc.">

                  <InputNumber

                    min={0}

                    step={0.01}

                    value={descuento}

                    onChange={(v) => setDescuento(v ?? 0)}

                  />

                </Form.Item>

              </Form>

              <Button

                type="primary"

                icon={<PlusOutlined />}

                loading={buscarArticulo.isPending}

                onClick={() => void agregarAlCarrito()}

              >

                Agregar

              </Button>

            </Space>

            <CredixDataTable<LineaCarrito>

              style={{ marginTop: 16 }}

              rowKey="key"

              dataSource={carrito}

              columns={columns}

              pagination={false}

              locale={{ emptyText: 'Carrito vacío' }}

            />

            <p style={{ marginTop: 12, marginBottom: 0 }}>

              Total estimado: <strong>{formatMoney(total)}</strong>

            </p>

          </CredixPanel>



          <Button

            type="primary"

            size="large"

            disabled={!personaId || carrito.length === 0}

            loading={cobrar.isPending}

            onClick={() => cobrar.mutate()}

          >

            Realizar pedido y cobrar

          </Button>

        </>

      ) : null}

    </CredixPage>

  )

}


