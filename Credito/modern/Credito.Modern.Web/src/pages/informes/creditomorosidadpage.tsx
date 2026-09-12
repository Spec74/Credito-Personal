import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { AuditOutlined, SearchOutlined } from '@ant-design/icons'
import { Alert, Button, DatePicker, Form, InputNumber, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCreditoMorosidadCsv,
  downloadCreditoMorosidadPdf,
  fetchCreditoMorosidad,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import type { CredixStatItem } from '../../components/credix'
import { MorosidadContactDetalle } from '../../components/reportes/morosidad/MorosidadContactDetalle'
import { buildMorosidadInformeColumns } from '../../config/morosidadInformeColumns'
import { useInformeStats } from '../../hooks/useInformeStats'
import type { CreditoMorosidadParams, RptCreditoMorosidadRow } from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { readUrlDay, readUrlInt, readUrlOfficeId } from '../../utils/informeUrlParams'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { OficinaSelect } from '../../components/reportes/ReporteFiltrosMaestros'

type FormValues = {
  oficinaId: number
  hastaFecha: Dayjs
  diasAtrazoIni: number
  diasAtrazoFin: number
}

function toParams(v: FormValues): CreditoMorosidadParams {
  return {
    oficinaId: v.oficinaId,
    hastaFecha: v.hastaFecha.format('YYYY-MM-DD'),
    diasAtrazoIni: v.diasAtrazoIni,
    diasAtrazoFin: v.diasAtrazoFin,
  }
}

const MOROSIDAD_COLUMNS = buildMorosidadInformeColumns()

export function CreditoMorosidadPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [form] = Form.useForm<FormValues>()
  const [expandedKeys, setExpandedKeys] = useState<number[]>([])

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    hastaFecha: dayjs(),
    diasAtrazoIni: 1,
    diasAtrazoFin: 9999,
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues) => fetchCreditoMorosidad(toParams(v)),
  })

  useEffect(() => {
    if (!session) return
    const fromUrl: Partial<FormValues> = {
      oficinaId: readUrlOfficeId(searchParams) ?? session.oficinaId,
      hastaFecha: readUrlDay(searchParams, 'pFechaHasta', 'hastaFecha'),
      diasAtrazoIni: readUrlInt(searchParams, 'pDiasAtrazoIni', 'diasAtrazoIni'),
      diasAtrazoFin: readUrlInt(searchParams, 'pDiasAtrazoFin', 'diasAtrazoFin'),
    }
    const next: FormValues = {
      oficinaId: fromUrl.oficinaId ?? session.oficinaId,
      hastaFecha: fromUrl.hastaFecha ?? dayjs(),
      diasAtrazoIni: fromUrl.diasAtrazoIni ?? 1,
      diasAtrazoFin: fromUrl.diasAtrazoFin ?? 9999,
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('hastaFecha') ||
      searchParams.has('pFechaHasta') ||
      searchParams.has('diasAtrazoIni') ||
      searchParams.has('pDiasAtrazoIni')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta solo al abrir con query de reportes
  }, [session, searchParams.toString()])

  const csv = useMutation({
    mutationFn: (v: FormValues) => downloadCreditoMorosidadCsv(toParams(v)),
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => downloadCreditoMorosidadPdf(toParams(v)),
  })

  const filas = useMemo(() => consulta.data ?? [], [consulta.data])
  const queried = consulta.isSuccess || consulta.isError

  const statsExtras = useMemo((): CredixStatItem[] => {
    if (!consulta.isSuccess || filas.length === 0) {
      return []
    }
    const totalDeuda = filas.reduce((s, r) => s + (r.deudaAtrazo ?? 0), 0)
    const hasta = form.getFieldValue('hastaFecha') as Dayjs | undefined
    return [
      {
        value: hasta ? formatFecha(hasta.format('YYYY-MM-DD')) : '—',
        label: 'Hasta la fecha',
      },
      {
        value: formatMoney(totalDeuda),
        label: 'Deuda atraso (total)',
        tone: 'red',
      },
    ]
  }, [consulta.isSuccess, filas, form])

  const stats = useInformeStats(consulta, session?.oficinaId, statsExtras)

  const exportDisabled = !consulta.isSuccess || filas.length === 0

  const columns: ColumnsType<RptCreditoMorosidadRow> = MOROSIDAD_COLUMNS

  const filtrosAplicados =
    queried && consulta.isSuccess ? (
      <Typography.Text type="secondary" className="credix-morosidad-filtros-aplicados">
        Corte {formatFecha(form.getFieldValue('hastaFecha')?.format('YYYY-MM-DD'))} · días de atraso{' '}
        {form.getFieldValue('diasAtrazoIni')} al {form.getFieldValue('diasAtrazoFin')} (paridad Reporte
        Morosidad MVC)
      </Typography.Text>
    ) : null

  return (
    <CredixInformePage
      title="Reporte morosidad"
      subtitle={
        <>
          Paridad <strong>Reporte → Crédito → Reporte Morosidad</strong> y PDF{' '}
          <em>rptCreditoMorosidad</em>. Use <strong>+</strong> en cada fila para cliente, celular y
          dirección (segunda línea del PDF).
        </>
      }
      breadcrumb={reportesCreditoBreadcrumb('Reporte morosidad')}
      stats={stats}
      panelTitle="Créditos con morosidad"
      tableResultCount={filas.length}
      enableTableSearch={queried && filas.length > 0}
      searchPlaceholder="Buscar por crédito, artículo o contacto…"
      filters={
        <Form form={form} layout="inline" initialValues={defaultValues} onFinish={(v) => consulta.mutate(v)}>
          <Form.Item name="oficinaId" label="Oficina">
            <OficinaSelect disabled size="middle" />
          </Form.Item>
          <Form.Item
            name="hastaFecha"
            label="Hasta la fecha"
            rules={[{ required: true, message: 'Indique la fecha' }]}
          >
            <DatePicker format="DD/MM/YYYY" allowClear={false} />
          </Form.Item>
          <Form.Item
            name="diasAtrazoIni"
            label="Días atraso inicio"
            rules={[{ required: true }]}
          >
            <InputNumber min={0} max={500000} style={{ width: 110 }} />
          </Form.Item>
          <Form.Item
            name="diasAtrazoFin"
            label="Días atraso final"
            rules={[{ required: true }]}
          >
            <InputNumber min={0} max={500000} style={{ width: 110 }} />
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
          csvDisabled={exportDisabled}
          pdfDisabled={exportDisabled}
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
      {filtrosAplicados ? <div style={{ marginBottom: 12 }}>{filtrosAplicados}</div> : null}

      {consulta.isSuccess && filas.length === 0 ? (
        <Alert
          type="info"
          showIcon
          icon={<AuditOutlined />}
          message="Sin créditos en mora"
          description="No hay registros para la oficina, fecha de corte y rango de días indicados."
          style={{ marginBottom: 12 }}
        />
      ) : null}

      <CredixDataTable<RptCreditoMorosidadRow>
        rowKey="creditoId"
        columns={columns}
        dataSource={filas}
        loading={consulta.isPending}
        pagination={{ pageSize: 20, showSizeChanger: true, pageSizeOptions: ['10', '20', '50', '100'] }}
        locale={{ emptyText: 'Pulse Consultar (mismos filtros que el reporte legacy).' }}
        expandable={{
          expandedRowKeys: expandedKeys,
          onExpandedRowsChange: (keys) => {
            const ids = keys.map((k) => Number(k))
            setExpandedKeys(ids.length > 1 ? [ids[ids.length - 1]!] : ids)
          },
          expandedRowRender: (row) => <MorosidadContactDetalle row={row} />,
          rowExpandable: () => true,
          columnWidth: 40,
        }}
      />
    </CredixInformePage>
  )
}
