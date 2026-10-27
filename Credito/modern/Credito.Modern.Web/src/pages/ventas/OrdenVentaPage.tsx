import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ExclamationCircleOutlined, PlusOutlined, ReloadOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Form,
  Input,
  InputNumber,
  Modal,
  Space,
  Spin,
  Switch,
  Tabs,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { CompradorVentaField } from '../../components/ventas/CompradorVentaField'
import {
  actualizarOrdenVentaDetalle,
  agregarOrdenVentaDetalle,
  crearOrdenVenta,
  eliminarOrdenVenta,
  eliminarOrdenVentaDetalle,
  enviarOrdenVentaContado,
  enviarOrdenVentaCredito,
  fetchOrdenVentaDetalle,
  fetchOrdenesVenta,
  type OrdenVentaDetLinea,
  type OrdenVentaListRow,
} from '../../api/ventas'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import {
  CredixDataTable,
  CredixListToolbar,
  CredixPage,
  CredixPanel,
  CredixSelect,
  type CredixStatItem,
} from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

const numRule = (min: number) => [{ required: true, type: 'number' as const, min }]

function obtenerCondicion(estado: string, tipoVenta: string): string {
  if (estado === 'ANU') return 'ANULADO'
  if (estado === 'ENT') return 'ENTREGADO'
  if (estado === 'PEN') return 'PENDIENTE'
  if (estado === 'ENV' && tipoVenta === 'CON') return 'POR COBRAR'
  if (estado === 'ENV' && tipoVenta === 'CRE') return 'EN EVALUACIÓN'
  return estado
}

function obtenerTipoOv(tipoVenta: string, estadoCredito: string | null): string {
  if (tipoVenta === 'CON') return 'CONTADO'
  if (tipoVenta === 'CRE') return `CRÉDITO ${estadoCredito ?? ''}`.trim()
  return tipoVenta
}

export function OrdenVentaPage() {
  const { session } = useAuth()
  const queryClient = useQueryClient()
  const oficinaId = session?.oficinaId ?? 0
  const [searchParams, setSearchParams] = useSearchParams()
  const ordenVentaId = Number(searchParams.get('ordenVentaId') || 0)

  const [entregado, setEntregado] = useState(false)
  const [buscar, setBuscar] = useState('')
  const [buscarInput, setBuscarInput] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [nuevaOpen, setNuevaOpen] = useState(false)
  const [terminoCliente, setTerminoCliente] = useState('')
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')
  const [tipoVentaNueva, setTipoVentaNueva] = useState<'CON' | 'CRE'>('CON')

  const [serieForm] = Form.useForm()
  const [descForm] = Form.useForm()

  const listado = useQuery({
    queryKey: ['ordenes-venta', oficinaId, entregado, buscar, page, pageSize],
    queryFn: () =>
      fetchOrdenesVenta({ oficinaId, entregado, buscar, page, pageSize }),
    enabled: oficinaId > 0 && ordenVentaId < 1,
  })

  const detalle = useQuery({
    queryKey: ['orden-venta-detalle', oficinaId, ordenVentaId],
    queryFn: () => fetchOrdenVentaDetalle(oficinaId, ordenVentaId),
    enabled: oficinaId > 0 && ordenVentaId > 0,
  })

  useEffect(() => {
    if (ordenVentaId > 0) {
      serieForm.setFieldValue('ordenVentaId', ordenVentaId)
    }
  }, [ordenVentaId, serieForm])

  const refrescarDetalle = () => {
    void queryClient.invalidateQueries({ queryKey: ['orden-venta-detalle'] })
    void queryClient.invalidateQueries({ queryKey: ['ordenes-venta'] })
  }

  const crear = useMutation({
    mutationFn: () =>
      crearOrdenVenta({
        oficinaId,
        personaId: personaId!,
        tipoVenta: tipoVentaNueva,
      }),
    onSuccess: (r) => {
      message.success(`Orden #${r.ordenVentaId} creada`)
      setNuevaOpen(false)
      setPersonaId(null)
      setClienteLabel('')
      setTerminoCliente('')
      setSearchParams({ ordenVentaId: String(r.ordenVentaId) })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const agregar = useMutation({
    mutationFn: agregarOrdenVentaDetalle,
    onSuccess: (r) => {
      message.success(r.mensaje || 'Serie agregada')
      serieForm.resetFields(['numeroSerie'])
      refrescarDetalle()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const actualizar = useMutation({
    mutationFn: actualizarOrdenVentaDetalle,
    onSuccess: (r) => {
      message.success(`Actualizado (código ${r.resultCode})`)
      refrescarDetalle()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const eliminarDet = useMutation({
    mutationFn: eliminarOrdenVentaDetalle,
    onSuccess: (r) => {
      message.success(`Detalle eliminado (código ${r.resultCode})`)
      refrescarDetalle()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const eliminarOv = useMutation({
    mutationFn: eliminarOrdenVenta,
    onSuccess: (r) => {
      message.success(`Orden eliminada (código ${r.resultCode})`)
      setSearchParams({})
      refrescarDetalle()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const enviarContado = useMutation({
    mutationFn: enviarOrdenVentaContado,
    onSuccess: (r) => message.success(`Orden #${r.ordenVentaId} enviada al contado`),
    onError: (e) => message.error(errMsg(e)),
  })

  const enviarCredito = useMutation({
    mutationFn: enviarOrdenVentaCredito,
    onSuccess: (r) =>
      message.success(
        `Orden #${r.ordenVentaId} a crédito${r.creditoId ? ` (crédito #${r.creditoId})` : ''}`,
      ),
    onError: (e) => message.error(errMsg(e)),
  })

  const confirmar = (
    title: string,
    content: string,
    onOk: () => Promise<unknown>,
  ) => {
    Modal.confirm({
      title,
      icon: <ExclamationCircleOutlined />,
      content,
      okText: 'Confirmar',
      cancelText: 'Cancelar',
      onOk,
    })
  }

  const listColumns: ColumnsType<OrdenVentaListRow> = [
    { title: 'Id', dataIndex: 'ordenVentaId', width: 70 },
    {
      title: 'Fecha',
      dataIndex: 'fechaReg',
      width: 100,
      render: formatFecha,
    },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    {
      title: 'Desc.',
      dataIndex: 'totalDescuento',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total',
      dataIndex: 'totalNeto',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Tipo',
      key: 'tipo',
      width: 130,
      render: (_, r) => obtenerTipoOv(r.tipoVenta, r.estadoCredito),
    },
    {
      title: 'Condición',
      key: 'condicion',
      width: 120,
      render: (_, r) => (
        <Tag>{obtenerCondicion(r.estado, r.tipoVenta)}</Tag>
      ),
    },
    {
      title: '',
      key: 'act',
      width: 90,
      render: (_, r) => (
        <Button
          type="link"
          size="small"
          onClick={() => setSearchParams({ ordenVentaId: String(r.ordenVentaId) })}
        >
          Abrir
        </Button>
      ),
    },
  ]

  const detColumns: ColumnsType<OrdenVentaDetLinea> = [
    { title: 'Det. ID', dataIndex: 'ordenVentaDetId', width: 80 },
    { title: 'Artículo', dataIndex: 'descripcion', ellipsis: true },
    { title: 'Cant.', dataIndex: 'cantidad', width: 60 },
    {
      title: 'Valor',
      dataIndex: 'valorVenta',
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
      title: 'Subtotal',
      dataIndex: 'subtotal',
      width: 90,
      align: 'right',
      render: formatMoney,
    },
    {
      title: '',
      key: 'act',
      width: 80,
      render: (_, r) =>
        r.estado ? (
          <Button
            type="link"
            danger
            size="small"
            onClick={() =>
              confirmar(
                'Eliminar línea',
                `¿Eliminar detalle #${r.ordenVentaDetId}?`,
                () =>
                  eliminarDet.mutateAsync({
                    oficinaId,
                    ordenVentaDetId: r.ordenVentaDetId,
                  }),
              )
            }
          >
            Quitar
          </Button>
        ) : (
          <Tag>Inactivo</Tag>
        ),
    },
  ]

  const lineasActivas = (detalle.data?.detalle ?? []).filter((d) => d.estado)

  const stats = useMemo((): CredixStatItem[] => {
    if (ordenVentaId > 0) {
      const lineas = detalle.data?.detalle?.length ?? 0
      return [
        { value: ordenVentaId, label: 'Orden actual' },
        { value: lineas, label: 'Líneas detalle' },
      ]
    }
    const total = listado.data?.totalCount ?? listado.data?.items?.length ?? 0
    return [{ value: total, label: 'Órdenes en lista' }]
  }, [ordenVentaId, detalle.data, listado.data])

  return (
    <CredixPage
      className="orden-venta-page"
      title="Orden de venta"
      subtitle="Primero el cliente. Luego agregue series que ya estén en almacén."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/ventas">Ventas</Link> },
        { title: 'Orden de venta' },
      ]}
      actions={
        oficinaId > 0 && ordenVentaId < 1 ? (
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setNuevaOpen(true)}>
            Nueva orden
          </Button>
        ) : ordenVentaId > 0 ? (
          <>
            <Button onClick={() => setSearchParams({})}>Volver al listado</Button>
            <Button
              icon={<ReloadOutlined />}
              onClick={() => void detalle.refetch()}
              loading={detalle.isFetching}
            >
              Actualizar
            </Button>
            {detalle.data?.cabecera.puedeEliminar ? (
              <Button
                danger
                loading={eliminarOv.isPending}
                onClick={() =>
                  confirmar(
                    'Eliminar orden',
                    `¿Eliminar la orden #${ordenVentaId} completa?`,
                    () => eliminarOv.mutateAsync({ oficinaId, ordenVentaId }),
                  )
                }
              >
                Eliminar orden
              </Button>
            ) : null}
          </>
        ) : null
      }
      toolbar={
        oficinaId > 0 && ordenVentaId < 1 ? (
          <CredixListToolbar
            value={buscarInput}
            onChange={setBuscarInput}
            onSubmit={() => {
              setBuscar(buscarInput.trim())
              setPage(1)
            }}
            placeholder="Buscar id, cliente o fecha"
            loading={listado.isFetching}
            onRefresh={() => void listado.refetch()}
            extra={
              <label className="credix-toolbar-switch">
                Entregados
                <Switch
                  checked={entregado}
                  onChange={(v) => {
                    setEntregado(v)
                    setPage(1)
                  }}
                />
              </label>
            }
          />
        ) : undefined
      }
    >
      {oficinaId < 1 ? (
        <Alert type="warning" showIcon message="Sesión sin oficina válida." />
      ) : ordenVentaId < 1 ? (
        <>

          {listado.isError ? (
            <Alert type="error" showIcon message={errMsg(listado.error)} />
          ) : (
            <CredixDataTable<OrdenVentaListRow>
              rowKey="ordenVentaId"
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
          {detalle.isError ? (
            <Alert type="error" showIcon message={errMsg(detalle.error)} />
          ) : detalle.data ? (
            <>
              <CredixPanel>
                <Space direction="vertical" size={4}>
                  <Text strong>
                    Orden #{detalle.data.cabecera.ordenVentaId} —{' '}
                    {detalle.data.cabecera.cliente}
                  </Text>
                  <Text type="secondary">
                    {formatFecha(detalle.data.cabecera.fechaReg)} ·{' '}
                    {obtenerTipoOv(
                      detalle.data.cabecera.tipoVenta,
                      detalle.data.cabecera.estadoCredito,
                    )}{' '}
                    ·{' '}
                    <Tag>
                      {obtenerCondicion(
                        detalle.data.cabecera.estado,
                        detalle.data.cabecera.tipoVenta,
                      )}
                    </Tag>
                  </Text>
                  <Text>
                    Total neto: {formatMoney(detalle.data.cabecera.totalNeto)} ·{' '}
                    {detalle.data.cantidadTotal} unidad(es)
                  </Text>
                </Space>
              </CredixPanel>

              <CredixDataTable<OrdenVentaDetLinea>
                rowKey="ordenVentaDetId"
                size="small"
                style={{ marginBottom: 16 }}
                loading={detalle.isLoading}
                dataSource={lineasActivas}
                columns={detColumns}
                pagination={false}
                locale={{
                  emptyText:
                    'Sin líneas. Agregue una serie que ya esté en stock.',
                }}
              />

              {lineasActivas.length === 0 ? (
                <Alert
                  type="info"
                  showIcon
                  style={{ marginBottom: 16 }}
                  message="La orden se arma con series del almacén"
                  description={
                    <Space wrap>
                      <Link to="/almacen/entrada">Dar entrada</Link>
                      <Link to="/maestros/articulos">Ver artículos</Link>
                    </Space>
                  }
                />
              ) : null}

              <Tabs
                className="credix-tabs"
                items={[
                  {
                    key: 'serie',
                    label: 'Agregar serie',
                    children: (
                      <Form
                        form={serieForm}
                        layout="vertical"
                        style={{ maxWidth: 440 }}
                        initialValues={{ ordenVentaId }}
                        onFinish={(v) =>
                          agregar.mutate({
                            oficinaId,
                            ordenVentaId,
                            numeroSerie: v.numeroSerie.trim(),
                          })
                        }
                      >
                        <Form.Item
                          name="numeroSerie"
                          label="Número de serie"
                          extra="Es la serie registrada al dar entrada, no el id del artículo."
                          rules={[{ required: true, message: 'Ingrese la serie' }]}
                        >
                          <Input placeholder="Serie del artículo en almacén" autoComplete="off" />
                        </Form.Item>
                        <Button type="primary" htmlType="submit" loading={agregar.isPending}>
                          Agregar a la orden
                        </Button>
                      </Form>
                    ),
                  },
                  {
                    key: 'desc',
                    label: 'Descuento',
                    children: (
                      <Form
                        form={descForm}
                        layout="vertical"
                        style={{ maxWidth: 360 }}
                        onFinish={(v) =>
                          actualizar.mutate({
                            oficinaId,
                            ordenVentaDetId: v.ordenVentaDetId,
                            descuento: v.descuento,
                          })
                        }
                      >
                        <Form.Item
                          name="ordenVentaDetId"
                          label="Línea"
                          rules={[{ required: true, message: 'Elija la línea' }]}
                        >
                          <CredixSelect
                            placeholder="Artículo de la orden"
                            options={lineasActivas.map((d) => ({
                              value: d.ordenVentaDetId,
                              label: d.descripcion,
                            }))}
                            notFoundContent="Primero agregue una serie."
                          />
                        </Form.Item>
                        <Form.Item name="descuento" label="Descuento" rules={numRule(0)}>
                          <InputNumber style={{ width: '100%' }} min={0} step={0.01} />
                        </Form.Item>
                        <Button type="primary" htmlType="submit" loading={actualizar.isPending}>
                          Aplicar descuento
                        </Button>
                      </Form>
                    ),
                  },
                  {
                    key: 'enviar',
                    label: 'Enviar',
                    children: (
                      <Space direction="vertical">
                        <Text type="secondary">
                          Envíe cuando las líneas estén listas. Contado cobra; crédito pasa a evaluación.
                        </Text>
                        <Space wrap>
                          <Button
                            type="primary"
                            loading={enviarContado.isPending}
                            disabled={lineasActivas.length === 0}
                            onClick={() =>
                              confirmar(
                                'Enviar al contado',
                                `¿Enviar la orden #${ordenVentaId} al contado?`,
                                () =>
                                  enviarContado.mutateAsync({ oficinaId, ordenVentaId }),
                              )
                            }
                          >
                            Enviar contado
                          </Button>
                          <Button
                            loading={enviarCredito.isPending}
                            disabled={lineasActivas.length === 0}
                            onClick={() =>
                              confirmar(
                                'Enviar a crédito',
                                `¿Enviar la orden #${ordenVentaId} a crédito?`,
                                () =>
                                  enviarCredito.mutateAsync({ oficinaId, ordenVentaId }),
                              )
                            }
                          >
                            Enviar a crédito
                          </Button>
                        </Space>
                      </Space>
                    ),
                  },
                ]}
              />
            </>
          ) : (
            <CredixPanel>
              <Spin />
            </CredixPanel>
          )}
        </>
      )}

      <Modal
        title="Nueva orden de venta"
        open={nuevaOpen}
        onCancel={() => setNuevaOpen(false)}
        onOk={() => {
          if (!personaId) {
            message.warning('Seleccione un cliente')
            return
          }
          crear.mutate()
        }}
        confirmLoading={crear.isPending}
        okText="Crear orden"
      >
        <Form layout="vertical">
          <Form.Item label="Tipo de venta">
            <CredixSelect
              value={tipoVentaNueva}
              onChange={setTipoVentaNueva}
              options={[
                { value: 'CON', label: 'Contado' },
                { value: 'CRE', label: 'Crédito' },
              ]}
            />
          </Form.Item>
          <Form.Item label="Cliente" required>
            <CompradorVentaField
              personaId={personaId}
              clienteLabel={clienteLabel}
              termino={terminoCliente}
              onTerminoChange={setTerminoCliente}
              onSelect={(id, label) => {
                setPersonaId(id)
                setClienteLabel(label)
              }}
              onClear={() => {
                setPersonaId(null)
                setClienteLabel('')
                setTerminoCliente('')
              }}
            />
          </Form.Item>
        </Form>
      </Modal>
    </CredixPage>
  )
}
