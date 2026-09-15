import { useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, DatePicker, Form, Select } from 'antd'
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
import { CREDITO_ESTADO_REPORTE_OPTIONS } from '../../utils/creditoEstados'
import { GestorSelect, OficinaSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { readUrlDay, readUrlUserId } from '../../utils/informeUrlParams'

const { RangePicker } = DatePicker

type FormValues = {
  oficinaId: number
  estadoCredito: string
  rango: [Dayjs, Dayjs]
  gestorId?: number
}

function toParams(v: FormValues, oficinaSesion: number): ReporteCreditoParams {
  const p: ReporteCreditoParams = {
    oficinaId: oficinaSesion,
    estadoCredito: v.estadoCredito,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
  if (v.gestorId != null && v.gestorId > 0) {
    p.gestorId = v.gestorId
  }
  return p
}

export function ReporteCreditoPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [form] = Form.useForm<FormValues>()

  const consulta = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) {
        throw new Error('Sin oficina en sesión')
      }
      return fetchReporteCredito(toParams(v, session.oficinaId))
    },
  })

  useEffect(() => {
    if (!session) return
    const gestor = readUrlUserId(searchParams)
    const ini = readUrlDay(searchParams, 'pFechaIni', 'fechaIni')
    const fin = readUrlDay(searchParams, 'pFechaFin', 'fechaFin')
    const estado = searchParams.get('estadoCredito')
    const next: FormValues = {
      oficinaId: session.oficinaId,
      estadoCredito: estado && estado.length > 0 ? estado : 'DES',
      rango: [
        ini ?? dayjs().startOf('month'),
        fin ?? dayjs(),
      ],
      gestorId: gestor != null && gestor > 0 ? gestor : undefined,
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('fechaIni') ||
      searchParams.has('pFechaIni') ||
      searchParams.has('estadoCredito') ||
      searchParams.has('usuarioId')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes
  }, [session, searchParams.toString()])

  const csv = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) return Promise.resolve()
      return downloadReporteCreditoCsv(toParams(v, session.oficinaId))
    },
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) return Promise.resolve()
      return downloadReporteCreditoPdf(toParams(v, session.oficinaId))
    },
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
      subtitle="Mismos filtros que Reporte → Crédito: estado, rango de fechas y gestor (TODOS o uno). Oficina = sesión."
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
            gestorId: undefined,
          }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" label="Oficina">
            <OficinaSelect disabled size="middle" />
          </Form.Item>
          <Form.Item name="gestorId" label="Gestor">
            <GestorSelect allowAll legacyList size="middle" />
          </Form.Item>
          <Form.Item name="estadoCredito" rules={[{ required: true }]}>
            <Select options={CREDITO_ESTADO_REPORTE_OPTIONS} style={{ width: 180 }} />
          </Form.Item>
          <Form.Item name="rango" rules={[{ required: true }]}>
            <RangePicker format="DD/MM/YYYY" />
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
