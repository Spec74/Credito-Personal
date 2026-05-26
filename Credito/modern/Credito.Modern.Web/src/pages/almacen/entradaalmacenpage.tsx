import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Spin,
  Steps,
  Switch,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'
import { fetchAlmacenes } from '../../api/almacenes'
import {
  actualizarMovimientoAlmacen,
  confirmarMovimientoAlmacen,
  crearMovimientoDetalle,
  desconfirmarMovimientoAlmacen,
  eliminarMovimientoDetalle,
  fetchTiposMovimientoAlmacen,
} from '../../api/almacenMovimiento'
import {
  actualizarImporteMovimiento,
  agregarMovimientoDocumento,
  crearMovimientoEntrada,
  eliminarMovimientoDocumento,
  fetchMovimientoEntradaDetalle,
  fetchMovimientosEntrada,
  fetchTiposDocumentoAlmacenMov,
  validarSeriesEntrada,
  type MovimientoDocRow,
  type MovimientoEntradaDetLinea,
  type MovimientoEntradaListRow,
} from '../../api/entradaAlmacen'
import { ApiError } from '../../api/errors'
import { fetchValoresTabla } from '../../api/maestros'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

const { Paragraph, Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

const numRule = (min: number) => [{ required: true, type: 'number' as const, min }]

export function EntradaAlmacenPage() {
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const oficinaId = session?.oficinaId ?? 0
  const [searchParams, setSearchParams] = useSearchParams()
  const movimientoId = Number(searchParams.get('movimientoId') || 0)

  const [almacenId, setAlmacenId] = useState<number | null>(null)
  const [buscar, setBuscar] = useState('')
  const [articuloFiltro, setArticuloFiltro] = useState<number | null>(null)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [nuevaOpen, setNuevaOpen] = useState(false)
  const [tipoNuevo, setTipoNuevo] = useState<number | null>(null)
  const [step, setStep] = useState(0)

  const [cabForm] = Form.useForm()
  const [docForm] = Form.useForm()
  const [detForm] = Form.useForm()
  const [ajusteForm] = Form.useForm()

  const almacenes = useQuery({
    queryKey: ['almacenes', oficinaId],
    queryFn: () => fetchAlmacenes(oficinaId),
    enabled: oficinaId > 0,
  })

  const tiposEntrada = useQuery({
    queryKey: ['tipos-movimiento-entrada'],
    queryFn: fetchTiposMovimientoAlmacen,
    select: (rows) => rows.filter((t) => t.indEntrada),
  })

  const tiposDoc = useQuery({
    queryKey: ['tipos-documento-almacen-mov'],
    queryFn: fetchTiposDocumentoAlmacenMov,
  })

  const medidas = useQuery({
    queryKey: ['valores-tabla', 10],
    queryFn: () => fetchValoresTabla(10),
  })

  const almacenIdEfectivo =
    almacenId ?? (almacenes.data?.length ? almacenes.data[0].almacenId : null)

  const listado = useQuery({
    queryKey: ['movimientos-entrada', oficinaId, almacenIdEfectivo, buscar, articuloFiltro, page, pageSize],
    queryFn: () =>
      fetchMovimientosEntrada({
        oficinaId,
        almacenId: almacenIdEfectivo!,
        buscar,
        articuloId: articuloFiltro ?? 0,
        page,
        pageSize,
      }),
    enabled: oficinaId > 0 && almacenIdEfectivo != null && almacenIdEfectivo > 0 && movimientoId < 1,
  })

  const detalle = useQuery({
    queryKey: ['movimiento-entrada-detalle', oficinaId, movimientoId],
    queryFn: () => fetchMovimientoEntradaDetalle(oficinaId, movimientoId),
    enabled: oficinaId > 0 && movimientoId > 0,
  })

  useEffect(() => {
    const c = detalle.data?.cabecera
    if (c) {
      cabForm.setFieldsValue({
        tipoMovimientoId: c.tipoMovimientoId,
        fecha: dayjs(c.fecha.slice(0, 10)),
        observacion: c.observacion ?? '',
      })
      ajusteForm.setFieldsValue({ ajusteRedondeo: c.ajusteRedondeo })
      detForm.setFieldsValue({ movimientoId: c.movimientoId, movimientoDetId: 0 })
    }
  }, [detalle.data, cabForm, ajusteForm, detForm])

  const refrescar = () => {
    void queryClient.invalidateQueries({ queryKey: ['movimiento-entrada-detalle'] })
    void queryClient.invalidateQueries({ queryKey: ['movimientos-entrada'] })
  }

  const crear = useMutation({
    mutationFn: () =>
      crearMovimientoEntrada({
        oficinaId,
        almacenId: almacenIdEfectivo!,
        tipoMovimientoId: tipoNuevo!,
      }),
    onSuccess: (r) => {
      message.success(`Entrada #${r.movimientoId} creada`)
      setNuevaOpen(false)
      setSearchParams({ movimientoId: String(r.movimientoId) })
      setStep(0)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const actualizarCab = useMutation({
    mutationFn: actualizarMovimientoAlmacen,
    onSuccess: () => {
      message.success('Cabecera actualizada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const agregarDoc = useMutation({
    mutationFn: agregarMovimientoDocumento,
    onSuccess: () => {
      message.success('Documento agregado')
      docForm.resetFields(['serieDocumento', 'nroDocumento'])
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const quitarDoc = useMutation({
    mutationFn: eliminarMovimientoDocumento,
    onSuccess: () => {
      message.success('Documento eliminado')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const crearDet = useMutation({
    mutationFn: crearMovimientoDetalle,
    onSuccess: () => {
      message.success('Línea agregada')
      detForm.resetFields(['articuloId', 'cantidad', 'listaSerie'])
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const quitarDet = useMutation({
    mutationFn: eliminarMovimientoDetalle,
    onSuccess: () => {
      message.success('Línea eliminada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const ajuste = useMutation({
    mutationFn: actualizarImporteMovimiento,
    onSuccess: () => {
      message.success('Importe actualizado')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const confirmar = useMutation({
    mutationFn: confirmarMovimientoAlmacen,
    onSuccess: (r) => {
      message.success(r.mensaje || 'Entrada confirmada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const desconfirmar = useMutation({
    mutationFn: desconfirmarMovimientoAlmacen,
    onSuccess: (r) => {
      message.success(r.mensaje || 'Entrada desconfirmada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const validarSeries = useMutation({
    mutationFn: (v: { listaSerie: string; cantidad: number; indCorrelativo: boolean }) =>
      validarSeriesEntrada(oficinaId, v.listaSerie, v.cantidad, v.indCorrelativo),
  })

  const listColumns: ColumnsType<MovimientoEntradaListRow> = [
    { title: 'Id', dataIndex: 'movimientoId', width: 70 },
    { title: 'Fecha', dataIndex: 'fecha', width: 100, render: formatFecha },
    { title: 'Tipo', dataIndex: 'tipoMovimiento', ellipsis: true },
    { title: 'Documento', dataIndex: 'documento', ellipsis: true },
    { title: 'Estado', dataIndex: 'estado', width: 110 },
    {
      title: '',
      key: 'act',
      width: 80,
      render: (_, r) => (
        <Button
          type="link"
          size="small"
          onClick={() => {
            setSearchParams({ movimientoId: String(r.movimientoId) })
            setStep(0)
          }}
        >
          Abrir
        </Button>
      ),
    },
  ]

  const docColumns: ColumnsType<MovimientoDocRow> = [
    { title: 'Tipo', dataIndex: 'tipoDocumento', ellipsis: true },
    { title: 'Serie', dataIndex: 'serieDocumento', width: 80 },
    { title: 'Número', dataIndex: 'nroDocumento', width: 100 },
    {
      title: '',
      key: 'act',
      width: 80,
      render: (_, r) =>
        r.puedeEliminar ? (
          <Button
            type="link"
            danger
            size="small"
            loading={quitarDoc.isPending}
            onClick={() =>
              quitarDoc.mutate({ oficinaId, movimientoDocId: r.movimientoDocId })
            }
          >
            Quitar
          </Button>
        ) : null,
    },
  ]

  const detColumns: ColumnsType<MovimientoEntradaDetLinea> = [
    { title: 'Artículo', dataIndex: 'descripcion', ellipsis: true },
    { title: 'Cant.', dataIndex: 'cantidad', width: 60 },
    { title: 'Ud.', dataIndex: 'unidadMedida', width: 60 },
    {
      title: 'P. unit.',
      dataIndex: 'precioUnitario',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Importe',
      dataIndex: 'importe',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: '',
      key: 'act',
      width: 70,
      render: (_, r) =>
        r.puedeEliminar ? (
          <Button
            type="link"
            danger
            size="small"
            onClick={() =>
              quitarDet.mutate({ oficinaId, movimientoDetId: r.movimientoDetId })
            }
          >
            Quitar
          </Button>
        ) : null,
    },
  ]

  const editable = detalle.data?.cabecera.editable ?? false
  const cab = detalle.data?.cabecera

  const wizardSteps = [
    {
      title: 'Cabecera',
      content: cab ? (
        <Form
          form={cabForm}
          layout="vertical"
          style={{ maxWidth: 480 }}
          disabled={!editable}
          onFinish={(v) =>
            actualizarCab.mutate({
              oficinaId,
              movimientoId,
              tipoMovimientoId: v.tipoMovimientoId,
              fecha: (v.fecha as dayjs.Dayjs).format('YYYY-MM-DD'),
              observacion: v.observacion,
            })
          }
        >
          <Form.Item name="tipoMovimientoId" label="Tipo movimiento" rules={[{ required: true }]}>
            <Select
              options={(tiposEntrada.data ?? []).map((t) => ({
                value: t.tipoMovimientoId,
                label: t.denominacion,
              }))}
            />
          </Form.Item>
          <Form.Item name="fecha" label="Fecha" rules={[{ required: true }]}>
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
          <Form.Item name="observacion" label="Observación">
            <Input.TextArea rows={2} />
          </Form.Item>
          {editable ? (
            <Button type="primary" htmlType="submit" loading={actualizarCab.isPending}>
              Guardar cabecera
            </Button>
          ) : (
            <Alert type="info" showIcon message="Movimiento confirmado — cabecera bloqueada." />
          )}
        </Form>
      ) : (
        <CredixPanel>
          <Spin />
        </CredixPanel>
      ),
    },
    {
      title: 'Documentos',
      content: (
        <>
          <CredixDataTable<MovimientoDocRow>
            rowKey="movimientoDocId"
            size="small"
            style={{ marginBottom: 16 }}
            dataSource={detalle.data?.documentos ?? []}
            columns={docColumns}
            pagination={false}
            locale={{ emptyText: 'Sin documentos' }}
          />
          {editable ? (
            <Form
              form={docForm}
              layout="inline"
              onFinish={(v) =>
                agregarDoc.mutate({
                  oficinaId,
                  movimientoId,
                  tipoDocumentoId: v.tipoDocumentoId,
                  serieDocumento: v.serieDocumento,
                  nroDocumento: v.nroDocumento,
                })
              }
            >
              <Form.Item name="tipoDocumentoId" rules={[{ required: true }]}>
                <Select
                  placeholder="Tipo doc."
                  style={{ width: 160 }}
                  options={(tiposDoc.data ?? []).map((t) => ({
                    value: t.tipoDocumentoId,
                    label: t.denominacion,
                  }))}
                />
              </Form.Item>
              <Form.Item name="serieDocumento" rules={[{ required: true }]}>
                <Input placeholder="Serie" style={{ width: 80 }} />
              </Form.Item>
              <Form.Item name="nroDocumento" rules={[{ required: true }]}>
                <Input placeholder="Número" style={{ width: 100 }} />
              </Form.Item>
              <Form.Item>
                <Button type="primary" htmlType="submit" loading={agregarDoc.isPending}>
                  Agregar
                </Button>
              </Form.Item>
            </Form>
          ) : null}
          {cab?.documento ? (
            <Paragraph type="secondary" style={{ marginTop: 12, marginBottom: 0 }}>
              Resumen: {cab.documento}
            </Paragraph>
          ) : null}
        </>
      ),
    },
    {
      title: 'Detalle',
      content: (
        <>
          <CredixDataTable<MovimientoEntradaDetLinea>
            rowKey="movimientoDetId"
            size="small"
            style={{ marginBottom: 16 }}
            dataSource={detalle.data?.detalle ?? []}
            columns={detColumns}
            pagination={false}
          />
          {editable ? (
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
                medida: medidas.data?.[0]?.itemId ?? 1,
              }}
              onFinish={async (v) => {
                if (!v.indAutogenerar && v.listaSerie?.trim()) {
                  try {
                    const val = await validarSeries.mutateAsync({
                      listaSerie: v.listaSerie.trim(),
                      cantidad: v.cantidad,
                      indCorrelativo: v.indCorrelativo ?? false,
                    })
                    if (val.resultado) {
                      message.warning(val.resultado)
                      return
                    }
                  } catch (e) {
                    message.error(errMsg(e))
                    return
                  }
                }
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
              }}
            >
              <Form.Item name="articuloId" label="Artículo ID" rules={numRule(1)}>
                <InputNumber style={{ width: '100%' }} min={1} />
              </Form.Item>
              <Form.Item name="cantidad" label="Cantidad" rules={numRule(1)}>
                <InputNumber style={{ width: '100%' }} min={1} />
              </Form.Item>
              <Form.Item name="medida" label="Unidad medida">
                <Select
                  options={(medidas.data ?? []).map((m) => ({
                    value: m.itemId,
                    label: m.desCorta ?? m.denominacion,
                  }))}
                />
              </Form.Item>
              <Form.Item name="precioUnitario" label="Precio unitario">
                <InputNumber style={{ width: '100%' }} min={0} step={0.01} />
              </Form.Item>
              <Form.Item name="descuento" label="Descuento">
                <InputNumber style={{ width: '100%' }} min={0} step={0.01} />
              </Form.Item>
              <Form.Item name="listaSerie" label="Series (coma o salto de línea)">
                <Input.TextArea rows={2} />
              </Form.Item>
              <Form.Item name="indAutogenerar" label="Autogenerar series" valuePropName="checked">
                <Switch />
              </Form.Item>
              <Form.Item name="indCorrelativo" label="Series correlativas" valuePropName="checked">
                <Switch />
              </Form.Item>
              <Button type="primary" htmlType="submit" loading={crearDet.isPending}>
                Agregar línea
              </Button>
            </Form>
          ) : null}
        </>
      ),
    },
    {
      title: 'Confirmar',
      content: cab ? (
        <Space direction="vertical" style={{ width: '100%' }}>
          <Text>
            Subtotal {formatMoney(cab.subTotal)} + IGV {formatMoney(cab.igv)} ={' '}
            <Text strong>{formatMoney(cab.totalImporte)}</Text>
          </Text>
          {editable ? (
            <Form
              form={ajusteForm}
              layout="inline"
              onFinish={(v) =>
                ajuste.mutate({
                  oficinaId,
                  movimientoId,
                  ajusteRedondeo: v.ajusteRedondeo ?? 0,
                })
              }
            >
              <Form.Item name="ajusteRedondeo" label="Ajuste redondeo">
                <InputNumber step={0.01} />
              </Form.Item>
              <Form.Item>
                <Button htmlType="submit" loading={ajuste.isPending}>
                  Recalcular total
                </Button>
              </Form.Item>
            </Form>
          ) : null}
          <Space wrap>
            {editable ? (
              <Button
                type="primary"
                loading={confirmar.isPending}
                onClick={() => confirmar.mutate({ oficinaId, movimientoId })}
              >
                Confirmar entrada
              </Button>
            ) : (
              <Button
                loading={desconfirmar.isPending}
                onClick={() => desconfirmar.mutate({ oficinaId, movimientoId })}
              >
                Desconfirmar (volver a pendiente)
              </Button>
            )}
          </Space>
          <Paragraph type="secondary" style={{ marginBottom: 0 }}>
            Al confirmar se actualiza el movimiento y el stock queda registrado.
          </Paragraph>
        </Space>
      ) : null,
    },
  ]

  const stats = useMemo((): CredixStatItem[] => {
    if (movimientoId > 0 && detalle.data) {
      return [
        { value: movimientoId, label: 'Movimiento' },
        { value: detalle.data.detalle?.length ?? 0, label: 'Líneas detalle' },
        { value: detalle.data.documentos?.length ?? 0, label: 'Documentos' },
        { value: `Paso ${step + 1}`, label: 'Asistente' },
      ]
    }
    return [
      {
        value: listado.data?.totalCount ?? listado.data?.items?.length ?? 0,
        label: 'Entradas en lista',
      },
    ]
  }, [movimientoId, detalle.data, step, listado.data])

  return (
    <CredixPage
      title="Entrada de almacén"
      subtitle="Alta por pasos: cabecera, documentos, detalle y confirmación."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/almacen">Almacén</Link> },
        { title: 'Entrada' },
      ]}
    >
      {oficinaId < 1 ? (
        <Alert type="warning" showIcon message="Sesión sin oficina válida." />
      ) : movimientoId < 1 ? (
        <>
          <CredixPanel>
            <Space wrap>
              <Select
                placeholder="Almacén"
                style={{ width: 200 }}
                value={almacenId ?? undefined}
                onChange={setAlmacenId}
                options={(almacenes.data ?? []).map((a) => ({
                  value: a.almacenId,
                  label: a.denominacion,
                }))}
              />
              <Input.Search
                placeholder="Buscar id, documento o fecha"
                allowClear
                style={{ width: 240 }}
                onSearch={(v) => {
                  setBuscar(v)
                  setPage(1)
                }}
              />
              <InputNumber
                placeholder="Filtrar artículo ID"
                min={0}
                style={{ width: 140 }}
                value={articuloFiltro ?? undefined}
                onChange={(v) => {
                  setArticuloFiltro(v ?? null)
                  setPage(1)
                }}
              />
              <Button
                icon={<ReloadOutlined />}
                onClick={() => void listado.refetch()}
                loading={listado.isFetching}
              >
                Actualizar
              </Button>
              <Button
                type="primary"
                icon={<PlusOutlined />}
                disabled={!almacenIdEfectivo}
                onClick={() => setNuevaOpen(true)}
              >
                Nueva entrada
              </Button>
            </Space>
          </CredixPanel>
          {listado.isError ? (
            <Alert type="error" showIcon message={errMsg(listado.error)} />
          ) : (
            <CredixDataTable<MovimientoEntradaListRow>
              rowKey="movimientoId"
              size="small"
              loading={listado.isLoading}
              dataSource={listado.data?.items ?? []}
              columns={listColumns}
              pagination={{
                current: page,
                pageSize,
                total: listado.data?.totalCount ?? 0,
                showSizeChanger: true,
                onChange: (p, ps) => {
                  setPage(p)
                  setPageSize(ps)
                },
              }}
            />
          )}
        </>
      ) : (
        <>
          <CredixPanel>
            <Space wrap>
              <Button onClick={() => setSearchParams({})}>← Listado</Button>
              <Button icon={<ReloadOutlined />} onClick={refrescar} loading={detalle.isFetching}>
                Actualizar
              </Button>
              {movimientoId > 0 ? (
                <Link to={`/almacen/constancia?movimientoId=${movimientoId}`}>
                  <Button>Constancia</Button>
                </Link>
              ) : null}
              {cab ? (
                <Text type="secondary">
                  #{cab.movimientoId} · {cab.almacen} · {cab.estado}
                </Text>
              ) : null}
            </Space>
          </CredixPanel>
          {detalle.isError ? (
            <Alert type="error" showIcon message={errMsg(detalle.error)} />
          ) : (
            <>
              <Steps
                current={step}
                onChange={setStep}
                items={wizardSteps.map((s) => ({ title: s.title }))}
                style={{ marginBottom: 24 }}
              />
              <CredixPanel>{wizardSteps[step]?.content}</CredixPanel>
            </>
          )}
        </>
      )}

      <Modal
        title="Nueva entrada"
        open={nuevaOpen}
        onCancel={() => setNuevaOpen(false)}
        onOk={() => {
          if (!tipoNuevo) {
            message.warning('Seleccione tipo de movimiento')
            return
          }
          crear.mutate()
        }}
        confirmLoading={crear.isPending}
        okText="Crear"
      >
        <Form layout="vertical">
          <Form.Item label="Almacén">
            <Select
              value={almacenId ?? undefined}
              onChange={setAlmacenId}
              options={(almacenes.data ?? []).map((a) => ({
                value: a.almacenId,
                label: a.denominacion,
              }))}
            />
          </Form.Item>
          <Form.Item label="Tipo de entrada" required>
            <Select
              placeholder="Tipo movimiento"
              value={tipoNuevo ?? undefined}
              onChange={setTipoNuevo}
              options={(tiposEntrada.data ?? []).map((t) => ({
                value: t.tipoMovimientoId,
                label: t.denominacion,
              }))}
            />
          </Form.Item>
        </Form>
      </Modal>
    </CredixPage>
  )
}
