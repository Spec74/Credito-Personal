import { useMemo, useState } from 'react'

import { Link } from 'react-router-dom'

import { useMutation, useQuery } from '@tanstack/react-query'

import { GiftOutlined, SearchOutlined } from '@ant-design/icons'

import {

  Alert,

  Button,

  Input,

  Modal,

  Space,

  Typography,

  message,

} from 'antd'

import type { ColumnsType } from 'antd/es/table'

import { buscarClientes } from '../../api/clientes'

import {

  canjearPuntos,

  fetchArticulosCanjear,

  fetchTarjetaPuntos,

  type ArticuloCanjeListItem,

} from '../../api/canjearPuntos'

import { ApiError } from '../../api/errors'

import {
  CredixDataTable,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'

import type { ClienteBuscarItem } from '../../types/api'



const { Text } = Typography



function errMsg(e: unknown): string {

  return e instanceof ApiError ? e.message : 'Error desconocido'

}



export function CanjearPuntosPage() {

  const [personaId, setPersonaId] = useState<number | null>(null)

  const [clienteLabel, setClienteLabel] = useState('')

  const [terminoCliente, setTerminoCliente] = useState('')

  const [modalOpen, setModalOpen] = useState(false)

  const [numeroSerie, setNumeroSerie] = useState('')

  const [errorCanje, setErrorCanje] = useState('')



  const busquedaCliente = useMutation({

    mutationFn: (t: string) => buscarClientes(t),

  })



  const tarjeta = useQuery({

    queryKey: ['tarjeta-puntos', personaId],

    queryFn: () => fetchTarjetaPuntos(personaId!),

    enabled: personaId != null && personaId > 0,

    retry: false,

  })



  const articulos = useQuery({

    queryKey: ['articulos-canjear', personaId],

    queryFn: () => fetchArticulosCanjear(personaId!),

    enabled: personaId != null && personaId > 0 && tarjeta.isSuccess,

    retry: false,

  })



  const canje = useMutation({

    mutationFn: () => canjearPuntos(personaId!, numeroSerie.trim()),

    onSuccess: (r) => {

      if (r.mensaje) {

        setErrorCanje(r.mensaje)

        return

      }

      message.success('Canje registrado')

      setModalOpen(false)

      setNumeroSerie('')

      setErrorCanje('')

      tarjeta.refetch()

      articulos.refetch()

    },

    onError: (e) => setErrorCanje(errMsg(e)),

  })



  const seleccionarCliente = (row: ClienteBuscarItem) => {

    setPersonaId(row.personaId)

    setClienteLabel(row.label)

    setTerminoCliente('')

    busquedaCliente.reset()

  }



  const limpiarCliente = () => {

    setPersonaId(null)

    setClienteLabel('')

    setTerminoCliente('')

    busquedaCliente.reset()

  }



  const abrirCanje = () => {

    setNumeroSerie('')

    setErrorCanje('')

    setModalOpen(true)

  }



  const columns: ColumnsType<ArticuloCanjeListItem> = [

    { title: 'Tipo', dataIndex: 'tipoArticulo', width: 120 },

    { title: 'Artículo', dataIndex: 'articuloDesc', ellipsis: true },

    {

      title: 'Puntos',

      dataIndex: 'puntosCanje',

      width: 90,

      align: 'center',

    },

  ]



  const sinTarjeta =
    personaId != null &&
    tarjeta.isError &&
    tarjeta.error instanceof ApiError &&
    tarjeta.error.status === 404

  const stats = useMemo((): CredixStatItem[] => {
    const puntos = tarjeta.data?.totalPuntos
    return [
      {
        value: clienteLabel || '—',
        label: 'Cliente',
      },
      {
        value: puntos ?? (personaId ? '…' : '—'),
        label: 'Puntos disponibles',
        tone: puntos != null && Number(puntos) > 0 ? 'green' : 'default',
      },
      {
        value: articulos.data?.length ?? 0,
        label: 'Artículos para canje',
      },
      {
        value: personaId ? (sinTarjeta ? 'Sin tarjeta' : 'Listo') : 'Seleccione cliente',
        label: 'Estado',
        tone: personaId && !sinTarjeta ? 'green' : 'default',
      },
    ]
  }, [
    clienteLabel,
    tarjeta.data?.totalPuntos,
    articulos.data?.length,
    personaId,
    sinTarjeta,
  ])

  return (
    <CredixPage
      title="Canje de puntos"
      subtitle="Canjee artículos con los puntos acumulados del cliente."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/ventas">Ventas</Link> },
        { title: 'Canjear puntos' },
      ]}
      stats={stats}
    >

      <CredixPanel title="Cliente">

        {personaId == null ? (

          <>

            <Space wrap style={{ marginBottom: 12 }}>

              <Input.Search

                allowClear

                placeholder="Apellidos, DNI, código o celular (mín. 2 caracteres)"

                style={{ width: 360 }}

                value={terminoCliente}

                onChange={(e) => setTerminoCliente(e.target.value)}

                onSearch={() => {

                  const t = terminoCliente.trim()

                  if (t.length >= 2) {

                    busquedaCliente.mutate(t)

                  }

                }}

                enterButton={

                  <span>

                    <SearchOutlined /> Buscar

                  </span>

                }

                loading={busquedaCliente.isPending}

              />

            </Space>

            {busquedaCliente.data && busquedaCliente.data.length > 0 && (

              <CredixDataTable<ClienteBuscarItem>

                rowKey="personaId"

                size="small"

                pagination={false}

                dataSource={busquedaCliente.data}

                columns={[

                  { title: 'Cliente', dataIndex: 'label' },

                  {

                    title: '',

                    width: 100,

                    render: (_, row) => (

                      <Button size="small" onClick={() => seleccionarCliente(row)}>

                        Elegir

                      </Button>

                    ),

                  },

                ]}

              />

            )}

          </>

        ) : (

          <Space wrap>

            <Text strong>{clienteLabel}</Text>

            <Text type="secondary">(personaId {personaId})</Text>

            <Button size="small" onClick={limpiarCliente}>

              Cambiar cliente

            </Button>

          </Space>

        )}

      </CredixPanel>



      {personaId != null && (

        <>

          {sinTarjeta ? (

            <Alert

              type="warning"

              showIcon

              message="El cliente no tiene tarjeta de puntos activa."

            />

          ) : (

            <>

              <CredixPanel

                title="Artículos para canjear"

                extra={

                  <Button

                    type="primary"

                    icon={<GiftOutlined />}

                    disabled={!articulos.data?.length}

                    onClick={abrirCanje}

                  >

                    Canjear por serie

                  </Button>

                }

              >

                {articulos.isError && (

                  <Alert

                    type="error"

                    showIcon

                    style={{ marginBottom: 16 }}

                    message={errMsg(articulos.error)}

                  />

                )}

                <CredixDataTable<ArticuloCanjeListItem>

                  rowKey="listaPrecioId"

                  columns={columns}

                  dataSource={articulos.data ?? []}

                  loading={articulos.isLoading}

                  pagination={{ pageSize: 15 }}

                  locale={{ emptyText: 'Sin artículos canjeables con los puntos actuales' }}

                  onRow={() => ({

                    onDoubleClick: abrirCanje,

                  })}

                />

              </CredixPanel>

            </>

          )}

        </>

      )}



      <Modal

        title="Canjear artículo"

        open={modalOpen}

        onCancel={() => setModalOpen(false)}

        onOk={() => {

          if (!numeroSerie.trim()) {

            setErrorCanje('Ingrese el número de serie')

            return

          }

          canje.mutate()

        }}

        confirmLoading={canje.isPending}

        okText="Canjear"

      >

        <Typography.Paragraph>

          Ingrese el número de serie del artículo a canjear.

        </Typography.Paragraph>

        <Input

          placeholder="Número de serie"

          value={numeroSerie}

          onChange={(e) => setNumeroSerie(e.target.value)}

          onPressEnter={() => canje.mutate()}

        />

        {errorCanje && (

          <Alert type="error" showIcon message={errorCanje} style={{ marginTop: 12 }} />

        )}

      </Modal>

    </CredixPage>

  )

}

