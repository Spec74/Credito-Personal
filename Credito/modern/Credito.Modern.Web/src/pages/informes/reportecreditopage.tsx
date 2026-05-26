import { useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Checkbox, DatePicker, Form, InputNumber, Select } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadReporteCreditoCsv,
  downloadReporteCreditoPdf,
  fetchReporteCredito,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { ReporteCreditoParams, RptCreditoRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'

const { RangePicker } = DatePicker

const ESTADOS_CREDITO = [
  { value: 'CRE', label: 'Solicitud crédito' },
  { value: 'PEN', label: 'Pendiente' },
  { value: 'DES', label: 'Desembolsado' },
  { value: 'PAG', label: 'Pagado' },
  { value: 'REP', label: 'Reprogramado' },
  { value: 'ANU', label: 'Anulado' },
]

type FormValues = {
  oficinaId: number
  estadoCredito: string
  rango: [Dayjs, Dayjs]
  soloMiGestor: boolean
}

function toParams(v: FormValues, gestorId?: number): ReporteCreditoParams {
  const p: ReporteCreditoParams = {
    oficinaId: v.oficinaId,
    estadoCredito: v.estadoCredito,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
  if (v.soloMiGestor && gestorId) {
    p.gestorId = gestorId
  }
  return p
}

export function ReporteCreditoPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  useEffect(() => {
    if (session) {
      form.setFieldsValue({ oficinaId: session.oficinaId })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) =>
      fetchReporteCredito(toParams(v, session?.usuarioId)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) =>
      downloadReporteCreditoCsv(toParams(v, session?.usuarioId)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) =>
      downloadReporteCreditoPdf(toParams(v, session?.usuarioId)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptCreditoRow> = [
    { title: 'Crédito', dataIndex: 'creditoId', width: 75, fixed: 'left' },
    { title: 'Producto', dataIndex: 'producto', width: 100, ellipsis: true },
    { title: 'Cliente', dataIndex: 'cliente', width: 140, ellipsis: true },
    { title: 'Estado', dataIndex: 'estado', width: 90 },
    {
      title: 'Desembolso',
      dataIndex: 'fechaDesembolso',
      width: 100,
      render: formatFecha,
    },
    {
      title: 'Vencimiento',
      dataIndex: 'fechaVcto',
      width: 100,
      render: formatFecha,
    },
    { title: 'Forma pago', dataIndex: 'formaPago', width: 85 },
    { title: 'Cuotas', dataIndex: 'numeroCuotas', width: 60 },
    {
      title: 'Monto crédito',
      dataIndex: 'montoCredito',
      width: 100,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Desembolso',
      dataIndex: 'montoDesembolso',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Observación', dataIndex: 'observacion', ellipsis: true },
  ]

  return (
    <CredixInformePage
      title="Reporte de créditos"
      subtitle="Listado de créditos por estado, fechas y gestor opcional."
      breadcrumb={reportesCreditoBreadcrumb('Reporte créditos')}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={{
            oficinaId: session?.oficinaId ?? 0,
            estadoCredito: 'DES',
            rango: [dayjs().startOf('month'), dayjs()],
            soloMiGestor: false,
          }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="estadoCredito" rules={[{ required: true }]}>
            <Select options={ESTADOS_CREDITO} style={{ width: 180 }} />
          </Form.Item>
          <Form.Item name="rango" rules={[{ required: true }]}>
            <RangePicker format="DD/MM/YYYY" />
          </Form.Item>
          <Form.Item name="soloMiGestor" valuePropName="checked">
            <Checkbox>Solo mi gestión</Checkbox>
          </Form.Item>
          <Form.Item>
            <Button
              type="primary"
              icon={<SearchOutlined />}
              htmlType="submit"
              loading={consulta.isPending}
            >
              Consultar
            </Button>
          </Form.Item>
        </Form>
      }
      exportBar={
        <InformeExportBar
          csvLoading={csv.isPending}
          pdfLoading={pdf.isPending}
          onCsv={async () => csv.mutate(await form.validateFields())}
          onPdfTabular={async () => pdf.mutate(await form.validateFields())}
        />
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              consulta.error instanceof ApiError ? consulta.error.message : 'Error en la consulta'
            }
          />
        ) : null
      }
    >
      <CredixDataTable<RptCreditoRow>
        rowKey="creditoId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Consulte para ver créditos' }}
      />
    </CredixInformePage>
  )
}
