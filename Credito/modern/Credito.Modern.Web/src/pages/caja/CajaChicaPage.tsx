import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Spin,
  Tabs,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  ExclamationCircleOutlined,
  FilePdfOutlined,
} from '@ant-design/icons'
import { fetchValidarCierreCajaChica } from '../../api/boveda'
import {
  buscarUsuarios,
  cerrarCajaChicaDiario,
  cerrarRendicionCajaChica,
  crearRendicionCajaChica,
  eliminarRendicionCajaChica,
  entradaSalidaCajaChica,
  fetchCajaChicaSesion,
  fetchMontoCajaChica,
  downloadMovimientoCajaChicaTicketPdf,
  fetchMovimientosCajaChica,
  fetchRendicionesCajaChica,
  fetchRendicionesPendientesCajaChica,
  fetchTipoOperacionesCajaChica,
  fetchTieneRendicionesPendientesCajaChica,
  fetchTiposDocumentoCajaChica,
  transferirSaldosCajaChicaBoveda,
  type MovimientoCajaChicaRow,
  type RendicionComprobanteRow,
  type RendicionPendienteRow,
} from '../../api/cajaChica'
import { fetchRptSaldosCaja } from '../../api/cajaDiario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { ClienteBuscarItem } from '../../types/api'
import {
  CredixDataTable,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { filterTableRows } from '../../utils/tableClientFilter'
import { formatMoney } from '../../utils/formatMoney'
import { CajaListToolbar } from './components/CajaListToolbar'

const { Paragraph, Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

function movRowText(r: MovimientoCajaChicaRow): string {
  return [r.fechaReg, r.persona, r.operacion, r.descripcion, r.importePago, r.estado].join(' ')
}

function rendPendText(r: RendicionPendienteRow): string {
  return [r.cliente, r.descripcion, r.fechaReg, r.importe, r.importeRendido].join(' ')
}

export function CajaChicaPage() {
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [tab, setTab] = useState('gastos')
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [usuarioLabel, setUsuarioLabel] = useState('')
  const [terminoUsuario, setTerminoUsuario] = useState('')
  const terminoDebounced = useDebouncedValue(terminoUsuario.trim(), 400)
  const [movimientoRendicionId, setMovimientoRendicionId] = useState<number | null>(null)
  const [rendicionLabel, setRendicionLabel] = useState('')
  const [filtroRendicion, setFiltroRendicion] = useState('')
  const [filtroArqueo, setFiltroArqueo] = useState('')
  const filtroRendDebounced = useDebouncedValue(filtroRendicion.trim(), 300)
  const filtroArqDebounced = useDebouncedValue(filtroArqueo.trim(), 300)
  const [transferOpen, setTransferOpen] = useState(false)
  const [transferImporte, setTransferImporte] = useState<number | null>(null)
  const [transferDesc, setTransferDesc] = useState('')
  const [gastoForm] = Form.useForm()

  const sesion = useQuery({
    queryKey: ['caja-chica-sesion'],
    queryFn: fetchCajaChicaSesion,
    retry: false,
  })

  const sesionOk = sesion.isSuccess && sesion.data != null

  const validacion = useQuery({
    queryKey: ['validar-cierre-caja-chica', oficinaId],
    queryFn: () => fetchValidarCierreCajaChica(oficinaId),
    enabled: oficinaId > 0 && tab === 'arqueo',
  })

  const tipoOps = useQuery({
    queryKey: ['tipo-operaciones-caja-chica'],
    queryFn: fetchTipoOperacionesCajaChica,
    enabled: tab === 'gastos',
  })

  const tiposDoc = useQuery({
    queryKey: ['tipos-documento-caja-chica'],
    queryFn: fetchTiposDocumentoCajaChica,
    enabled: tab === 'rendicion',
  })

  const busquedaUsuario = useQuery({
    queryKey: ['buscar-usuario-caja-chica', terminoDebounced],
    queryFn: () => buscarUsuarios(terminoDebounced),
    enabled: sesionOk && tab === 'gastos' && terminoDebounced.length >= 2,
  })

  const entradas = useQuery({
    queryKey: ['movimientos-caja-chica', 'E'],
    queryFn: () => fetchMovimientosCajaChica('E'),
    enabled: sesionOk && tab === 'arqueo',
  })

  const salidas = useQuery({
    queryKey: ['movimientos-caja-chica', 'S'],
    queryFn: () => fetchMovimientosCajaChica('S'),
    enabled: sesionOk && tab === 'arqueo',
  })

  const pendientes = useQuery({
    queryKey: ['rendiciones-pendientes-caja-chica'],
    queryFn: fetchRendicionesPendientesCajaChica,
    enabled: sesionOk && tab === 'rendicion',
  })

  const comprobantes = useQuery({
    queryKey: ['rendiciones-caja-chica', movimientoRendicionId],
    queryFn: () => fetchRendicionesCajaChica(movimientoRendicionId!),
    enabled: movimientoRendicionId != null && movimientoRendicionId > 0,
  })

  const arqueo = useQuery({
    queryKey: ['rpt-saldos-caja-chica', sesion.data?.id],
    queryFn: () => fetchRptSaldosCaja(sesion.data!.id, true),
    enabled: sesionOk && tab === 'arqueo',
  })

  const refrescar = () => {
    void queryClient.invalidateQueries({ queryKey: ['caja-chica-sesion'] })
    void queryClient.invalidateQueries({ queryKey: ['movimientos-caja-chica'] })
    void queryClient.invalidateQueries({ queryKey: ['rendiciones-pendientes-caja-chica'] })
    void queryClient.invalidateQueries({ queryKey: ['rendiciones-caja-chica'] })
    void queryClient.invalidateQueries({ queryKey: ['rpt-saldos-caja-chica'] })
  }

  const gasto = useMutation({
    mutationFn: (v: {
      tipoOperacionId: number
      importe: number
      descripcion: string
    }) =>
      entradaSalidaCajaChica({
        oficinaId,
        personaId: personaId!,
        tipoOperacionId: v.tipoOperacionId,
        importe: v.importe,
        descripcion: v.descripcion,
      }),
    onSuccess: () => {
      message.success('Movimiento registrado')
      gastoForm.resetFields(['importe', 'descripcion'])
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const agregarRendicion = useMutation({
    mutationFn: crearRendicionCajaChica,
    onSuccess: () => {
      message.success('Comprobante agregado')
      void comprobantes.refetch()
      void pendientes.refetch()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cerrarRend = useMutation({
    mutationFn: cerrarRendicionCajaChica,
    onSuccess: (r) => {
      message.success(
        r.devolucion > 0
          ? `Rendición cerrada — devolución S/ ${formatMoney(r.devolucion)}`
          : 'Rendición cerrada',
      )
      setMovimientoRendicionId(null)
      setRendicionLabel('')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const eliminarComp = useMutation({
    mutationFn: eliminarRendicionCajaChica,
    onSuccess: () => {
      message.success('Comprobante eliminado')
      void comprobantes.refetch()
      void pendientes.refetch()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cerrarSesion = useMutation({
    mutationFn: async () => {
      const pend = await fetchTieneRendicionesPendientesCajaChica()
      if (pend > 0) {
        throw new Error(`Hay ${pend} rendición(es) pendiente(s).`)
      }
      return cerrarCajaChicaDiario()
    },
    onSuccess: () => {
      message.success('Caja chica cerrada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const transferir = useMutation({
    mutationFn: transferirSaldosCajaChicaBoveda,
    onSuccess: () => {
      message.success('Transferencia a bóveda registrada')
      setTransferOpen(false)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const ticket = useMutation({
    mutationFn: downloadMovimientoCajaChicaTicketPdf,
    onSuccess: () => message.success('Ticket descargado'),
    onError: (e) => message.error(errMsg(e)),
  })

  const descargarTicket = useCallback(
    (movimientoCajaChicaId: number) => ticket.mutate(movimientoCajaChicaId),
    [ticket],
  )

  const pendientesFiltrados = useMemo(
    () => filterTableRows(pendientes.data ?? [], filtroRendDebounced, rendPendText),
    [pendientes.data, filtroRendDebounced],
  )

  const entradasFiltradas = useMemo(
    () => filterTableRows(entradas.data ?? [], filtroArqDebounced, movRowText),
    [entradas.data, filtroArqDebounced],
  )

  const salidasFiltradas = useMemo(
    () => filterTableRows(salidas.data ?? [], filtroArqDebounced, movRowText),
    [salidas.data, filtroArqDebounced],
  )

  const colsMov: ColumnsType<MovimientoCajaChicaRow> = useMemo(
    () => [
      { title: 'Fecha', dataIndex: 'fechaReg', width: 160 },
      { title: 'Persona', dataIndex: 'persona', ellipsis: true },
      { title: 'Operación', dataIndex: 'operacion', width: 120 },
      { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
      {
        title: 'Importe',
        dataIndex: 'importePago',
        width: 100,
        align: 'right',
        render: (v: number) => formatMoney(v),
      },
      {
        title: 'Estado',
        dataIndex: 'estado',
        width: 90,
        render: (v: boolean) => (v ? 'ACTIVO' : 'ANULADO'),
      },
      {
        title: '',
        width: 90,
        render: (_, row) => (
          <Button
            size="small"
            icon={<FilePdfOutlined />}
            loading={ticket.isPending}
            onClick={() => descargarTicket(row.movimientoCajaChicaId)}
          >
            Ticket
          </Button>
        ),
      },
    ],
    [descargarTicket, ticket.isPending],
  )

  const colsPend: ColumnsType<RendicionPendienteRow> = [
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
    { title: 'Fecha', dataIndex: 'fechaReg', width: 160 },
    {
      title: 'Importe',
      dataIndex: 'importe',
      width: 100,
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    {
      title: 'Rendido',
      dataIndex: 'importeRendido',
      width: 100,
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    {
      title: '',
      width: 100,
      render: (_, row) => (
        <Button
          size="small"
          type="link"
          onClick={() => {
            setMovimientoRendicionId(row.movimientoCajaChicaId)
            setRendicionLabel(`${row.cliente} — S/ ${formatMoney(row.importe)}`)
          }}
        >
          Rendir
        </Button>
      ),
    },
  ]

  const colsComp: ColumnsType<RendicionComprobanteRow> = [
    { title: 'Tipo doc.', dataIndex: 'tipoDocumento', width: 110 },
    { title: 'RUC', dataIndex: 'ruc', width: 110 },
    { title: 'Razón social', dataIndex: 'razonSocial', ellipsis: true },
    { title: 'Detalle', dataIndex: 'detalleGasto', ellipsis: true },
    { title: 'Serie', dataIndex: 'serie', width: 80 },
    { title: 'Número', dataIndex: 'numero', width: 90 },
    {
      title: 'Importe',
      dataIndex: 'importe',
      width: 90,
      align: 'right',
      render: (v: number) => formatMoney(v),
    },
    {
      title: '',
      width: 80,
      render: (_, row) => (
        <Button
          size="small"
          danger
          onClick={() => eliminarComp.mutate(row.id)}
          loading={eliminarComp.isPending}
        >
          Quitar
        </Button>
      ),
    },
  ]

  const sinSesion =
    sesion.isError && sesion.error instanceof ApiError && sesion.error.status === 404

  const chicaStats: CredixStatItem[] = useMemo(() => {
    const s = sesion.data
    if (!s) return []
    return [
      { value: formatMoney(s.saldoInicial), label: 'Saldo inicial (S/.)', tone: 'green' },
      { value: formatMoney(s.entradas), label: 'Entradas (S/.)', tone: 'green' },
      { value: formatMoney(s.salidas), label: 'Salidas (S/.)', tone: 'green' },
      { value: formatMoney(s.saldoFinal), label: 'Neto caja (S/.)', tone: 'red' },
      {
        value: s.indCierre ? 'CERRADO' : 'ABIERTO',
        label: 'Caja chica',
        detail: new Date(s.fechaIniOperacion).toLocaleString('es-PE'),
      },
      {
        value: pendientes.data?.length ?? '—',
        label: 'Rend. pendientes',
      },
    ]
  }, [sesion.data, pendientes.data])

  const tabLoading =
    (tab === 'gastos' && (tipoOps.isFetching || busquedaUsuario.isFetching)) ||
    (tab === 'rendicion' && pendientes.isFetching) ||
    (tab === 'arqueo' &&
      (entradas.isFetching || salidas.isFetching || arqueo.isFetching))

  const confirmarGasto = (values: {
    tipoOperacionId: number
    importe: number
    descripcion: string
  }) => {
    if (personaId == null) {
      message.warning('Seleccione usuario')
      return
    }
    Modal.confirm({
      title: 'Confirmar operación',
      icon: <ExclamationCircleOutlined />,
      content: `¿Registrar gasto/operación por S/ ${formatMoney(values.importe)}?`,
      okText: 'Sí, registrar',
      cancelText: 'Cancelar',
      onOk: () => gasto.mutateAsync(values),
    })
  }

  useEffect(() => {
    if (personaId != null) {
      setTerminoUsuario('')
    }
  }, [personaId])

  return (
    <CredixPage
      className="caja-chica-page credix-page--stats-3"
      title="Caja chica"
      subtitle="Gastos, rendición de comprobantes y arqueo — pestañas como CajaChica/Index del MVC."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: 'Caja chica' },
      ]}
      stats={chicaStats}
      actions={
        <Space wrap>
          <Link to="/caja/asignar">
            <Button type="primary">Asignar caja</Button>
          </Link>
          <Button onClick={refrescar} loading={sesion.isFetching}>
            Actualizar
          </Button>
        </Space>
      }
    >
      {sinSesion ? (
        <Alert
          className="caja-chica-page__sesion-alert"
          type="warning"
          showIcon
          message="No hay caja chica abierta"
          description="Asigne la caja «CAJA CHICA» desde Asignar caja antes de operar."
          action={
            <Link to="/caja/asignar">
              <Button size="small">Ir a asignar</Button>
            </Link>
          }
        />
      ) : sesion.data ? (
        <>
          <p className="credix-module-banner credix-module-banner--spaced">
            <strong>Paridad MVC:</strong> GASTOS · RENDICIÓN (pendientes + comprobantes) · ARQUEO
            (entradas/salidas, cierre y transferir a bóveda en la misma pestaña).
          </p>

          <Tabs
            className="credix-tabs"
            activeKey={tab}
            onChange={setTab}
            destroyInactiveTabPane
            items={[
              {
                key: 'gastos',
                label: 'Gastos',
                children: (
                  <CredixPanel title="MOVIMIENTOS CAJA CHICA — GASTOS">
                    <Form
                      className="caja-chica-gastos-form"
                      form={gastoForm}
                      layout="vertical"
                      onFinish={confirmarGasto}
                    >
                      <Row gutter={16}>
                        <Col xs={24} md={12}>
                          {personaId == null ? (
                            <>
                              <Text type="secondary">Usuario</Text>
                              <Input
                                style={{ marginTop: 4 }}
                                placeholder="Buscar usuario (mín. 2 caracteres)"
                                value={terminoUsuario}
                                onChange={(e) => setTerminoUsuario(e.target.value)}
                                allowClear
                              />
                              {busquedaUsuario.isFetching ? (
                                <Spin size="small" style={{ marginTop: 8 }} />
                              ) : null}
                              {(busquedaUsuario.data?.length ?? 0) > 0 && (
                                <CredixDataTable<ClienteBuscarItem>
                                  mode="operacion"
                                  className="caja-chica-table"
                                  size="small"
                                  style={{ marginTop: 8 }}
                                  rowKey="personaId"
                                  pagination={false}
                                  dataSource={busquedaUsuario.data}
                                  columns={[
                                    { title: 'Usuario', dataIndex: 'label' },
                                    {
                                      title: '',
                                      width: 80,
                                      render: (_, row) => (
                                        <Button
                                          size="small"
                                          type="primary"
                                          onClick={() => {
                                            setPersonaId(row.personaId)
                                            setUsuarioLabel(row.label)
                                          }}
                                        >
                                          Elegir
                                        </Button>
                                      ),
                                    },
                                  ]}
                                />
                              )}
                            </>
                          ) : (
                            <div className="caja-chica-usuario-elegido">
                              <Text strong>{usuarioLabel}</Text>
                              <Button
                                size="small"
                                onClick={() => {
                                  setPersonaId(null)
                                  setUsuarioLabel('')
                                }}
                              >
                                Cambiar
                              </Button>
                            </div>
                          )}
                        </Col>
                        <Col xs={24} md={6}>
                          <Form.Item
                            name="tipoOperacionId"
                            label="Operación"
                            rules={[{ required: true }]}
                          >
                            <Select
                              loading={tipoOps.isLoading}
                              options={(tipoOps.data ?? []).map((o) => ({
                                value: o.tipoOperacionId,
                                label: o.denominacion,
                              }))}
                              placeholder="Tipo operación"
                            />
                          </Form.Item>
                        </Col>
                        <Col xs={24} md={6}>
                          <Form.Item
                            name="importe"
                            label="Importe"
                            rules={[{ required: true }]}
                          >
                            <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} />
                          </Form.Item>
                        </Col>
                        <Col xs={24} md={12}>
                          <Form.Item
                            name="descripcion"
                            label="Descripción"
                            rules={[{ required: true }]}
                          >
                            <Input />
                          </Form.Item>
                        </Col>
                        <Col xs={24}>
                          <Button
                            type="primary"
                            danger
                            htmlType="submit"
                            loading={gasto.isPending}
                          >
                            Generar operación
                          </Button>
                        </Col>
                      </Row>
                    </Form>
                  </CredixPanel>
                ),
              },
              {
                key: 'rendicion',
                label: 'Rendición',
                children: (
                  <>
                    <CajaListToolbar
                      className="credix-list-toolbar"
                      value={filtroRendicion}
                      onChange={setFiltroRendicion}
                      placeholder="Cliente, descripción"
                      hint="Rendiciones pendientes de comprobantes."
                      hintShort="Pendientes de comprobantes."
                      filteredCount={pendientesFiltrados.length}
                      totalCount={(pendientes.data ?? []).length}
                      loading={tabLoading}
                      onRefresh={() => void pendientes.refetch()}
                    />
                    <CredixDataTable<RendicionPendienteRow>
                      mode="operacion"
                      className="caja-chica-table"
                      rowKey="movimientoCajaChicaId"
                      columns={colsPend}
                      dataSource={pendientesFiltrados}
                      loading={pendientes.isLoading}
                      scroll={{ x: 'max-content' }}
                      pagination={{ pageSize: 10, showSizeChanger: true }}
                      style={{ marginBottom: 16 }}
                    />
                    {movimientoRendicionId != null && (
                      <CredixPanel title={`Rendición: ${rendicionLabel}`}>
                        <div className="caja-chica-rendicion-banner">
                          Agregue comprobantes y cierre la rendición cuando el total coincida.
                        </div>
                        <Form
                          layout="vertical"
                          onFinish={(v) =>
                            agregarRendicion.mutate({
                              movimientoCajaChicaId: movimientoRendicionId,
                              tipoDocumentoId: v.tipoDocumentoId,
                              fecha: v.fecha,
                              serie: v.serie ?? '',
                              numero: v.numero ?? '',
                              ruc: v.ruc ?? '',
                              razonSocial: v.razonSocial ?? '',
                              detalleGasto: v.detalleGasto,
                              importe: v.importe,
                            })
                          }
                        >
                          <Row gutter={16}>
                            <Col xs={24} md={6}>
                              <Form.Item
                                name="tipoDocumentoId"
                                label="Documento"
                                rules={[{ required: true }]}
                              >
                                <Select
                                  loading={tiposDoc.isLoading}
                                  options={(tiposDoc.data ?? []).map((d) => ({
                                    value: d.tipoDocumentoId,
                                    label: d.denominacion ?? '',
                                  }))}
                                />
                              </Form.Item>
                            </Col>
                            <Col xs={24} md={4}>
                              <Form.Item name="fecha" label="Fecha" rules={[{ required: true }]}>
                                <Input type="date" />
                              </Form.Item>
                            </Col>
                            <Col xs={12} md={3}>
                              <Form.Item name="serie" label="Serie">
                                <Input />
                              </Form.Item>
                            </Col>
                            <Col xs={12} md={3}>
                              <Form.Item name="numero" label="Número">
                                <Input />
                              </Form.Item>
                            </Col>
                            <Col xs={12} md={4}>
                              <Form.Item name="ruc" label="RUC">
                                <Input />
                              </Form.Item>
                            </Col>
                            <Col xs={24} md={8}>
                              <Form.Item name="razonSocial" label="Razón social">
                                <Input />
                              </Form.Item>
                            </Col>
                            <Col xs={24} md={8}>
                              <Form.Item
                                name="detalleGasto"
                                label="Detalle gasto"
                                rules={[{ required: true }]}
                              >
                                <Input />
                              </Form.Item>
                            </Col>
                            <Col xs={12} md={4}>
                              <Form.Item
                                name="importe"
                                label="Importe"
                                rules={[{ required: true }]}
                              >
                                <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} />
                              </Form.Item>
                            </Col>
                          </Row>
                          <Space wrap>
                            <Button
                              type="primary"
                              htmlType="submit"
                              loading={agregarRendicion.isPending}
                            >
                              Agregar comprobante
                            </Button>
                            <Button
                              danger
                              onClick={() =>
                                Modal.confirm({
                                  title: 'Cerrar rendición',
                                  icon: <ExclamationCircleOutlined />,
                                  content: '¿Cerrar esta rendición de caja chica?',
                                  onOk: () => cerrarRend.mutateAsync(movimientoRendicionId),
                                })
                              }
                              loading={cerrarRend.isPending}
                            >
                              Cerrar rendición
                            </Button>
                            <Button
                              type="link"
                              onClick={() => {
                                setMovimientoRendicionId(null)
                                setRendicionLabel('')
                              }}
                            >
                              Cancelar
                            </Button>
                          </Space>
                        </Form>
                        <CredixDataTable<RendicionComprobanteRow>
                          className="caja-chica-table"
                          style={{ marginTop: 16 }}
                          rowKey="id"
                          size="small"
                          mode="operacion"
                          columns={colsComp}
                          dataSource={comprobantes.data ?? []}
                          loading={comprobantes.isLoading}
                          pagination={false}
                          scroll={{ x: 'max-content' }}
                        />
                      </CredixPanel>
                    )}
                  </>
                ),
              },
              {
                key: 'arqueo',
                label: 'Arqueo',
                children: (
                  <>
                    <CajaListToolbar
                      className="credix-list-toolbar"
                      value={filtroArqueo}
                      onChange={setFiltroArqueo}
                      placeholder="Persona, operación, descripción"
                      hint="Filtra entradas y salidas a la vez."
                      hintShort="Entradas y salidas."
                      filteredCount={entradasFiltradas.length + salidasFiltradas.length}
                      totalCount={
                        (entradas.data?.length ?? 0) + (salidas.data?.length ?? 0)
                      }
                      loading={tabLoading}
                      onRefresh={() => {
                        void entradas.refetch()
                        void salidas.refetch()
                        void arqueo.refetch()
                      }}
                    />

                    <div className="caja-chica-arqueo-total">
                      TOTAL CAJA: S/ {formatMoney(sesion.data.saldoFinal)}
                    </div>

                    <CredixDataTable<MovimientoCajaChicaRow>
                      mode="operacion"
                      className="caja-chica-table"
                      title={() => 'Entradas'}
                      rowKey="movimientoCajaChicaId"
                      columns={colsMov}
                      dataSource={entradasFiltradas}
                      loading={entradas.isLoading}
                      scroll={{ x: 'max-content' }}
                      pagination={{ pageSize: 8, showSizeChanger: true }}
                      style={{ marginBottom: 16 }}
                    />
                    <CredixDataTable<MovimientoCajaChicaRow>
                      mode="operacion"
                      className="caja-chica-table"
                      title={() => 'Salidas'}
                      rowKey="movimientoCajaChicaId"
                      columns={colsMov}
                      dataSource={salidasFiltradas}
                      loading={salidas.isLoading}
                      scroll={{ x: 'max-content' }}
                      pagination={{ pageSize: 8, showSizeChanger: true }}
                      style={{ marginBottom: 16 }}
                    />
                    <CredixDataTable
                      mode="operacion"
                      className="caja-chica-table"
                      title={() => 'Resumen arqueo'}
                      rowKey="movimientoCajaId"
                      size="small"
                      loading={arqueo.isLoading}
                      dataSource={arqueo.data ?? []}
                      scroll={{ x: 'max-content' }}
                      pagination={{ pageSize: 10 }}
                      columns={[
                        { title: 'Operación', dataIndex: 'operacion' },
                        { title: 'Fecha', dataIndex: 'fechaReg', width: 160 },
                        { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
                        {
                          title: 'Importe',
                          dataIndex: 'importePago',
                          align: 'right',
                          render: (v: number) => formatMoney(v),
                        },
                        {
                          title: 'Tipo',
                          dataIndex: 'indEntrada',
                          render: (v: boolean) => (v ? 'ENTRADA' : 'SALIDA'),
                        },
                      ]}
                    />

                    <div className="caja-chica-arqueo-actions">
                      {validacion.data ? (
                        <Alert
                          type={validacion.data.puedeCerrar ? 'success' : 'warning'}
                          showIcon
                          style={{ flex: '1 1 100%', marginBottom: 0 }}
                          message={
                            validacion.data.puedeCerrar
                              ? 'Validación cierre masivo (saldos) OK'
                              : 'Revise cierre masivo en Saldos si aplica'
                          }
                          description={validacion.data.mensaje}
                        />
                      ) : null}
                      <Button
                        type="primary"
                        danger
                        loading={cerrarSesion.isPending}
                        onClick={() =>
                          Modal.confirm({
                            title: '¿Cerrar caja chica del día?',
                            icon: <ExclamationCircleOutlined />,
                            content: 'Paridad botón «Cerrar Caja» del MVC en arqueo.',
                            okText: 'Cerrar',
                            onOk: () => cerrarSesion.mutateAsync(),
                          })
                        }
                      >
                        Cerrar caja chica
                      </Button>
                      <Button
                        type="primary"
                        onClick={async () => {
                          try {
                            const m = await fetchMontoCajaChica()
                            setTransferImporte(m)
                            setTransferOpen(true)
                          } catch (e) {
                            message.error(errMsg(e))
                          }
                        }}
                      >
                        Transferir saldos a bóveda
                      </Button>
                    </div>
                  </>
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

      <Modal
        title="Transferir saldo a bóveda"
        open={transferOpen}
        onCancel={() => setTransferOpen(false)}
        onOk={() => {
          if (!transferImporte || transferImporte <= 0 || !transferDesc.trim()) {
            message.warning('Importe y descripción obligatorios')
            return
          }
          transferir.mutate({
            oficinaId,
            importe: transferImporte,
            descripcion: transferDesc.trim(),
          })
        }}
        confirmLoading={transferir.isPending}
      >
        <Paragraph>
          Monto total de caja: <strong>S/ {formatMoney(transferImporte ?? 0)}</strong>
        </Paragraph>
        <InputNumber
          min={0.01}
          step={0.01}
          style={{ width: '100%', marginBottom: 12 }}
          value={transferImporte}
          onChange={(v) => setTransferImporte(v)}
        />
        <Input.TextArea
          rows={2}
          placeholder="Descripción de transferencia"
          value={transferDesc}
          onChange={(e) => setTransferDesc(e.target.value)}
        />
      </Modal>
    </CredixPage>
  )
}
