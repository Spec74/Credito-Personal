import { useEffect, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { Alert, Button, Form, InputNumber, Typography, message } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import {
  downloadMorosidadGestorCsv,
  downloadMorosidadGestorPdf,
  fetchCajaPorCajero,
  fetchCobroDiario,
} from '../../api/creditoPlanes'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useAuth } from '../../auth/useAuth'
import { ApiError } from '../../api/errors'
import type { RptCobroDiarioRow } from '../../types/api'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import type { CredixStatItem } from '../../components/credix'
import { GestorSelect, OficinaSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { buildCobroDiarioInformeColumns } from '../../config/cobroDiarioInformeColumns'
import { useInformeStats } from '../../hooks/useInformeStats'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import { buildCobroDiarioKpiExtras } from '../../utils/cobroDiarioKpis'
import { formatFecha } from '../../utils/formatFecha'
import {
  gestorLabelFromId,
  toCobroDiarioQuery,
} from '../../utils/gestorInformeForm'
import { readUrlUserId } from '../../utils/informeUrlParams'

const MOROSIDAD_COLUMNS = buildCobroDiarioInformeColumns()

type FormValues = {
  oficinaId: number
  usuarioId?: number
}

export function MorosidadGestorPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [form] = Form.useForm<FormValues>()
  const puedeElegirGestor = usePuedeElegirGestorInforme()

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) {
        throw new Error('Sin oficina en sesión')
      }
      return fetchCobroDiario(toCobroDiarioQuery(session.oficinaId, v.usuarioId, true))
    },
    onSuccess: (data) => message.success(`${data.length} crédito(s) con mora`),
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'Error al consultar'),
  })

  useEffect(() => {
    if (!session) return
    const uid = readUrlUserId(searchParams)
    const next: FormValues = {
      oficinaId: session.oficinaId,
      usuarioId:
        uid != null && uid > 0 ? uid : puedeElegirGestor ? undefined : session.usuarioId,
    }
    form.setFieldsValue(next)
    if (
      searchParams.has('usuarioId') ||
      searchParams.has('pUsuarioId') ||
      searchParams.has('oficinaId') ||
      searchParams.has('pOficinaId')
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes
  }, [session, searchParams.toString(), puedeElegirGestor])

  const csv = useMutation({
    mutationFn: async (v: FormValues) => {
      if (!session?.oficinaId) return
      await downloadMorosidadGestorCsv(toCobroDiarioQuery(session.oficinaId, v.usuarioId, true))
    },
  })

  const pdf = useMutation({
    mutationFn: async (v: FormValues) => {
      if (!session?.oficinaId) return
      await downloadMorosidadGestorPdf(toCobroDiarioQuery(session.oficinaId, v.usuarioId, true))
    },
  })

  const filas = useMemo(() => consulta.data ?? [], [consulta.data])
  const queried = consulta.isSuccess || consulta.isError
  const exportDisabled = !consulta.isSuccess || filas.length === 0
  const gestorId = form.getFieldValue('usuarioId') as number | undefined

  const cajaQuery = useQuery({
    queryKey: ['caja-por-cajero', gestorId],
    queryFn: () => fetchCajaPorCajero(gestorId!),
    enabled: consulta.isSuccess && gestorId != null && gestorId > 0,
    staleTime: 60_000,
  })

  const morosoExtras = useMemo((): CredixStatItem[] => {
    if (!consulta.isSuccess || filas.length === 0) {
      return []
    }
    return buildCobroDiarioKpiExtras(filas, {
      soloMorosidad: true,
      caja: cajaQuery.data?.denominacion,
    })
  }, [consulta.isSuccess, filas, cajaQuery.data?.denominacion])

  const stats = useInformeStats(consulta, session?.oficinaId, morosoExtras)

  return (
    <CredixInformePage
      title="Morosidad por gestor"
      subtitle={
        <>
          Paridad <strong>Reporte → Crédito → Morosidad gestor</strong> (<em>indMora=true</em>,
          mismo SP que cobro diario). Solo créditos con mora &gt; 0; gestor opcional (TODOS) para
          roles elevados.
        </>
      }
      breadcrumb={reportesCreditoBreadcrumb('Morosidad gestor')}
      stats={stats}
      panelTitle="REPORTE DE MOROSIDAD"
      tableResultCount={filas.length}
      enableTableSearch={queried && filas.length > 0}
      searchPlaceholder="Buscar por crédito, cliente o dirección…"
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
          Fecha {formatFecha(dayjs().format('YYYY-MM-DD'))} · Oficina sesión #{session?.oficinaId} ·{' '}
          {gestorLabelFromId(gestorId)} · Solo mora &gt; 0
          {cajaQuery.data?.denominacion ? ` · Caja ${cajaQuery.data.denominacion}` : ''}
        </Typography.Text>
      ) : null}

      <CredixDataTable<RptCobroDiarioRow>
        rowKey={(r) => `${r.creditoId}-${r.nro ?? r.orden ?? ''}`}
        columns={MOROSIDAD_COLUMNS}
        dataSource={filas}
        loading={consulta.isPending}
        pagination={{ pageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        locale={{ emptyText: 'Pulse Consultar (export PDF/CSV aplica filtro mora en servidor).' }}
      />
    </CredixInformePage>
  )
}
