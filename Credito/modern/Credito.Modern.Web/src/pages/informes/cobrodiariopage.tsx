import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { Alert, Button, Form, InputNumber, Typography, message } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import { InformeExportBar } from '../../components/informes/InformeExportBar'
import {
  fetchCajaPorCajero,
  fetchCobroDiario,
  generarRutaCobros,
  openCobroDiarioCsvInTab,
  openCobroDiarioPdfInTab,
} from '../../api/creditoPlanes'
import { useAuth } from '../../auth/useAuth'
import { ApiError } from '../../api/errors'
import type { RptCobroDiarioRow } from '../../types/api'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import type { CredixStatItem } from '../../components/credix'
import { GestorSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { buildCobroDiarioInformeColumns } from '../../config/cobroDiarioInformeColumns'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import { buildCobroDiarioKpiExtras } from '../../utils/cobroDiarioKpis'
import { formatFecha } from '../../utils/formatFecha'
import {
  gestorLabelFromId,
  toCobroDiarioQuery,
} from '../../utils/gestorInformeForm'
import { readUrlUserId } from '../../utils/informeUrlParams'
import { canViewReporteCredito } from '../../utils/reporteCreditoAccess'

const COBRO_DIARIO_COLUMNS = buildCobroDiarioInformeColumns()

type FormValues = {
  oficinaId: number
  usuarioId?: number
}

export function CobroDiarioPage() {
  const { session } = useAuth()
  const [searchParams] = useSearchParams()
  const [selectedIds, setSelectedIds] = useState<number[]>([])
  const [form] = Form.useForm<FormValues>()

  const puedeElegirGestor = canViewReporteCredito(session?.roles ?? [])

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues) => {
      if (!session?.oficinaId) {
        throw new Error('Sin oficina en sesión')
      }
      if (!v.usuarioId || v.usuarioId < 1) {
        throw new Error('Seleccione un gestor')
      }
      return fetchCobroDiario(toCobroDiarioQuery(session.oficinaId, v.usuarioId))
    },
    onSuccess: (data) => message.success(`${data.length} registro(s)`),
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
      next.usuarioId != null &&
      next.usuarioId > 0 &&
      (searchParams.has('usuarioId') || searchParams.has('pUsuarioId'))
    ) {
      consulta.mutate(next)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes
  }, [session, searchParams.toString(), puedeElegirGestor])

  const openExport = (kind: 'csv' | 'pdf') => {
    if (!session?.oficinaId) {
      message.error('Sin oficina en sesión')
      return
    }
    const usuarioId = form.getFieldValue('usuarioId') as number | undefined
    if (!usuarioId || usuarioId < 1) {
      message.error('Seleccione un gestor')
      return
    }
    const query = toCobroDiarioQuery(session.oficinaId, usuarioId)
    if (kind === 'csv') {
      void openCobroDiarioCsvInTab(query)
      return
    }
    void openCobroDiarioPdfInTab(query)
  }

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

  const cobroExtras = useMemo((): CredixStatItem[] => {
    if (!consulta.isSuccess || filas.length === 0) {
      return []
    }
    return buildCobroDiarioKpiExtras(filas, {
      caja: cajaQuery.data?.denominacion,
      seleccionados: selectedIds.length,
    })
  }, [consulta.isSuccess, filas, selectedIds.length, cajaQuery.data?.denominacion])

  const cobroStats = useInformeStats(consulta, session?.oficinaId, cobroExtras)

  const rutaWa = useMutation({
    mutationFn: generarRutaCobros,
    onSuccess: (res) => {
      if (!res.exito || !res.urlCortita) {
        message.error(res.mensaje ?? 'No se pudo generar la ruta')
        return
      }
      const apiBase = (import.meta.env.VITE_API_BASE_URL as string).replace(/\/$/, '')
      const relative = (res.urlCortita ?? '').replace(/^\/api\/v1/, '')
      window.open(`${apiBase}${relative}`, '_blank', 'noopener,noreferrer')
      message.success('Abra el enlace en el celular (WhatsApp)')
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'Error al generar ruta'),
  })

  return (
    <CredixInformePage
      title="Cobro diario"
      subtitle={
        <>
          Paridad <strong>Reporte → Crédito → Cobro diario</strong> y PDF/XLS{' '}
          <em>rptCobroDiario</em>. Requiere gestor; oficina = sesión. Seleccione filas para ruta
          GPS de cobranza.
        </>
      }
      breadcrumb={reportesCreditoBreadcrumb('Cobro diario')}
      stats={cobroStats}
      panelTitle="Cartera del día del gestor"
      tableResultCount={filas.length}
      enableTableSearch={queried && filas.length > 0}
      searchPlaceholder="Buscar por crédito, cliente, dirección o celular…"
      filters={
        <>
          <Form
            form={form}
            layout="inline"
            initialValues={defaultValues}
            onFinish={(v) => consulta.mutate(v)}
          >
            <Form.Item name="oficinaId" hidden>
              <InputNumber />
            </Form.Item>
            {puedeElegirGestor ? (
              <Form.Item
                name="usuarioId"
                label="Gestor"
                rules={[{ required: true, message: 'Seleccione un gestor' }]}
              >
                <GestorSelect legacyList size="middle" />
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
          <Button
            disabled={selectedIds.length === 0}
            loading={rutaWa.isPending}
            onClick={() => rutaWa.mutate(selectedIds)}
          >
            Ruta WhatsApp ({selectedIds.length})
          </Button>
        </>
      }
      exportBar={
        <InformeExportBar
          csvLoading={false}
          pdfLoading={false}
          csvDisabled={exportDisabled}
          pdfDisabled={exportDisabled}
          onCsv={() => openExport('csv')}
          onPdfTabular={() => openExport('pdf')}
        />
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            style={{ marginBottom: 16 }}
            message={
              consulta.error instanceof ApiError
                ? consulta.error.message
                : 'Error en la consulta'
            }
          />
        ) : null
      }
    >
      {queried && consulta.isSuccess ? (
        <Typography.Text type="secondary" className="credix-aprobacion-filtros-aplicados">
          Fecha {formatFecha(dayjs().format('YYYY-MM-DD'))} · Oficina sesión #{session?.oficinaId} ·{' '}
          {gestorLabelFromId(gestorId)}
          {cajaQuery.data?.denominacion
            ? ` · Caja ${cajaQuery.data.denominacion}`
            : gestorId != null && gestorId > 0
              ? ' · Caja —'
              : ''}
        </Typography.Text>
      ) : null}

      <CredixDataTable<RptCobroDiarioRow>
        rowKey={(r) => `${r.creditoId}-${r.nro ?? r.orden ?? ''}`}
        rowSelection={{
          selectedRowKeys: selectedIds,
          onChange: (keys) => setSelectedIds(keys.map((k) => Number(k))),
        }}
        columns={COBRO_DIARIO_COLUMNS}
        dataSource={filas}
        loading={consulta.isPending}
        pagination={{ pageSize: 25, showSizeChanger: true, pageSizeOptions: ['10', '25', '50', '100'] }}
        locale={{ emptyText: 'Pulse Consultar (gestor obligatorio, como en MVC).' }}
      />
    </CredixInformePage>
  )
}
