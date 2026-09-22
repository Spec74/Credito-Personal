import { useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { FileTextOutlined, SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber, Select } from 'antd'
import dayjs from 'dayjs'
import {
  downloadCentralRiesgoGenerarCsv,
  downloadCentralRiesgoGenerarPdf,
  downloadCentralRiesgoGenerarTxt,
  fetchCentralRiesgoGenerar,
  type CentralRiesgoGenerarRow,
  type CentralRiesgoParams,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { buildCentralRiesgoInformeColumns } from '../../config/centralRiesgoInformeColumns'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'

const COLUMNS = buildCentralRiesgoInformeColumns()
const MESES = [
  { value: 1, label: 'Enero' },
  { value: 2, label: 'Febrero' },
  { value: 3, label: 'Marzo' },
  { value: 4, label: 'Abril' },
  { value: 5, label: 'Mayo' },
  { value: 6, label: 'Junio' },
  { value: 7, label: 'Julio' },
  { value: 8, label: 'Agosto' },
  { value: 9, label: 'Septiembre' },
  { value: 10, label: 'Octubre' },
  { value: 11, label: 'Noviembre' },
  { value: 12, label: 'Diciembre' },
]

type FormValues = {
  oficinaId: number
  anio: number
  mes: number
}

function toParams(v: FormValues): CentralRiesgoParams {
  return { oficinaId: v.oficinaId, anio: v.anio, mes: v.mes }
}

export function CentralRiesgoPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()
  useEffect(() => {
    if (session) {
      const prevMonth = dayjs().subtract(1, 'month')
      form.setFieldsValue({
        oficinaId: session.oficinaId,
        anio: prevMonth.year(),
        mes: prevMonth.month() + 1,
      })
    }
  }, [session, form])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCentralRiesgoGenerar(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCentralRiesgoGenerarCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCentralRiesgoGenerarPdf(toParams(v)),
  })

  const txt = useMutation({
    mutationFn: (v: FormValues) => downloadCentralRiesgoGenerarTxt(toParams(v)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)

  return (
    <CredixInformePage
      title="Central de riesgo"
      subtitle="Generación mensual para central de riesgo; incluye exportación TXT DM007898."
      breadcrumb={reportesCreditoBreadcrumb('Central de riesgo')}
      stats={stats}
      filters={
        <Form form={form} layout="inline" onFinish={(v) => consulta.mutate(v)}>
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="anio" rules={[{ required: true }]}>
            <InputNumber min={1900} max={2100} placeholder="Año" style={{ width: 100 }} />
          </Form.Item>
          <Form.Item name="mes" rules={[{ required: true }]}>
            <Select options={MESES} placeholder="Mes" style={{ width: 140 }} />
          </Form.Item>
          <Form.Item>
            <Button
              type="primary"
              icon={<SearchOutlined />}
              htmlType="submit"
              loading={consulta.isPending}
            >
              Generar / consultar
            </Button>
          </Form.Item>
        </Form>
      }
      exportBar={
        <>
          <InformeExportBar
            csvLoading={csv.isPending}
            pdfLoading={pdf.isPending}
            onCsv={async () => csv.mutate(await form.validateFields())}
            onPdfTabular={async () => pdf.mutate(await form.validateFields())}
          />
          <Button
            icon={<FileTextOutlined />}
            loading={txt.isPending}
            onClick={async () => txt.mutate(await form.validateFields())}
          >
            TXT DM007898
          </Button>
        </>
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              consulta.error instanceof ApiError ? consulta.error.message : 'Error al generar'
            }
          />
        ) : null
      }
    >
      {consulta.data ? (
        <p style={{ marginBottom: 12, color: 'var(--ant-color-text-secondary)' }}>
          {consulta.data.length} registro(s). Montos de deuda en formato legacy; use CSV/PDF para
          revisión tabular.
        </p>
      ) : null}
      <CredixDataTable<CentralRiesgoGenerarRow>
        rowKey={(r) => String(r.creditoId)}
        columns={COLUMNS}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        scroll={{ x: 1800 }}
        locale={{ emptyText: 'Seleccione año y mes' }}
      />
    </CredixInformePage>
  )
}
