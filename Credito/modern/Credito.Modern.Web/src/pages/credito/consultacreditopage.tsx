import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Input,
  InputNumber,
  Modal,
  Space,
  Tabs,
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
  prorrogarCredito,
  reprogramarCredito,
  validarAnularCredito,
} from '../../api/creditoPlanes'
import { fetchCreditoMoraResumen } from '../../api/cajaDiario'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { EstadoPlanPagoCuota, RptMovimientoCreditoRow } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'
import { actualizarDescuentoPlanPago } from '../../api/creditoGestion'
import { CreditoConsultaGestionPanel } from './CreditoConsultaGestionPanel'
import { CreditoConsultaClienteBar } from '../../components/credito/CreditoConsultaClienteBar'
import { CreditoBuscarPorCredito } from '../../components/credito/CreditoBuscarPorCredito'
import { CreditoConsultaAccionesCredito } from '../../components/credito/CreditoConsultaAccionesCredito'
import { CreditoConsultaImpresosBar } from '../../components/credito/CreditoConsultaImpresosBar'
import { CreditoConsultaResumenCredito } from '../../components/credito/CreditoConsultaResumenCredito'
import {
  puedeAnularCreditoUi,
  puedeProrrogarCreditoUi,
  puedeReprogramarCreditoUi,
  tieneCreditoModoLectura,
} from '../../utils/creditoOperacionPermisos'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
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

export function ConsultaCreditoPage() {
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const roles = session?.roles ?? []
  const soloLectura = tieneCreditoModoLectura(roles)
  const puedeAnularBtn = puedeAnularCreditoUi(roles)
  const puedeProrrogarBtn = puedeProrrogarCreditoUi(roles)
  const puedeReprogramarBtn = puedeReprogramarCreditoUi(roles)
  const [searchParams, setSearchParams] = useSearchParams()
  const [modalAnular, setModalAnular] = useState(false)
  const [modalProrrogar, setModalProrrogar] = useState(false)
  const [modalReprogramar, setModalReprogramar] = useState(false)
  const [moraModalOpen, setMoraModalOpen] = useState(false)
  const [observacionAnular, setObservacionAnular] = useState('')
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

  const [creditoId, setCreditoId] = useState<number | null>(creditoIdFromUrl)
  const [activeId, setActiveId] = useState<number | null>(creditoIdFromUrl)
  const [tabActiva, setTabActiva] = useState('plan')

  useEffect(() => {
    if (creditoIdFromUrl !== null && creditoIdFromUrl !== creditoId) {
      setCreditoId(creditoIdFromUrl)
      setActiveId(creditoIdFromUrl)
    }
  }, [creditoIdFromUrl, creditoId])

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

  const aplicarCredito = useCallback(
    (id: number, personaId?: number) => {
      setCreditoId(id)
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

  const buscar = () => {
    if (!creditoId || creditoId < 1) {
      message.warning('Ingrese un número de crédito válido')
      return
    }
    aplicarCredito(creditoId, personaIdFromUrl ?? undefined)
  }

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
      }),
    onSuccess: () => {
      message.success('Crédito anulado')
      setModalAnular(false)
      setObservacionAnular('')
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

  const planCols: ColumnsType<EstadoPlanPagoCuota> = [
    { title: 'Nro', dataIndex: 'numero' },
    {
      title: 'Vence',
      dataIndex: 'fechaVencimiento',
      render: (v: string) => v?.slice(0, 10),
    },
    { title: 'Estado', dataIndex: 'estado' },
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
      title="Consulta de crédito"
      subtitle="Busque por cliente (como Creditos MVC) o por número de crédito. Plan, gestión, movimientos e impresos."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Consulta' },
      ]}
      stats={creditoStats}
    >
      <CreditoConsultaClienteBar
        oficinaId={oficinaId}
        creditoActivoId={activeId}
        personaIdInicial={personaIdFromUrl}
        onSeleccionarCredito={(id, pid) => aplicarCredito(id, pid)}
      />

      <CreditoBuscarPorCredito
        creditoId={creditoId}
        onCreditoIdChange={setCreditoId}
        onSearch={buscar}
        loading={
          planQuery.isFetching && creditoId != null && creditoId === activeId
        }
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
            personaId={personaIdFromUrl}
          />
          <CreditoConsultaAccionesCredito
            creditoId={activeId}
            oficinaId={oficinaId}
            puedeAnular={puedeAnularBtn}
            puedeProrrogar={puedeProrrogarBtn}
            puedeReprogramar={puedeReprogramarBtn}
            onAnular={() => void abrirAnular()}
            onProrrogar={() => setModalProrrogar(true)}
            onReprogramar={() => setModalReprogramar(true)}
            onMora={() => setMoraModalOpen(true)}
          />
          <CreditoConsultaResumenCredito creditoId={activeId} />
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
                    columns={planCols}
                    dataSource={planQuery.data ?? []}
                    loading={planQuery.isLoading}
                    pagination={{ pageSize: 12 }}
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
        onCancel={() => setModalAnular(false)}
        onOk={() => {
          if (!observacionAnular.trim()) {
            message.warning('La observación es obligatoria')
            return
          }
          if (puedeAnular === false) {
            message.error('Este crédito no cumple las condiciones para anular')
            return
          }
          anular.mutate()
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
          estado anulable.
        </Paragraph>
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
