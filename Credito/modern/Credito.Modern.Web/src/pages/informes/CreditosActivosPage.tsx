import { useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber } from 'antd'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCreditosActivosCsv,
  downloadCreditosActivosPdf,
  fetchCreditosActivos,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage, CredixRangePicker } from '../../components/credix'
import { GestorSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { buildCreditosActivosInformeColumns } from '../../config/creditosActivosInformeColumns'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import type { InformeRangoGestorParams, RptCreditosActivosRow } from '../../types/api'

const COLUMNS = buildCreditosActivosInformeColumns()

type FormValues = {
  oficinaId: number
  rango: [Dayjs, Dayjs]
  usuarioId?: number
}

function toParams(v: FormValues): InformeRangoGestorParams {
  const p: InformeRangoGestorParams = {
    oficinaId: v.oficinaId,
    fechaIni: v.rango[0].format('YYYY-MM-DD'),
    fechaFin: v.rango[1].format('YYYY-MM-DD'),
  }
  if (v.usuarioId != null && v.usuarioId > 0) {
    p.usuarioId = v.usuarioId
  }
  return p
}

export function CreditosActivosPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()
  const puedeElegirGestor = usePuedeElegirGestorInforme()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    rango: [dayjs().startOf('month'), dayjs()],
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
  }

  useEffect(() => {
    if (!session) return
    form.setFieldsValue({
      oficinaId: session.oficinaId,
      usuarioId: puedeElegirGestor ? form.getFieldValue('usuarioId') : session.usuarioId,
    })
  }, [session, form, puedeElegirGestor])

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCreditosActivos(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCreditosActivosCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCreditosActivosPdf(toParams(v)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)

  return (
    <CredixInformePage
      title="Créditos activos"
      subtitle="Créditos activos por oficina y rango de fechas; gestor opcional (TODOS) para roles elevados."
      breadcrumb={reportesCreditoBreadcrumb('Créditos activos')}
      stats={stats}
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={defaultValues}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" hidden>
            <InputNumber />
          </Form.Item>
          <Form.Item name="rango" rules={[{ required: true }]}>
            <CredixRangePicker format="DD/MM/YYYY" />
          </Form.Item>
          {puedeElegirGestor ? (
            <Form.Item name="usuarioId" label="Gestor">
              <GestorSelect allowAll legacyList size="middle" />
            </Form.Item>
          ) : (
            <Form.Item name="usuarioId" hidden>
              <InputNumber />
            </Form.Item>
          )}
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
      <CredixDataTable<RptCreditosActivosRow>
        rowKey="creditoId"
        columns={COLUMNS}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        scroll={{ x: 2200 }}
        locale={{ emptyText: 'Consulte para ver créditos activos' }}
      />
    </CredixInformePage>
  )
}
