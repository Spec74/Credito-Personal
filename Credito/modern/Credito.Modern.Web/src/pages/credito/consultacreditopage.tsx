import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PrinterOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Input,
  InputNumber,
  Modal,
  Space,
  Table,
  Tabs,
  Tag,
  Typography,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  anularCredito,
  fetchEstadoPlanPago,
  fetchMoraPendiente,
  fetchMetricasVencimientoCredito,
  fetchMovimientosCredito,
  fetchRptEstadoCredito,
  prorrogarCredito,
  reprogramarCredito,
  validarAnularCredito,
} from '../../api/creditoPlanes'
import { downloadMovimientoCajaTicketPdf, fetchCreditoMoraResumen } from '../../api/cajaDiario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { EstadoPlanPagoCuota, RptMovimientoCreditoRow } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'
import { actualizarDescuentoPlanPago } from '../../api/creditoGestion'
import { CreditoConsultaGestionPanel } from './CreditoConsultaGestionPanel'
import { CreditoConsultaClienteBar } from '../../components/credito/CreditoConsultaClienteBar'
import { CreditoConsultaAccionesCredito } from '../../components/credito/CreditoConsultaAccionesCredito'
import { CreditoConsultaImpresosBar } from '../../components/credito/CreditoConsultaImpresosBar'
import { CreditoConsultaResumenCredito } from '../../components/credito/CreditoConsultaResumenCredito'
import {
  tieneCreditoModoLectura,
} from '../../utils/creditoOperacionPermisos'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import { getCreditoEstadoMeta, resolverCreditoAccionesUi } from '../../utils/creditoEstados'
import { CreditoMoraModal } from '../../components/caja/CreditoMoraModal'
import {
  CredixDataTable,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'

function DescuentoCuotaEditor({
  planPagoId,
  descuentoInicial,
  oficinaId,
  creditoId,
  onSaved,
  soloLectura,
}: {
  planPagoId: number
  descuentoInicial: number | null
  oficinaId: number
  creditoId: number
  onSaved: () => void
  soloLectura?: boolean
}) {
  const [valor, setValor] = useState(descuentoInicial ?? 0)
  const guardar = useMutation({
    mutationFn: () =>
      actualizarDescuentoPlanPago({
        oficinaId,
        creditoId,
        planPagoId,
        descuento: valor,
      }),
    onSuccess: () => {
      message.success('Descuento actualizado')
      onSaved()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  if (soloLectura) {
    return <span>{formatMoney(descuentoInicial)}</span>
  }

  return (
    <Space size={4}>
      <InputNumber
        size="small"
        min={0}
        precision={2}
        value={valor}
        onChange={(v) => setValor(v ?? 0)}
        style={{ width: 76 }}
      />
      <Button
        type="link"
        size="small"
        loading={guardar.isPending}
        onClick={() => guardar.mutate()}
      >
        OK
      </Button>
    </Space>
  )
}
function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

const { Paragraph, Text } = Typography

function estadoPlanRowClass(row: EstadoPlanPagoCuota): string {
  const estado = row.estado?.trim().toUpperCase()
  if (estado === 'PAG') return 'credito-plan-row credito-plan-row--pagado'
  if ((row.diasAtrazo ?? 0) > 0 && estado !== 'PAG') {
    return 'credito-plan-row credito-plan-row--vencido'
  }
  if (estado === 'CRE') return 'credito-plan-row credito-plan-row--creado'
  return 'credito-plan-row credito-plan-row--pendiente'
}

export function ConsultaCreditoPage() {
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const roles = useMemo(() => session?.roles ?? [], [session?.roles])
  const soloLectura = tieneCreditoModoLectura(roles)
  const [searchParams, setSearchParams] = useSearchParams()
  const [modalAnular, setModalAnular] = useState(false)
  const [modalProrrogar, setModalProrrogar] = useState(false)
  const [modalReprogramar, setModalReprogramar] = useState(false)
  const [moraModalOpen, setMoraModalOpen] = useState(false)
  const [observacionAnular, setObservacionAnular] = useState('')
  const [claveAnular, setClaveAnular] = useState('')
  const [diasProrroga, setDiasProrroga] = useState(1)
  const [puedeAnular, setPuedeAnular] = useState<boolean | null>(null)
  const creditoIdParam = searchParams.get('creditoId')
  const personaIdParam =
    searchParams.get('personaId') ??
    searchParams.get('pPersonaId') ??
    searchParams.get('personalId')
  const personaIdFromUrl = useMemo(() => {
    if (!personaIdParam) return null
    const id = Number(personaIdParam)
    return !Number.isNaN(id) && id > 0 ? id : null
  }, [personaIdParam])

  const creditoIdFromUrl = useMemo(() => {
    if (!creditoIdParam) return null
    const id = Number(creditoIdParam)
    return !Number.isNaN(id) && id > 0 ? id : null
  }, [creditoIdParam])

  const [activeId, setActiveId] = useState<number | null>(creditoIdFromUrl)
  const [tabActiva, setTabActiva] = useState('plan')

  useEffect(() => {
    if (creditoIdFromUrl !== null && creditoIdFromUrl !== activeId) {
      setActiveId(creditoIdFromUrl)
    }
  }, [creditoIdFromUrl, activeId])

  const moraQuery = useQuery({
    queryKey: ['mora-pendiente', activeId],
    queryFn: () => fetchMoraPendiente(activeId!),
    enabled: activeId != null,
    staleTime: creditoStaleTime.operacion,
  })

  const planQuery = useQuery({
    queryKey: ['estado-plan-pago', activeId],
    queryFn: () => fetchEstadoPlanPago(activeId!),
    enabled: activeId != null,
    staleTime: creditoStaleTime.operacion,
  })

  const movQuery = useQuery({
    queryKey: ['movimiento-credito', activeId],
    queryFn: () => fetchMovimientosCredito(activeId!),
    enabled: activeId != null && tabActiva === 'mov',
    staleTime: creditoStaleTime.operacion,
  })

  const vencimientoQuery = useQuery({
    queryKey: ['metricas-vencimiento-credito', activeId],
    queryFn: () => fetchMetricasVencimientoCredito(activeId!),
    enabled: activeId != null && planQuery.isSuccess,
    staleTime: creditoStaleTime.operacion,
  })

  const estadoCreditoQuery = useQuery({
    queryKey: ['rpt-estado-credito-resumen', activeId],
    queryFn: () => fetchRptEstadoCredito(activeId!),
    enabled: activeId != null,
    staleTime: creditoStaleTime.operacion,
  })

  const accionesCredito = useMemo(
    () => resolverCreditoAccionesUi(roles, estadoCreditoQuery.data?.cabecera?.estado),
    [roles, estadoCreditoQuery.data?.cabecera?.estado],
  )
  const personaIdActiva =
    personaIdFromUrl ?? estadoCreditoQuery.data?.cabecera?.personaId ?? null

  const aplicarCredito = useCallback(
    (id: number, personaId?: number) => {
      setActiveId(id)
      const next = new URLSearchParams()
      next.set('creditoId', String(id))
      if (personaId != null && personaId > 0) {
        next.set('personaId', String(personaId))
      }
      setSearchParams(next)
    },
    [setSearchParams],
  )

  const invalidarConsulta = () => {
    void queryClient.invalidateQueries({ queryKey: ['estado-plan-pago', activeId] })
    void queryClient.invalidateQueries({ queryKey: ['movimiento-credito', activeId] })
    void queryClient.invalidateQueries({ queryKey: ['mora-pendiente', activeId] })
    void queryClient.invalidateQueries({ queryKey: ['credito-mora-resumen', activeId] })
    void queryClient.invalidateQueries({ queryKey: ['credito-mora-historial', activeId] })
    void queryClient.invalidateQueries({
      queryKey: ['metricas-vencimiento-credito', activeId],
    })
  }

  const abrirAnular = async () => {
    if (!activeId || oficinaId < 1) return
    try {
      const v = await validarAnularCredito(oficinaId, activeId)
      setPuedeAnular(v.puedeAnular)
      setObservacionAnular('')
      setClaveAnular('')
      setModalAnular(true)
    } catch (e) {
      message.error(errMsg(e))
    }
  }

  const anular = useMutation({
    mutationFn: () =>
      anularCredito({
        oficinaId,
        creditoId: activeId!,
        observacion: observacionAnular.trim(),
        claveAutorizacion: claveAnular,
      }),
    onSuccess: () => {
      message.success('Crédito anulado')
      setModalAnular(false)
      setObservacionAnular('')
      setClaveAnular('')
      invalidarConsulta()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const prorrogar = useMutation({
    mutationFn: () =>
      prorrogarCredito({
        oficinaId,
        creditoId: activeId!,
        dias: diasProrroga,
      }),
    onSuccess: () => {
      message.success('Crédito prorrogado')
      setModalProrrogar(false)
      invalidarConsulta()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const reprogramar = useMutation({
    mutationFn: () =>
      reprogramarCredito({
        oficinaId,
        creditoId: activeId!,
      }),
    onSuccess: () => {
      message.success('Crédito reprogramado')
      setModalReprogramar(false)
      invalidarConsulta()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const ticketMovimiento = useMutation({
    mutationFn: (movimientoCajaId: number) =>
      downloadMovimientoCajaTicketPdf(oficinaId, movimientoCajaId),
    onSuccess: () => message.success('Ticket de pago generado'),
    onError: (e) => message.error(errMsg(e)),
  })

  const planCols: ColumnsType<EstadoPlanPagoCuota> = [
    { title: 'Nro', dataIndex: 'numero' },
    {
      title: 'Vence',
      dataIndex: 'fechaVencimiento',
      render: (v: string) => v?.slice(0, 10),
    },
    {
      title: 'Estado',
      dataIndex: 'estado',
      render: (estado: string | null, row) => {
        const meta = getCreditoEstadoMeta(estado)
        const tag = meta ? <Tag color={meta.color}>{meta.codigo}</Tag> : estado || '—'
        if (estado?.trim().toUpperCase() !== 'PAG' || !row.movimientoCajaId) {
          return tag
        }

        return (
          <Button
            type="link"
            size="small"
            className="credito-plan-ticket-link"
            icon={<PrinterOutlined />}
            loading={ticketMovimiento.isPending}
            onClick={() => ticketMovimiento.mutate(row.movimientoCajaId!)}
          >
            {tag}
          </Button>
        )
      },
    },
    {
      title: 'Capital',
      dataIndex: 'capital',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Amort.',
      dataIndex: 'amortizacion',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés',
      dataIndex: 'interes',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'G.A.',
      dataIndex: 'gastosAdm',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Cuota',
      dataIndex: 'cuota',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Mora',
      dataIndex: 'importeMora',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Desc.',
      dataIndex: 'descuento',
      align: 'right',
      minWidth: 120,
      render: (v: number | null, row) =>
        activeId != null && oficinaId > 0 ? (
          <DescuentoCuotaEditor
            planPagoId={row.planPagoId}
            descuentoInicial={v}
            oficinaId={oficinaId}
            creditoId={activeId}
            soloLectura={soloLectura}
            onSaved={() => void queryClient.invalidateQueries({ queryKey: ['estado-plan-pago', activeId] })}
          />
        ) : (
          (v != null ? formatMoney(v) : '—')
        ),
    },
    {
      title: 'Cargo',
      dataIndex: 'cargo',
      align: 'right',
      render: (v: number | null) => (v != null ? formatMoney(v) : '—'),
    },
    {
      title: 'P. libre',
      dataIndex: 'pagoLibre',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'F. pago',
      dataIndex: 'fechaPagoCuota',
      render: (v: string | null) => v?.slice(0, 10) ?? '—',
    },
    {
      title: 'Pagado',
      dataIndex: 'pagoCuota',
      align: 'right',
      render: formatMoney,
    },
    { title: 'Atrazo', dataIndex: 'diasAtrazo' },
    { title: 'Mov.', dataIndex: 'movimientoCajaId' },
  ]

  const movCols: ColumnsType<RptMovimientoCreditoRow> = [
    { title: 'Mov.', dataIndex: 'movimientoCajaId' },
    {
      title: 'Fecha',
      dataIndex: 'fecha',
      render: (v: string | null) => v?.slice(0, 10) ?? '—',
    },
    { title: 'Operación', dataIndex: 'operacion', ellipsis: true, minWidth: 120 },
    { title: 'Glosa', dataIndex: 'glosa', ellipsis: true, minWidth: 160 },
    {
      title: 'Importe',
      dataIndex: 'importePago',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Saldo',
      dataIndex: 'saldo',
      align: 'right',
      render: formatMoney,
    },
  ]
  const moraResumenQuery = useQuery({
    queryKey: ['credito-mora-resumen', activeId],
    queryFn: () => fetchCreditoMoraResumen(activeId!),
    enabled: activeId != null && activeId > 0,
    staleTime: creditoStaleTime.operacion,
  })

  const pendientes =
    planQuery.data?.filter((c) => c.estado !== 'PAG').length ?? 0

  const planResumen = useMemo(() => {
    const rows = planQuery.data ?? []
    return rows.reduce(
      (acc, row) => ({
        capital: acc.capital + (row.capital ?? 0),
        amortizacion: acc.amortizacion + (row.amortizacion ?? 0),
        interes: acc.interes + (row.interes ?? 0),
        gastosAdm: acc.gastosAdm + (row.gastosAdm ?? 0),
        cuota: acc.cuota + (row.cuota ?? 0),
        mora: acc.mora + (row.importeMora ?? 0),
        descuento: acc.descuento + (row.descuento ?? 0),
        cargo: acc.cargo + (row.cargo ?? 0),
        pagoLibre: acc.pagoLibre + (row.pagoLibre ?? 0),
        pagado: acc.pagado + (row.pagoCuota ?? 0),
        pagadas: acc.pagadas + (row.estado?.trim().toUpperCase() === 'PAG' ? 1 : 0),
      }),
      {
        capital: 0,
        amortizacion: 0,
        interes: 0,
        gastosAdm: 0,
        cuota: 0,
        mora: 0,
        descuento: 0,
        cargo: 0,
        pagoLibre: 0,
        pagado: 0,
        pagadas: 0,
      },
    )
  }, [planQuery.data])

  const moraVigente = moraQuery.data?.moraPendiente ?? 0
  const moraPostergada = moraResumenQuery.data?.saldoPostergado ?? 0
  const moraTotal =
    moraResumenQuery.data?.indMoraProducto === true
      ? moraVigente + moraPostergada
      : moraVigente

  const creditoStats: CredixStatItem[] =
    activeId != null
      ? [
          { value: activeId, label: 'Crédito N°' },
          {
            value:
              moraQuery.isLoading || moraResumenQuery.isLoading
                ? '…'
                : formatMoney(moraTotal),
            label: 'Mora total (S/.)',
            tone: 'red',
          },
          {
            value:
              moraResumenQuery.isLoading
                ? '…'
                : formatMoney(moraPostergada),
            label: 'Mora postergada (S/.)',
            tone: moraPostergada > 0 ? 'red' : undefined,
          },
          {
            value: planQuery.isLoading ? '…' : pendientes,
            label: 'Cuotas no pagadas',
          },
          {
            value: vencimientoQuery.isLoading
              ? '…'
              : formatMoney(vencimientoQuery.data?.creditoVencido),
            label: 'Vencido total (S/.)',
            tone: 'red',
          },
          {
            value: vencimientoQuery.isLoading
              ? '…'
              : formatMoney(vencimientoQuery.data?.vencidoMenor60),
            label: 'Venc. menor 60 d. (S/.)',
          },
          {
            value: vencimientoQuery.isLoading
              ? '…'
              : formatMoney(vencimientoQuery.data?.vencidoMayor60),
            label: 'Venc. mayor 60 d. (S/.)',
            tone: 'red',
          },
        ]
      : []

  return (
    <CredixPage
      className="credito-consulta-page"
      title="Créditos"
      subtitle="Búsqueda, solicitud, plan de pagos, gestión, movimientos e impresos con paridad del Creditos MVC."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Créditos' },
      ]}
      stats={creditoStats}
    >
      <CreditoConsultaClienteBar
        oficinaId={oficinaId}
        creditoActivoId={activeId}
        personaIdInicial={personaIdFromUrl}
        onSeleccionarCredito={(id, pid) => aplicarCredito(id, pid)}
      />

      {moraResumenQuery.data?.indMoraProducto &&
      (moraResumenQuery.data.saldoPostergado ?? 0) > 0 ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message={`Mora postergada acumulada: ${formatMoney(moraResumenQuery.data.saldoPostergado)}`}
          description="Se liquidará al cobrar la última cuota pendiente (tabla CreditoMora)."
          action={
            <Button size="small" onClick={() => setMoraModalOpen(true)}>
              Ver historial
            </Button>
          }
        />
      ) : null}

      {soloLectura && activeId != null ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Consulta en modo solo lectura"
          description="Puede ver plan, movimientos e impresos. Las operaciones de ciclo y gestión están deshabilitadas."
        />
      ) : null}

      {activeId != null && (
        <CredixPanel title={`Crédito ${activeId}`} className="credito-consulta-panel-credito">
          <CreditoConsultaImpresosBar
            creditoId={activeId}
            personaId={personaIdActiva}
          />
          <CreditoConsultaAccionesCredito
            creditoId={activeId}
            oficinaId={oficinaId}
            puedeCobrar={accionesCredito.cobrar}
            puedeMora={accionesCredito.mora}
            puedeAnular={accionesCredito.anular}
            puedeProrrogar={accionesCredito.prorrogar}
            puedeReprogramar={accionesCredito.reprogramar}
            onAnular={() => void abrirAnular()}
            onProrrogar={() => setModalProrrogar(true)}
            onReprogramar={() => setModalReprogramar(true)}
            onMora={() => setMoraModalOpen(true)}
          />
          <CreditoConsultaResumenCredito creditoId={activeId} />
          {estadoCreditoQuery.data?.cabecera?.estado === 'PEN' ? (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message="Crédito pendiente de aprobación"
              description="La aprobación se gestiona desde la bandeja Crédito > Aprobar para conservar roles, auditoría y flujo de revisión."
              action={
                <Link to="/credito/aprobar">
                  <Button size="small" type="primary">
                    Ir a aprobación
                  </Button>
                </Link>
              }
            />
          ) : null}
          <Tabs
            className="credix-tabs"
            size="small"
            destroyInactiveTabPane
            activeKey={tabActiva}
            onChange={setTabActiva}
            items={[
              {
                key: 'plan',
                label: 'Plan de pago',
                children: (
                  <CredixDataTable<EstadoPlanPagoCuota>
                    mode="operacion"
                    rowKey="planPagoId"
                    className="credito-plan-table"
                    columns={planCols}
                    dataSource={planQuery.data ?? []}
                    loading={planQuery.isLoading}
                    pagination={{ pageSize: 12 }}
                    rowClassName={estadoPlanRowClass}
                    summary={() => (
                      <Table.Summary fixed>
                        <Table.Summary.Row className="credito-plan-summary-row">
                          <Table.Summary.Cell index={0} colSpan={3}>
                            <Text strong>
                              Total ({planResumen.pagadas}/{planQuery.data?.length ?? 0} pagadas)
                            </Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={3} align="right">
                            <Text strong>{formatMoney(planResumen.capital)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={4} align="right">
                            <Text strong>{formatMoney(planResumen.amortizacion)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={5} align="right">
                            <Text strong>{formatMoney(planResumen.interes)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={6} align="right">
                            <Text strong>{formatMoney(planResumen.gastosAdm)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={7} align="right">
                            <Text strong>{formatMoney(planResumen.cuota)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={8} align="right">
                            <Text strong>{formatMoney(planResumen.mora)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={9} align="right">
                            <Text strong>{formatMoney(planResumen.descuento)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={10} align="right">
                            <Text strong>{formatMoney(planResumen.cargo)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={11} align="right">
                            <Text strong>{formatMoney(planResumen.pagoLibre)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={12}>—</Table.Summary.Cell>
                          <Table.Summary.Cell index={13} align="right">
                            <Text strong>{formatMoney(planResumen.pagado)}</Text>
                          </Table.Summary.Cell>
                          <Table.Summary.Cell index={14}>—</Table.Summary.Cell>
                          <Table.Summary.Cell index={15}>—</Table.Summary.Cell>
                        </Table.Summary.Row>
                      </Table.Summary>
                    )}
                  />
                ),
              },
              {
                key: 'gestion',
                label: 'Gestión',
                children: (
                  <CreditoConsultaGestionPanel
                    creditoId={activeId}
                    oficinaId={oficinaId}
                    roles={roles}
                    activo={tabActiva === 'gestion'}
                  />
                ),
              },
              {
                key: 'mov',
                label: 'Movimientos',
                children: (
                  <>
                    <Text type="secondary" style={{ display: 'block', marginBottom: 8 }}>
                      Exportar también desde la barra Impresos arriba.
                    </Text>
                    <CredixDataTable<RptMovimientoCreditoRow>
                      mode="operacion"
                      rowKey={(r, i) => String(r.movimientoCajaId ?? `m-${i}`)}
                      columns={movCols}
                      dataSource={movQuery.data ?? []}
                      loading={movQuery.isLoading}
                      pagination={{ pageSize: 12 }}
                    />
                  </>
                ),
              },
            ]}
          />
        </CredixPanel>
      )}

      <Modal
        title={`Anular crédito ${activeId ?? ''}`}
        open={modalAnular}
        onCancel={() => {
          setModalAnular(false)
          setClaveAnular('')
        }}
        onOk={async () => {
          if (!observacionAnular.trim()) {
            message.warning('La observación es obligatoria')
            return
          }
          if (!claveAnular.trim()) {
            message.warning('Ingrese la clave de autorización')
            return
          }
          if (puedeAnular === false) {
            message.error('Este crédito no cumple las condiciones para anular')
            return
          }
          try {
            await anular.mutateAsync()
          } catch (e) {
            message.error(errMsg(e))
          }
        }}
        confirmLoading={anular.isPending}
        okText="Anular"
        okButtonProps={{ danger: true }}
      >
        {puedeAnular === false && (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 12 }}
            message="Anulación no permitida según validación del sistema."
          />
        )}
        <Paragraph type="secondary">
          Misma regla que el MVC: todas las cuentas por cobrar deben estar en
          estado anulable y se requiere clave de autorización.
        </Paragraph>
        <Input.Password
          style={{ marginBottom: 12 }}
          placeholder="Clave de autorización"
          value={claveAnular}
          onChange={(e) => setClaveAnular(e.target.value)}
        />
        <Input.TextArea
          rows={3}
          placeholder="Observación (obligatoria)"
          value={observacionAnular}
          onChange={(e) => setObservacionAnular(e.target.value)}
        />
      </Modal>

      <Modal
        title={`Prorrogar crédito ${activeId ?? ''}`}
        open={modalProrrogar}
        onCancel={() => setModalProrrogar(false)}
        onOk={() => {
          if (diasProrroga < 1) {
            message.warning('Indique al menos 1 día')
            return
          }
          prorrogar.mutate()
        }}
        confirmLoading={prorrogar.isPending}
        okText="Prorrogar"
      >
        <Space direction="vertical">
          <Paragraph type="secondary">Días de prórroga (paridad MVC).</Paragraph>
          <InputNumber
            min={1}
            value={diasProrroga}
            onChange={(v) => setDiasProrroga(v ?? 1)}
          />
        </Space>
      </Modal>

      <Modal
        title={`Reprogramar crédito ${activeId ?? ''}`}
        open={modalReprogramar}
        onCancel={() => setModalReprogramar(false)}
        onOk={() => reprogramar.mutate()}
        confirmLoading={reprogramar.isPending}
        okText="Reprogramar"
      >
        <Paragraph>
          Reprograma el plan de pagos del crédito según las reglas del
          procedimiento almacenado (misma operación que en el sistema clásico).
        </Paragraph>
      </Modal>

      <CreditoMoraModal
        open={moraModalOpen}
        creditoId={activeId}
        cuotas={[]}
        onClose={() => setMoraModalOpen(false)}
      />
    </CredixPage>
  )
}
