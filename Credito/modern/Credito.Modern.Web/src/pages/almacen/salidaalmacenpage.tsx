import { useMemo, useState } from 'react'

import { Link } from 'react-router-dom'

import { useMutation, useQuery } from '@tanstack/react-query'

import { DeleteOutlined } from '@ant-design/icons'

import {

  Alert,

  Button,

  Form,

  Input,

  Select,

  Space,

  Typography,

  message,

} from 'antd'

import type { ColumnsType } from 'antd/es/table'

import { fetchAlmacenes } from '../../api/almacenes'

import { fetchTiposMovimientoAlmacen } from '../../api/almacenMovimiento'

import {

  buscarSerieSalida,

  realizarSalidaAlmacen,

  type SerieSalidaLinea,

} from '../../api/salidaAlmacen'

import { ApiError } from '../../api/errors'

import { useAuth } from '../../auth/useAuth'

import { CredixDataTable, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'



const { Text } = Typography



function errMsg(e: unknown): string {

  return e instanceof ApiError ? e.message : 'Error desconocido'

}



export function SalidaAlmacenPage() {

  const { session } = useAuth()

  const oficinaId = session?.oficinaId ?? 0

  const [tipoMovId, setTipoMovId] = useState<number | null>(null)

  const [glosa, setGlosa] = useState('')

  const [serieTerm, setSerieTerm] = useState('')

  const [carrito, setCarrito] = useState<SerieSalidaLinea[]>([])



  const almacenes = useQuery({

    queryKey: ['almacenes', oficinaId],

    queryFn: () => fetchAlmacenes(oficinaId),

    enabled: oficinaId > 0,

  })



  const tiposSalida = useQuery({

    queryKey: ['tipos-movimiento-salida'],

    queryFn: fetchTiposMovimientoAlmacen,

    select: (rows) =>

      rows.filter(

        (t) =>

          !t.indEntrada &&

          !t.indTransferencia &&

          !t.indDevolucion &&

          t.tipoMovimientoId !== 2,

      ),

  })



  const buscar = useMutation({

    mutationFn: (numeroSerie: string) => buscarSerieSalida(numeroSerie),

    onSuccess: (r) => {

      if (r.error || !r.serieId) {

        message.warning(r.mensaje ?? 'Serie no válida')

        return

      }

      if (carrito.some((c) => c.serieId === r.serieId)) {

        message.warning('La serie ya está en la lista')

        return

      }

      setCarrito((prev) => [

        ...prev,

        {

          serieId: r.serieId!,

          serie: r.serie ?? '',

          articuloId: r.articuloId!,

          denominacion: r.denominacion ?? '',

        },

      ])

      setSerieTerm('')

    },

    onError: (e) => message.error(errMsg(e)),

  })



  const registrar = useMutation({

    mutationFn: () =>

      realizarSalidaAlmacen({

        oficinaId,

        tipoMovimientoId: tipoMovId!,

        glosa,

        series: carrito,

      }),

    onSuccess: (r) => {

      message.success(`Salida registrada — movimiento #${r.movimientoId}`)

      setCarrito([])

      setGlosa('')

      setSerieTerm('')

    },

    onError: (e) => message.error(errMsg(e)),

  })



  const columns: ColumnsType<SerieSalidaLinea> = [

    { title: 'Serie', dataIndex: 'serie', width: 140 },

    { title: 'Artículo', dataIndex: 'denominacion', ellipsis: true },

    { title: 'Art. ID', dataIndex: 'articuloId', width: 80 },

    {

      title: '',

      key: 'act',

      width: 56,

      render: (_, row) => (

        <Button

          type="text"

          danger

          icon={<DeleteOutlined />}

          onClick={() => setCarrito((p) => p.filter((x) => x.serieId !== row.serieId))}

        />

      ),

    },

  ]



  const almacenLabel = almacenes.data?.[0]?.denominacion ?? '—'

  const stats = useMemo((): CredixStatItem[] => [
    { value: carrito.length, label: 'Series en salida' },
    { value: tipoMovId ?? '—', label: 'Tipo movimiento' },
    { value: almacenLabel, label: 'Almacén' },
  ], [carrito.length, tipoMovId, almacenLabel])



  return (

    <CredixPage

      title="Salida de almacén"

      subtitle="Registro de salida por escaneo o ingreso de series en almacén."

      stats={stats}

      breadcrumb={[

        { title: <Link to="/inicio">Inicio</Link> },

        { title: <Link to="/almacen">Almacén</Link> },

        { title: 'Salida' },

      ]}

    >

      {oficinaId < 1 ? (

        <Alert type="warning" showIcon message="Sesión sin oficina válida." />

      ) : (

        <>

          <CredixPanel>

            <Text type="secondary">

              Almacén de la oficina: <Text strong>{almacenLabel}</Text>

            </Text>

          </CredixPanel>



          <CredixPanel title="Datos de salida">

            <Form layout="vertical" style={{ maxWidth: 480 }}>

              <Form.Item label="Tipo de salida" required>

                <Select

                  placeholder="Seleccione tipo"

                  value={tipoMovId ?? undefined}

                  onChange={setTipoMovId}

                  loading={tiposSalida.isLoading}

                  options={(tiposSalida.data ?? []).map((t) => ({

                    value: t.tipoMovimientoId,

                    label: t.denominacion,

                  }))}

                />

              </Form.Item>

              <Form.Item label="Glosa / observación">

                <Input.TextArea

                  rows={2}

                  value={glosa}

                  onChange={(e) => setGlosa(e.target.value)}

                  placeholder="Motivo de la salida"

                />

              </Form.Item>

              <Form.Item label="Número de serie">

                <Input.Search

                  placeholder="Escanee o escriba la serie"

                  value={serieTerm}

                  onChange={(e) => setSerieTerm(e.target.value)}

                  onSearch={(v) => {

                    if (v.trim()) buscar.mutate(v.trim())

                  }}

                  loading={buscar.isPending}

                  enterButton="Agregar"

                />

              </Form.Item>

            </Form>

          </CredixPanel>



          <CredixDataTable<SerieSalidaLinea>

            rowKey="serieId"

            style={{ marginBottom: 16 }}

            dataSource={carrito}

            columns={columns}

            pagination={false}

            locale={{ emptyText: 'Agregue series para salir del almacén' }}

          />



          <Space>

            <Button

              type="primary"

              disabled={!tipoMovId || carrito.length === 0}

              loading={registrar.isPending}

              onClick={() => registrar.mutate()}

            >

              Registrar salida ({carrito.length} serie{carrito.length === 1 ? '' : 's'})

            </Button>

            <Button disabled={carrito.length === 0} onClick={() => setCarrito([])}>

              Vaciar lista

            </Button>

          </Space>

        </>

      )}

    </CredixPage>

  )

}


