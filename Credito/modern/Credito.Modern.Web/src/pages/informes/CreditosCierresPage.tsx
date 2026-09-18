import { useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Checkbox, Form, InputNumber } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCreditosCierresCsv,
  downloadCreditosCierresPdf,
  fetchCreditosCierres,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage, CredixRangePicker } from '../../components/credix'

import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { InformeRangoGestorParams, RptCreditosCierresRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'


type FormValues = {
  oficinaId: number
  rango: [Dayjs, Dayjs]
  soloMiGestor: boolean
}

function toParams(v: FormValues, usuarioId?: number): InformeRangoGestorParams {
  const p: InformeRangoGestorParams = {
    oficinaId: v.oficinaId,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
  if (v.soloMiGestor && usuarioId) {
    p.usuarioId = usuarioId
  }
  return p
}

export function CreditosCierresPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  useEffect(() => {
    if (session) {
      form.setFieldsValue({ oficinaId: session.oficinaId })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCreditosCierres(toParams(v, session?.usuarioId)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCreditosCierresCsv(toParams(v, session?.usuarioId)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCreditosCierresPdf(toParams(v, session?.usuarioId)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptCreditosCierresRow> = [
    { title: 'Crédito', dataIndex: 'creditoId', width: 75 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    { title: 'Agente', dataIndex: 'agente', width: 120, ellipsis: true },
    {
      title: 'Monto',
      dataIndex: 'montoCredito',
      width: 95,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés',
      dataIndex: 'interes',
      width: 85,
      align: 'right',
      render: formatMoney,
    },
    { title: 'Forma pago', dataIndex: 'formaPago', width: 90 },
    { title: 'Cuotas', dataIndex: 'numeroCuotas', width: 65 },
    {
      title: '1er pago',
      dataIndex: 'fechaPrimerPago',
      width: 100,
      render: formatFecha,
    },
    {
      title: 'Vencimiento',
      dataIndex: 'fechaVencimiento',
      width: 100,
      render: formatFecha,
    },
  ]

  return (
    <CredixInformePage
      title="Créditos cierres"
      subtitle="Créditos cerrados en el rango de fechas por oficina y gestor opcional."
      breadcrumb={reportesCreditoBreadcrumb('Créditos cierres')}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={{
            oficinaId: session?.oficinaId ?? 0,
            rango: [dayjs().startOf('month'), dayjs()],
            soloMiGestor: false,
          }}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="rango" rules={[{ required: true }]}>
            <CredixRangePicker format="DD/MM/YYYY" />
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
      <CredixDataTable<RptCreditosCierresRow>
        rowKey="creditoId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        locale={{ emptyText: 'Consulte para ver créditos cierres' }}
      />
    </CredixInformePage>
  )
}
