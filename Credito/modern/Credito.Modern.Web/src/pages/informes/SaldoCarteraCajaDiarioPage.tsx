import { useEffect } from 'react'
import { useMutation } from '@tanstack/react-query'
import { SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber, Select } from 'antd'
import {
  downloadSaldoCarteraCajaDiarioCsv,
  downloadSaldoCarteraCajaDiarioPdf,
  fetchSaldoCarteraCajaDiario,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { GestorSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { buildSaldoCarteraCajaDiarioInformeColumns } from '../../config/saldoCarteraCajaDiarioInformeColumns'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import type {
  RptSaldoCarteraCajaDiarioRow,
  SaldoCarteraCajaDiarioParams,
} from '../../types/api'

const COLUMNS = buildSaldoCarteraCajaDiarioInformeColumns()
const MESES = Array.from({ length: 12 }, (_, i) => ({
  value: i + 1,
  label: String(i + 1).padStart(2, '0'),
}))

const now = new Date()

type FormValues = {
  oficinaId: number
  anioIni: number
  mesIni: number
  anioFin: number
  mesFin: number
  usuarioId?: number
}

function toParams(v: FormValues): SaldoCarteraCajaDiarioParams {
  const p: SaldoCarteraCajaDiarioParams = {
    oficinaId: v.oficinaId,
    anioIni: v.anioIni,
    mesIni: v.mesIni,
    anioFin: v.anioFin,
    mesFin: v.mesFin,
  }
  if (v.usuarioId != null && v.usuarioId > 0) {
    p.usuarioId = v.usuarioId
  }
  return p
}

export function SaldoCarteraCajaDiarioPage() {
  const { session } = useAuth()
  const [form] = Form.useForm<FormValues>()
  const puedeElegirGestor = usePuedeElegirGestorInforme()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    anioIni: now.getFullYear(),
    mesIni: now.getMonth() + 1,
    anioFin: now.getFullYear(),
    mesFin: now.getMonth() + 1,
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
    mutationFn: (v: FormValues) => fetchSaldoCarteraCajaDiario(toParams(v)),
  })

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadSaldoCarteraCajaDiarioCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadSaldoCarteraCajaDiarioPdf(toParams(v)),
  })

  const stats = useInformeStats(consulta, session?.oficinaId)

  return (
    <CredixInformePage
      title="Saldo cartera por caja diario"
      subtitle="Evolución de cartera por caja entre periodos; gestor opcional (TODOS) para roles elevados."
      breadcrumb={reportesCreditoBreadcrumb('Saldo cartera caja')}
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
          <Form.Item name="anioIni" rules={[{ required: true }]}>
            <InputNumber min={2000} max={2100} placeholder="Año ini." style={{ width: 90 }} />
          </Form.Item>
          <Form.Item name="mesIni" rules={[{ required: true }]}>
            <Select options={MESES} style={{ width: 72 }} />
          </Form.Item>
          <Form.Item name="anioFin" rules={[{ required: true }]}>
            <InputNumber min={2000} max={2100} placeholder="Año fin" style={{ width: 90 }} />
          </Form.Item>
          <Form.Item name="mesFin" rules={[{ required: true }]}>
            <Select options={MESES} style={{ width: 72 }} />
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
      <CredixDataTable<RptSaldoCarteraCajaDiarioRow>
        rowKey={(r) => `${r.agenteId}-${r.caja ?? ''}`}
        columns={COLUMNS}
        dataSource={consulta.data ?? []}
        loading={consulta.isPending}
        pagination={{ pageSize: 25 }}
        scroll={{ x: 2600 }}
        locale={{ emptyText: 'Consulte para ver el informe' }}
      />
    </CredixInformePage>
  )
}
