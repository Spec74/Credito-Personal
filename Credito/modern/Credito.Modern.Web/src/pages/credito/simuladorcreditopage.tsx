import { useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Checkbox,
  Form,
  Input,
  InputNumber,
  Select,
  Space,
  Steps,
  Typography,
  message,
} from 'antd'
import { CalculatorOutlined, FileAddOutlined, UserAddOutlined } from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { buscarClientes } from '../../api/clientes'
import {
  calcularTem,
  crearCredito,
  crearSolicitudCredito,
  downloadRptSimuladorPlanPagosCsv,
  downloadRptSimuladorPlanPagosPdf,
  simularCredito,
  type RptSimuladorPlanPagosParams,
} from '../../api/creditoPlanes'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import {
  CredixDataTable,
  CredixPage,
  CredixPanel,
  type CredixStatItem,
} from '../../components/credix'
import { fetchProductos } from '../../api/productos'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import type { ClienteBuscarItem, SimuladorCreditoCuota } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

const { Paragraph, Text } = Typography

const FORMAS_PAGO = [
  { value: 'M', label: 'Mensual (M)' },
  { value: 'Q', label: 'Quincenal (Q)' },
  { value: 'S', label: 'Semanal (S)' },
  { value: 'D', label: 'Diario (D)' },
]

/** Paridad legacy `Creditos.cshtml` / `cboGA`: solo ADE (CUO comentado en MVC). */
const IND_GASTOS_ADM = 'ADE' as const

const IND_GASTOS_ADM_OPTIONS = [
  { value: IND_GASTOS_ADM, label: 'Trámite adm. pago adelantado (ADE)' },
]

interface SimForm {
  monto: number
  formaPago: string
  nroCuotas: number
  interesMensual: number
  fechaPrimerPago: string
  gastosAdm?: number
}

function defaultFecha(): string {
  const d = new Date()
  d.setMonth(d.getMonth() + 1)
  return d.toISOString().slice(0, 10)
}

export function SimuladorCreditoPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [cuotas, setCuotas] = useState<SimuladorCreditoCuota[]>([])
  const [tem, setTem] = useState<number | null>(null)
  const [personaId, setPersonaId] = useState<number | null>(null)
  const [clienteLabel, setClienteLabel] = useState('')
  const [terminoCliente, setTerminoCliente] = useState('')
  const [solicitudCreditoId, setSolicitudCreditoId] = useState<number | null>(null)
  const [productoId, setProductoId] = useState<number | null>(null)
  const [observacion, setObservacion] = useState('')
  const [indCentralRiesgo, setIndCentralRiesgo] = useState(true)
  const [form] = Form.useForm<SimForm>()

  const personaIdFromUrl = useMemo(() => {
    const q = searchParams.get('personaId')
    if (!q) return null
    const id = Number(q)
    return Number.isNaN(id) || id < 1 ? null : id
  }, [searchParams])

  const solicitudFromUrl = useMemo(() => {
    const q = searchParams.get('solicitudCreditoId')
    if (!q) return null
    const id = Number(q)
    return Number.isNaN(id) || id < 1 ? null : id
  }, [searchParams])

  if (personaIdFromUrl !== null && personaIdFromUrl !== personaId) {
    setPersonaId(personaIdFromUrl)
    setClienteLabel(`Persona #${personaIdFromUrl}`)
  }

  if (solicitudFromUrl !== null && solicitudFromUrl !== solicitudCreditoId) {
    setSolicitudCreditoId(solicitudFromUrl)
  }

  const productosQuery = useQuery({
    queryKey: ['productos'],
    queryFn: fetchProductos,
    staleTime: creditoStaleTime.master,
  })

  const busquedaCliente = useMutation({
    mutationFn: (t: string) => buscarClientes(t),
  })

  const crearSolicitud = useMutation({
    mutationFn: () =>
      crearSolicitudCredito({
        oficinaId,
        personaId: personaId!,
      }),
    onSuccess: (r) => {
      setSolicitudCreditoId(r.solicitudCreditoId)
      message.success(`Solicitud #${r.solicitudCreditoId} en estado CRE`)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const generarCredito = useMutation({
    mutationFn: async () => {
      const v = form.getFieldsValue()
      if (!solicitudCreditoId || !productoId) {
        throw new Error('Falta solicitud o producto')
      }
      if (cuotas.length < 1) {
        throw new Error('Simule el plan antes de generar el crédito')
      }
      return crearCredito({
        oficinaId,
        solicitudCreditoId,
        productoId,
        tipoCuota: 'F',
        montoInicial: 0,
        montoGastosAdm: v.gastosAdm ?? 0,
        indGastosAdm: IND_GASTOS_ADM,
        montoCredito: v.monto,
        modalidad: v.formaPago,
        numeroCuotas: v.nroCuotas,
        interesMensual: v.interesMensual,
        fechaPrimerPago: `${v.fechaPrimerPago}T00:00:00`,
        observacion: observacion.trim() || null,
        indCentralRiesgo,
      })
    },
    onSuccess: (r) => {
      if (r.mensaje?.trim()) {
        message.warning(r.mensaje)
      } else {
        message.success('Crédito generado para aprobación')
        navigate('/credito/aprobar')
      }
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const exportCsv = useMutation({
    mutationFn: (p: RptSimuladorPlanPagosParams) => downloadRptSimuladorPlanPagosCsv(p),
  })

  const exportPdf = useMutation({
    mutationFn: (p: RptSimuladorPlanPagosParams) => downloadRptSimuladorPlanPagosPdf(p),
  })



  const simular = useMutation({
    mutationFn: async (values: SimForm) => {
      // Paridad CreditoController.Simulador con cboGA=ADE: gastos no van al SP (solo en cabecera informe).
      const gastosSp = 0
      const rows = await simularCredito({
        monto: values.monto,
        formaPago: values.formaPago,
        nroCuotas: values.nroCuotas,
        interesMensual: values.interesMensual,
        fechaPrimerPago: `${values.fechaPrimerPago}T00:00:00`,
        gastosAdm: gastosSp,
      })
      try {
        const temRes = await calcularTem(values.interesMensual, values.formaPago)
        setTem(temRes.tem)
      } catch {
        setTem(null)
      }
      return rows
    },
    onSuccess: (data) => {
      setCuotas(data)
      message.success(`${data.length} cuota(s) simuladas`)
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'Error al simular'),
  })

  const pasoActual =
    solicitudCreditoId != null && cuotas.length > 0
      ? 3
      : cuotas.length > 0
        ? 2
        : personaId != null
          ? 1
          : 0

  const columns: ColumnsType<SimuladorCreditoCuota> = [
    { title: 'Nro', dataIndex: 'numero', width: 60 },
    {
      title: 'Capital',
      dataIndex: 'capital',
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Fecha pago',
      dataIndex: 'fechaPago',
      width: 110,
      render: (v: string | null) => v?.slice(0, 10) ?? '—',
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
  ]

  const totalCuota = cuotas.reduce((s, c) => s + (c.cuota ?? 0), 0)
  const montoSim = Form.useWatch('monto', form) as number | undefined
  const nroCuotasSim = Form.useWatch('nroCuotas', form) as number | undefined

  const simStats: CredixStatItem[] = useMemo(() => {
    const items: CredixStatItem[] = []
    if (personaId != null) {
      items.push({
        value: clienteLabel || `Persona #${personaId}`,
        label: 'Cliente',
      })
    }
    if (solicitudCreditoId != null) {
      items.push({ value: solicitudCreditoId, label: 'Solicitud' })
    }
    if (cuotas.length > 0) {
      items.push(
        { value: cuotas.length, label: 'Cuotas simuladas' },
        { value: formatMoney(totalCuota), label: 'Total plan', tone: 'green' },
      )
      if (tem != null) {
        items.push({ value: `${tem.toFixed(2)}%`, label: 'TEM' })
      }
      if (montoSim != null) {
        items.push({ value: formatMoney(montoSim), label: 'Monto crédito' })
      }
    } else if (montoSim != null && nroCuotasSim != null) {
      items.push(
        { value: formatMoney(montoSim), label: 'Monto' },
        { value: nroCuotasSim, label: 'Cuotas' },
      )
    }
    return items
  }, [
    personaId,
    clienteLabel,
    solicitudCreditoId,
    cuotas.length,
    totalCuota,
    tem,
    montoSim,
    nroCuotasSim,
  ])

  const productoOpts =
    productosQuery.data?.map((p) => ({
      value: p.productoId,
      label: p.denominacion,
    })) ?? []

  return (
    <CredixPage
      title="Simulador y originación de crédito"
      subtitle="Cliente, plan de pagos, solicitud y alta para aprobación."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/credito">Crédito</Link> },
        { title: 'Simulador' },
      ]}
      stats={simStats}
    >
      <Steps
        size="small"
        current={pasoActual}
        style={{ marginBottom: 24, maxWidth: 720 }}
        items={[
          { title: 'Cliente' },
          { title: 'Simular' },
          { title: 'Solicitud' },
          { title: 'Generar crédito' },
        ]}
      />

      <CredixPanel title="1. Cliente">
        <Space wrap style={{ marginBottom: 12 }}>
          <Input
            placeholder="Buscar cliente (mín. 2 caracteres)"
            value={terminoCliente}
            onChange={(e) => setTerminoCliente(e.target.value)}
            onPressEnter={() => {
              if (terminoCliente.trim().length >= 2) {
                busquedaCliente.mutate(terminoCliente.trim())
              }
            }}
            style={{ width: 280 }}
          />
          <Button
            onClick={() => {
              if (terminoCliente.trim().length < 2) {
                message.warning('Escriba al menos 2 caracteres')
                return
              }
              busquedaCliente.mutate(terminoCliente.trim())
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
            action={
              <Button
                size="small"
                onClick={() => {
                  setPersonaId(null)
                  setClienteLabel('')
                  setSolicitudCreditoId(null)
                }}
              >
                Cambiar
              </Button>
            }
          />
        ) : (
          <CredixDataTable<ClienteBuscarItem>
            size="small"
            rowKey="personaId"
            loading={busquedaCliente.isPending}
            dataSource={busquedaCliente.data ?? []}
            pagination={false}
            locale={{ emptyText: 'Busque un cliente' }}
            columns={[
              { title: 'Cliente', dataIndex: 'label', ellipsis: true },
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
                      setSolicitudCreditoId(null)
                      busquedaCliente.reset()
                    }}
                  >
                    Elegir
                  </Button>
                ),
              },
            ]}
          />
        )}
      </CredixPanel>

      <CredixPanel title="2. Simular plan">
        <Form
          form={form}
          layout="vertical"
          initialValues={{
            monto: 1000,
            formaPago: 'M',
            nroCuotas: 12,
            interesMensual: 5,
            fechaPrimerPago: defaultFecha(),
            gastosAdm: 0,
          }}
          onFinish={(v) => simular.mutate(v)}
        >
          <Space wrap align="start" size="large">
            <Form.Item
              name="monto"
              label="Monto crédito"
              rules={[{ required: true, type: 'number', min: 0.01 }]}
            >
              <InputNumber min={0.01} step={100} style={{ width: 140 }} />
            </Form.Item>
            <Form.Item name="formaPago" label="Modalidad" rules={[{ required: true }]}>
              <Select options={FORMAS_PAGO} style={{ width: 160 }} />
            </Form.Item>
            <Form.Item
              name="nroCuotas"
              label="Cuotas"
              rules={[{ required: true, type: 'number', min: 1 }]}
            >
              <InputNumber min={1} style={{ width: 100 }} />
            </Form.Item>
            <Form.Item
              name="interesMensual"
              label="Interés mensual (%)"
              rules={[{ required: true, type: 'number', min: 0 }]}
            >
              <InputNumber min={0} step={0.1} style={{ width: 120 }} />
            </Form.Item>
            <Form.Item
              name="fechaPrimerPago"
              label="Primer pago"
              rules={[{ required: true }]}
            >
              <Input type="date" style={{ width: 160 }} />
            </Form.Item>
            <Form.Item name="gastosAdm" label="Trámite adm.">
              <InputNumber min={0} step={1} style={{ width: 120 }} />
            </Form.Item>
          </Space>
          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              type="primary"
              icon={<CalculatorOutlined />}
              htmlType="submit"
              loading={simular.isPending}
              disabled={!personaId}
            >
              Simular
            </Button>
          </Form.Item>
        </Form>
        {tem != null && (
          <Text type="secondary" style={{ display: 'block', marginTop: 12 }}>
            TEM calculado: <Text strong>{tem.toFixed(4)}%</Text>
          </Text>
        )}
      </CredixPanel>

      <CredixPanel
        title={
          cuotas.length > 0
            ? `Plan simulado — total: ${formatMoney(totalCuota)}`
            : 'Plan simulado'
        }
      >
        <CredixDataTable<SimuladorCreditoCuota>
          rowKey={(r, i) => String(r.numero ?? i)}
          columns={columns}
          dataSource={cuotas}
          loading={simular.isPending}
          pagination={false}
          size="small"
          scroll={{ x: 700 }}
          locale={{ emptyText: 'Seleccione cliente y pulse Simular' }}
        />
        {cuotas.length > 0 ? (
          <div style={{ marginTop: 16 }}>
            <Paragraph type="secondary" style={{ marginBottom: 8 }}>
              Producto para exportar el plan:
            </Paragraph>
            <Select
              placeholder="Producto"
              style={{ width: 280, marginBottom: 12 }}
              options={productoOpts}
              value={productoId ?? undefined}
              onChange={(v) => setProductoId(v)}
            />
            <InformeExportBar
              csvLoading={exportCsv.isPending}
              pdfLoading={exportPdf.isPending}
              csvDisabled={!productoId}
              pdfDisabled={!productoId}
              onCsv={async () => {
                const v = form.getFieldsValue()
                if (!productoId) return
                exportCsv.mutate({
                  productoId,
                  monto: v.monto,
                  nroCuotas: v.nroCuotas,
                  interesMensual: v.interesMensual,
                  fechaPrimerPago: v.fechaPrimerPago,
                  formaPago: v.formaPago,
                  gastosAdm: v.gastosAdm ?? 0,
                  ga: IND_GASTOS_ADM,
                  cliente: clienteLabel,
                })
              }}
              onPdfTabular={async () => {
                if (!productoId) return
                const v = form.getFieldsValue()
                exportPdf.mutate({
                  productoId,
                  monto: v.monto,
                  nroCuotas: v.nroCuotas,
                  interesMensual: v.interesMensual,
                  fechaPrimerPago: v.fechaPrimerPago,
                  formaPago: v.formaPago,
                  gastosAdm: v.gastosAdm ?? 0,
                  ga: IND_GASTOS_ADM,
                  cliente: clienteLabel,
                })
              }}
            />
          </div>
        ) : null}
      </CredixPanel>

      <CredixPanel title="3. Solicitud (estado CRE)">
        {solicitudCreditoId ? (
          <Alert
            type="info"
            showIcon
            message={`Solicitud #${solicitudCreditoId} lista para generar crédito`}
          />
        ) : (
          <>
            <Paragraph type="secondary">
              Crea la solicitud asociada al cliente antes de generar el crédito.
            </Paragraph>
            <Button
              type="primary"
              icon={<UserAddOutlined />}
              loading={crearSolicitud.isPending}
              disabled={!personaId || oficinaId < 1}
              onClick={() => crearSolicitud.mutate()}
            >
              Crear solicitud
            </Button>
          </>
        )}
      </CredixPanel>

      <CredixPanel title="4. Generar crédito">
        <Space direction="vertical" style={{ width: '100%' }} size="middle">
          <Select
            placeholder="Producto de crédito"
            style={{ width: 320 }}
            loading={productosQuery.isLoading}
            options={productoOpts}
            value={productoId ?? undefined}
            onChange={(v) => setProductoId(v)}
          />
          <Select
            style={{ width: 320 }}
            value="ADE"
            disabled
            options={IND_GASTOS_ADM_OPTIONS}
          />
          <Input.TextArea
            rows={2}
            placeholder="Observación (opcional)"
            value={observacion}
            onChange={(e) => setObservacion(e.target.value)}
          />
          <Checkbox
            checked={indCentralRiesgo}
            onChange={(e) => setIndCentralRiesgo(e.target.checked)}
          >
            Incluir en central de riesgo
          </Checkbox>
          <Button
            type="primary"
            icon={<FileAddOutlined />}
            loading={generarCredito.isPending}
            disabled={
              !solicitudCreditoId ||
              !productoId ||
              cuotas.length < 1 ||
              oficinaId < 1
            }
            onClick={() => generarCredito.mutate()}
          >
            Generar crédito para aprobación
          </Button>
        </Space>
      </CredixPanel>
    </CredixPage>
  )
}
