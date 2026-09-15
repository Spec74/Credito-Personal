import { SearchOutlined } from '@ant-design/icons'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Form, InputNumber, Segmented } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  downloadCreditoVencidoCsv,
  downloadCreditoVencidoPdf,
  fetchCreditoVencido,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { CreditoVencidoFranja, CreditoVencidoParams, RptCreditoVencidoRow } from '../../types/api'
import { formatMoney } from '../../utils/formatMoney'

const FRANJA_OPCIONES: { label: string; value: CreditoVencidoFranja }[] = [
  { label: 'Todos los vencidos', value: 'todos' },
  { label: 'Vencido sin mora (<60)', value: 'menor60' },
  { label: 'Vencido con mora (>60)', value: 'mayor60' },
  { label: 'Irrecuperable', value: 'irrecuperable' },
]

type FormValues = {
  oficinaId: number
  franja: CreditoVencidoFranja
}

function toParams(v: FormValues): CreditoVencidoParams {
  return { oficinaId: v.oficinaId, franja: v.franja }
}

export function CreditoVencidoPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    franja: 'todos',
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCreditoVencido(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCreditoVencidoCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCreditoVencidoPdf(toParams(v)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)
  const columns: ColumnsType<RptCreditoVencidoRow> = [
    { title: 'Gestor', dataIndex: 'gestor', width: 120, ellipsis: true },
    { title: 'Crédito', dataIndex: 'creditoId', width: 80 },
    { title: 'Cliente', dataIndex: 'cliente', ellipsis: true },
    {
      title: 'Monto crédito',
      dataIndex: 'montoCredito',
      align: 'right',
      render: formatMoney,
    },
    { title: 'Forma pago', dataIndex: 'formaPago', width: 90 },
    {
      title: 'F. vencimiento',
      dataIndex: 'fechaVencimiento',
      width: 110,
      render: (v: string) => (v ? v.slice(0, 10) : '—'),
    },
    {
      title: 'Crédito vencido',
      dataIndex: 'creditoVencido',
      align: 'right',
      render: formatMoney,
    },
    { title: '<60', dataIndex: 'vencidoMenor60', width: 50 },
    { title: '>60', dataIndex: 'vencidoMayor60', width: 50 },
    { title: 'Irrec.', dataIndex: 'vencidoIrrecuperable', width: 55 },
  ]

  return (
    <CredixInformePage
      title="Crédito vencido"
      subtitle="Cartera vencida por franja de días; oficina validada por sesión."
      breadcrumb={reportesCreditoBreadcrumb('Crédito vencido')}
      stats={stats}
      filters={
        <Form form={form} layout="inline" initialValues={defaultValues} onFinish={(v) => consulta.mutate(v)}>
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="franja">
            <Segmented options={FRANJA_OPCIONES} />
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
      <CredixDataTable<RptCreditoVencidoRow>
        rowKey="creditoId"
        columns={columns}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 15 }}
        locale={{ emptyText: 'Ejecute Consultar para cargar datos' }}
      />
    </CredixInformePage>
  )
}
