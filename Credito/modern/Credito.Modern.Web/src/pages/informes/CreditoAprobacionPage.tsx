import { useEffect, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { CheckCircleOutlined, SearchOutlined } from '@ant-design/icons'
import { Alert, Button, Form, InputNumber, Typography } from 'antd'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCreditoAprobacionCsv,
  downloadCreditoAprobacionPdf,
  fetchCreditoAprobacion,
} from '../../api/creditoPlanes'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import { CredixDataTable, CredixDatePicker, CredixInformePage } from '../../components/credix'
import type { CredixStatItem } from '../../components/credix'
import { GestorSelect, OficinaSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { buildAprobacionInformeColumns } from '../../config/aprobacionInformeColumns'
import { useInformeStats } from '../../hooks/useInformeStats'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import type {
  CreditoAprobacionInformeParams,
  RptCreditoAprobacionRow,
} from '../../types/api'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { readUrlDay, readUrlUserId } from '../../utils/informeUrlParams'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { gestorLabelFromId } from '../../utils/gestorInformeForm'

type FormValues = {
  oficinaId: number
  fechaAprobacion: Dayjs
  usuarioId?: number
}

function toParams(v: FormValues, oficinaSesion: number): CreditoAprobacionInformeParams {
  const p: CreditoAprobacionInformeParams = {
    oficinaId: oficinaSesion,
    fechaAprobacion: v.fechaAprobacion.format('YYYY-MM-DD'),
  }
  if (v.usuarioId != null && v.usuarioId > 0) {
    p.usuarioId = v.usuarioId
  }
  return p
}

const APROBACION_COLUMNS = buildAprobacionInformeColumns()

export function CreditoAprobacionPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [form] = Form.useForm<FormValues>()
  const puedeElegirGestor = usePuedeElegirGestorInforme()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    fechaAprobacion: dayjs(),
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) {
        throw new Error('Sin oficina en sesión')
      }
      return fetchCreditoAprobacion(toParams(v, session.oficinaId))
    },
  })

  useEffect(() => {
    if (!session) return
    const uid = readUrlUserId(searchParams)
    const next: FormValues = {
      oficinaId: session.oficinaId,
      fechaAprobacion:
        readUrlDay(searchParams, 'pFecha', 'fechaAprobacion') ?? dayjs(),
      usuarioId:
        uid != null && uid > 0 ? uid : puedeElegirGestor ? undefined : session.usuarioId,
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('fechaAprobacion') ||
      searchParams.has('pFecha') ||
      searchParams.has('usuarioId') ||
      searchParams.has('pUsuarioId') ||
      searchParams.has('oficinaId') ||
      searchParams.has('pOficinaId')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta al abrir desde índice reportes
  }, [session, searchParams.toString(), puedeElegirGestor])

  const csv = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) return Promise.resolve()
      return downloadCreditoAprobacionCsv(toParams(v, session.oficinaId))
    },
  })

  const pdf = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) return Promise.resolve()
      return downloadCreditoAprobacionPdf(toParams(v, session.oficinaId))
    },
  })

  const filas = useMemo(() => consulta.data ?? [], [consulta.data])
  const queried = consulta.isSuccess || consulta.isError

  const statsExtras = useMemo((): CredixStatItem[] => {
    if (!consulta.isSuccess || filas.length === 0) {
      return []
    }
    const totalCredito = filas.reduce((s, r) => s + r.montoCredito, 0)
    const totalDesembolso = filas.reduce((s, r) => s + r.montoDesembolso, 0)
    const fecha = form.getFieldValue('fechaAprobacion') as Dayjs | undefined
    return [
      {
        value: fecha ? formatFecha(fecha.format('YYYY-MM-DD')) : '—',
        label: 'Fecha aprobación',
      },
      {
        value: formatMoney(totalCredito),
        label: 'Σ monto crédito',
        tone: 'green',
      },
      {
        value: formatMoney(totalDesembolso),
        label: 'Σ desembolso',
      },
    ]
  }, [consulta.isSuccess, filas, form])

  const stats = useInformeStats(consulta, session?.oficinaId, statsExtras)
  const exportDisabled = !consulta.isSuccess || filas.length === 0
  const gestorLabel = gestorLabelFromId(form.getFieldValue('usuarioId') as number | undefined)

  return (
    <CredixInformePage
      title="Créditos aprobados"
      subtitle={
        <>
          Paridad <strong>Reporte → Crédito → Créditos aprobados</strong> y PDF/XLS{' '}
          <em>rptCreditoAprobacion</em>. Filtros: gestor (TODOS para roles elevados) y fecha;
          oficina = la de su sesión.
        </>
      }
      breadcrumb={reportesCreditoBreadcrumb('Créditos aprobados')}
      stats={stats}
      panelTitle="Créditos aprobados del día"
      tableResultCount={filas.length}
      enableTableSearch={queried && filas.length > 0}
      searchPlaceholder="Buscar por crédito, cliente, oficina o gestor…"
      filters={
        <Form
          form={form}
          layout="inline"
          initialValues={defaultValues}
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="oficinaId" label="Oficina">
            <OficinaSelect disabled size="middle" />
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
          <Form.Item
            name="fechaAprobacion"
            label="Fecha"
            rules={[{ required: true, message: 'Indique la fecha' }]}
          >
            <CredixDatePicker allowClear={false} />
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
      {queried && consulta.isSuccess ? (
        <Typography.Text type="secondary" className="credix-aprobacion-filtros-aplicados">
          Oficina sesión #{session?.oficinaId} · Fecha{' '}
          {formatFecha(form.getFieldValue('fechaAprobacion')?.format('YYYY-MM-DD'))} · {gestorLabel}
        </Typography.Text>
      ) : null}

      {consulta.isSuccess && filas.length === 0 ? (
        <Alert
          type="info"
          showIcon
          icon={<CheckCircleOutlined />}
          message="Sin aprobaciones"
          description="No hay créditos aprobados para la fecha y gestor indicados."
          style={{ marginBottom: 12, marginTop: 12 }}
        />
      ) : null}

      <CredixDataTable<RptCreditoAprobacionRow>
        rowKey="creditoId"
        columns={APROBACION_COLUMNS}
        dataSource={filas}
        loading={consulta.isPending}
        pagination={{ pageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        locale={{ emptyText: 'Pulse Consultar (mismos filtros que el reporte legacy).' }}
      />
    </CredixInformePage>
  )
}
