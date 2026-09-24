import { useEffect, useMemo } from 'react'

import { Link, useSearchParams } from 'react-router-dom'

import { useMutation, useQuery } from '@tanstack/react-query'

import {

  Alert,

  Button,

  Form,

  Input,

  InputNumber,

  Select,

  Space,

  Switch,

  Tabs,

  message,

} from 'antd'

import dayjs from 'dayjs'

import {

  actualizarMovimientoAlmacen,

  confirmarMovimientoAlmacen,

  crearMovimientoDetalle,

  desconfirmarMovimientoAlmacen,

  eliminarMovimientoDetalle,

  fetchTiposMovimientoAlmacen,

} from '../../api/almacenMovimiento'

import { ApiError } from '../../api/errors'

import { useAuth } from '../../auth/useAuth'

import { CredixDatePicker, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'



function errMsg(e: unknown): string {

  return e instanceof ApiError ? e.message : 'Error desconocido'

}



export function MovimientoAlmacenPage() {

  const { session } = useAuth()

  const oficinaId = session?.oficinaId ?? 0

  const [searchParams, setSearchParams] = useSearchParams()

  const movimientoId = Number(searchParams.get('movimientoId') || 0)

  const numRule = (min: number) => [

    { required: true, type: 'number' as const, min },

  ]



  const [cabForm] = Form.useForm()

  const [detForm] = Form.useForm()

  const [delForm] = Form.useForm()



  const tipos = useQuery({

    queryKey: ['tipos-movimiento-almacen'],

    queryFn: fetchTiposMovimientoAlmacen,

  })



  useEffect(() => {

    if (movimientoId > 0) {

      cabForm.setFieldValue('movimientoId', movimientoId)

      detForm.setFieldsValue({ movimientoId, movimientoDetId: 0 })

    }

  }, [movimientoId, cabForm, detForm])



  const actualizar = useMutation({

    mutationFn: actualizarMovimientoAlmacen,

    onSuccess: () => message.success('Cabecera actualizada'),

    onError: (e) => message.error(errMsg(e)),

  })



  const confirmar = useMutation({

    mutationFn: confirmarMovimientoAlmacen,

    onSuccess: (r) =>

      message.success(r.mensaje || 'Movimiento confirmado'),

    onError: (e) => message.error(errMsg(e)),

  })



  const desconfirmar = useMutation({

    mutationFn: desconfirmarMovimientoAlmacen,

    onSuccess: (r) =>

      message.success(r.mensaje || 'Movimiento desconfirmado'),

    onError: (e) => message.error(errMsg(e)),

  })



  const crearDet = useMutation({

    mutationFn: crearMovimientoDetalle,

    onSuccess: () => {

      message.success('Detalle registrado')

      detForm.resetFields(['articuloId', 'cantidad', 'listaSerie'])

    },

    onError: (e) => message.error(errMsg(e)),

  })



  const eliminarDet = useMutation({

    mutationFn: eliminarMovimientoDetalle,

    onSuccess: () => message.success('Detalle eliminado'),

    onError: (e) => message.error(errMsg(e)),

  })



  const tipoOptions = (tipos.data ?? []).map((t) => ({

    value: t.tipoMovimientoId,

    label: `${t.denominacion} (${t.indEntrada ? 'Entrada' : 'Salida'})`,

  }))

  const stats = useMemo((): CredixStatItem[] => [
    { value: movimientoId > 0 ? movimientoId : '—', label: 'Movimiento' },
    { value: oficinaId > 0 ? oficinaId : '—', label: 'Oficina' },
  ], [movimientoId, oficinaId])



  return (

    <CredixPage

      title="Movimiento de almacén"

      subtitle="Edición avanzada por número de movimiento. Para el flujo completo use entrada de almacén."

      stats={stats}

      breadcrumb={[

        { title: <Link to="/inicio">Inicio</Link> },

        { title: <Link to="/almacen">Almacén</Link> },

        { title: 'Movimiento' },

      ]}

    >

      {oficinaId < 1 ? (

        <Alert

          type="warning"

          showIcon

          style={{ marginBottom: 16 }}

          message="Sesión sin oficina válida."

        />

      ) : null}



      <CredixPanel>

        <Form layout="inline">

          <Form.Item label="Movimiento ID">

            <InputNumber

              min={1}

              value={movimientoId || undefined}

              onChange={(v) => {

                const id = v ?? 0

                if (id > 0) {

                  setSearchParams({ movimientoId: String(id) })

                } else {

                  setSearchParams({})

                }

              }}

            />

          </Form.Item>

          <Form.Item>

            <Link to="/almacen/entrada">

              <Button type="primary">Nueva entrada</Button>

            </Link>

          </Form.Item>

        </Form>

      </CredixPanel>



      {oficinaId > 0 && movimientoId < 1 ? (

        <Alert

          type="info"

          showIcon

          message="Indique el ID del movimiento en el campo superior o en la URL (?movimientoId=)."

        />

      ) : oficinaId > 0 && movimientoId > 0 ? (

        <Tabs
          className="credix-tabs"
          items={[

            {

              key: 'cab',

              label: 'Cabecera',

              children: (

                <Form

                  form={cabForm}

                  layout="vertical"

                  style={{ maxWidth: 480 }}

                  initialValues={{

                    movimientoId,

                    fecha: dayjs(),

                    observacion: '',

                  }}

                  onFinish={(v) =>

                    actualizar.mutate({

                      oficinaId,

                      movimientoId,

                      tipoMovimientoId: v.tipoMovimientoId,

                      fecha: (v.fecha as dayjs.Dayjs).format('YYYY-MM-DD'),

                      observacion: v.observacion,

                    })

                  }

                >

                  <Form.Item name="movimientoId" label="Movimiento ID">

                    <InputNumber disabled style={{ width: '100%' }} />

                  </Form.Item>

                  <Form.Item

                    name="tipoMovimientoId"

                    label="Tipo movimiento"

                    rules={[{ required: true }]}

                  >

                    <Select loading={tipos.isLoading} options={tipoOptions} showSearch optionFilterProp="label" />

                  </Form.Item>

                  <Form.Item name="fecha" label="Fecha" rules={[{ required: true }]}>

                    <CredixDatePicker />

                  </Form.Item>

                  <Form.Item name="observacion" label="Observación">

                    <Input.TextArea rows={2} />

                  </Form.Item>

                  <Button type="primary" htmlType="submit" loading={actualizar.isPending}>

                    Actualizar cabecera

                  </Button>

                </Form>

              ),

            },

            {

              key: 'det',

              label: 'Detalle',

              children: (

                <Form

                  form={detForm}

                  layout="vertical"

                  style={{ maxWidth: 520 }}

                  initialValues={{

                    movimientoId,

                    movimientoDetId: 0,

                    indAutogenerar: false,

                    indCorrelativo: false,

                    cantidad: 1,

                    precioUnitario: 0,

                    descuento: 0,

                    medida: 1,

                  }}

                  onFinish={(v) =>

                    crearDet.mutate({

                      oficinaId,

                      movimientoId,

                      movimientoDetId: v.movimientoDetId ?? 0,

                      articuloId: v.articuloId,

                      indAutogenerar: v.indAutogenerar ?? false,

                      listaSerie: v.listaSerie ?? '',

                      cantidad: v.cantidad,

                      indCorrelativo: v.indCorrelativo ?? false,

                      precioUnitario: v.precioUnitario ?? 0,

                      descuento: v.descuento ?? 0,

                      medida: v.medida ?? 1,

                    })

                  }

                >

                  <Form.Item name="articuloId" label="Artículo ID" rules={numRule(1)}>

                    <InputNumber style={{ width: '100%' }} min={1} />

                  </Form.Item>

                  <Form.Item name="cantidad" label="Cantidad" rules={numRule(1)}>

                    <InputNumber style={{ width: '100%' }} min={1} />

                  </Form.Item>

                  <Form.Item name="precioUnitario" label="Precio unitario">

                    <InputNumber style={{ width: '100%' }} min={0} step={0.01} />

                  </Form.Item>

                  <Form.Item name="descuento" label="Descuento">

                    <InputNumber style={{ width: '100%' }} min={0} step={0.01} />

                  </Form.Item>

                  <Form.Item name="medida" label="Unidad de medida">

                    <InputNumber style={{ width: '100%' }} min={1} />

                  </Form.Item>

                  <Form.Item name="listaSerie" label="Series (separadas)">

                    <Input.TextArea rows={2} placeholder="Opcional si autogenerar" />

                  </Form.Item>

                  <Form.Item name="indAutogenerar" label="Autogenerar series" valuePropName="checked">

                    <Switch />

                  </Form.Item>

                  <Form.Item name="indCorrelativo" label="Correlativo series" valuePropName="checked">

                    <Switch />

                  </Form.Item>

                  <Button type="primary" htmlType="submit" loading={crearDet.isPending}>

                    Agregar línea

                  </Button>

                </Form>

              ),

            },

            {

              key: 'del',

              label: 'Eliminar línea',

              children: (

                <Form

                  form={delForm}

                  layout="vertical"

                  style={{ maxWidth: 360 }}

                  onFinish={(v) =>

                    eliminarDet.mutate({

                      oficinaId,

                      movimientoDetId: v.movimientoDetId,

                    })

                  }

                >

                  <Form.Item

                    name="movimientoDetId"

                    label="Detalle ID"

                    rules={numRule(1)}

                  >

                    <InputNumber style={{ width: '100%' }} min={1} />

                  </Form.Item>

                  <Button danger htmlType="submit" loading={eliminarDet.isPending}>

                    Eliminar detalle

                  </Button>

                </Form>

              ),

            },

            {

              key: 'estado',

              label: 'Confirmar',

              children: (

                <Space>

                  <Button

                    type="primary"

                    loading={confirmar.isPending}

                    onClick={() =>

                      confirmar.mutate({ oficinaId, movimientoId })

                    }

                  >

                    Confirmar movimiento

                  </Button>

                  <Button

                    loading={desconfirmar.isPending}

                    onClick={() =>

                      desconfirmar.mutate({ oficinaId, movimientoId })

                    }

                  >

                    Desconfirmar

                  </Button>

                </Space>

              ),

            },

          ]}

        />

      ) : null}

    </CredixPage>

  )

}


