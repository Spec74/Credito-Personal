import { lazy, Suspense, useEffect, useMemo, useState, type Key } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import dayjs from 'dayjs'
import { Alert, Button, Form, InputNumber, Spin, Switch, Typography, message } from 'antd'
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
import { GestorSelect, OficinaSelect } from '../../components/reportes/ReporteFiltrosMaestros'
import { useEsCentral } from '../../hooks/useEsCentral'
import { buildCobroDiarioInformeColumns } from '../../config/cobroDiarioInformeColumns'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { useInformeStats } from '../../hooks/useInformeStats'
import { buildCobroDiarioKpiExtras } from '../../utils/cobroDiarioKpis'
import { formatFecha } from '../../utils/formatFecha'
import {
  gestorLabelFromId,
  toCobroDiarioQuery,
} from '../../utils/gestorInformeForm'
import { readUrlUserId, resolveInformeOficinaId } from '../../utils/informeUrlParams'
import { canViewReporteCredito } from '../../utils/reporteCreditoAccess'
import type { RutaCobradorResult } from '../caja/cajaDiario/RutaCobradorResultModal'

const RutaCobradorResultModal = lazy(() =>
  import('../caja/cajaDiario/RutaCobradorResultModal').then((m) => ({
    default: m.RutaCobradorResultModal,
  })),
)

const COBRO_DIARIO_COLUMNS = buildCobroDiarioInformeColumns()
const MAX_RUTA_WA = 25

function buildApiUrl(urlCortita: string): string {
  const apiBase = (import.meta.env.VITE_API_BASE_URL as string).replace(/\/$/, '')
  const relative = urlCortita.replace(/^\/api\/v1/, '')
  return `${apiBase}${relative}`
}

type FormValues = {
  oficinaId: number
  usuarioId?: number
}

function readSoloMora(sp: URLSearchParams): boolean {
  const raw = (sp.get('soloMora') ?? sp.get('solo_mora') ?? '').trim().toLowerCase()
  return raw === '1' || raw === 'true' || raw === 'yes'
}

export function CobroDiarioPage() {
  const { session } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const [selectedIds, setSelectedIds] = useState<number[]>([])
  const [rutaResult, setRutaResult] = useState<RutaCobradorResult | null>(null)
  const [rutaOpen, setRutaOpen] = useState(false)
  const [form] = Form.useForm<FormValues>()
  const soloMora = readSoloMora(searchParams)
  const { esCentral } = useEsCentral()

  const puedeElegirGestor = canViewReporteCredito(session?.roles ?? [])

  const defaultValues: FormValues = {
    oficinaId: session?.oficinaId ?? 0,
    usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
  }

  const consulta = useMutation({
    mutationFn: (v: FormValues & { soloMora?: boolean }) => {
      if (!v.oficinaId) {
        throw new Error('Sin oficina en sesión')
      }
      if (!v.usuarioId || v.usuarioId < 1) {
        throw new Error('Seleccione un gestor')
      }
      return fetchCobroDiario(
        toCobroDiarioQuery(v.oficinaId, v.usuarioId, v.soloMora ?? soloMora),
      )
    },
    onSuccess: (data) => {
      setSelectedIds([])
      message.success(
        soloMora
          ? `${data.length} crédito(s) con mora`
          : `${data.length} registro(s)`,
      )
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'Error al consultar'),
  })

  useEffect(() => {
    if (!session) return
    const uid = readUrlUserId(searchParams)
    const next: FormValues = {
      oficinaId: resolveInformeOficinaId(searchParams, session.oficinaId),
      usuarioId:
        uid != null && uid > 0
          ? uid
          : puedeElegirGestor
            ? undefined
            : session.usuarioId,
    }
    form.setFieldsValue(next)
    const hasUsuario = next.usuarioId != null && next.usuarioId > 0
    const autoFromUrl =
      hasUsuario &&
      (searchParams.has('usuarioId') ||
        searchParams.has('pUsuarioId') ||
        searchParams.has('oficinaId') ||
        searchParams.has('pOficinaId'))
    // Gestor: consultar al entrar (atajo dashboard / accesos).
    // También si llega con ?soloMora=1 y ya hay gestor de sesión.
    const autoGestor = !puedeElegirGestor && hasUsuario
    const autoSoloMoraConGestor = soloMora && hasUsuario && (autoFromUrl || autoGestor)
    if (autoFromUrl || autoGestor || autoSoloMoraConGestor) {
      consulta.mutate({ ...next, soloMora })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- auto-consulta desde índice reportes / dashboard
  }, [session, searchParams.toString(), puedeElegirGestor])

  const setSoloMora = (checked: boolean) => {
    const next = new URLSearchParams(searchParams)
    if (checked) {
      next.set('soloMora', '1')
    } else {
      next.delete('soloMora')
      next.delete('solo_mora')
    }
    setSearchParams(next, { replace: true })
    const usuarioId = form.getFieldValue('usuarioId') as number | undefined
    if (usuarioId != null && usuarioId > 0) {
      consulta.mutate({
        oficinaId:
          (form.getFieldValue('oficinaId') as number | undefined) ?? session?.oficinaId ?? 0,
        usuarioId,
        soloMora: checked,
      })
    }
  }

  const openExport = (kind: 'csv' | 'pdf') => {
    const oficinaId = (form.getFieldValue('oficinaId') as number | undefined) ?? 0
    if (!oficinaId) {
      message.error('Sin oficina en sesión')
      return
    }
    const usuarioId = form.getFieldValue('usuarioId') as number | undefined
    if (!usuarioId || usuarioId < 1) {
      message.error('Seleccione un gestor')
      return
    }
    const query = toCobroDiarioQuery(oficinaId, usuarioId, soloMora)
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
      soloMorosidad: soloMora,
      caja: cajaQuery.data?.denominacion,
      seleccionados: selectedIds.length,
    })
  }, [consulta.isSuccess, filas, selectedIds.length, cajaQuery.data?.denominacion, soloMora])

  const oficinaWatch = Form.useWatch('oficinaId', form)
  const oficinaEfectiva = oficinaWatch ?? session?.oficinaId ?? 0
  const oficinaLabel =
    oficinaEfectiva === session?.oficinaId && session?.oficinaDenominacion?.trim()
      ? session.oficinaDenominacion.trim()
      : oficinaEfectiva > 0
        ? `Oficina #${oficinaEfectiva}`
        : 'Oficina'
  const pageTitle = soloMora ? 'Cobro diario · solo mora' : 'Cobro diario'
  const panelTitle = soloMora
    ? 'Cartera del día con mora'
    : 'Cartera del día del gestor'

  const cobroStats = useInformeStats(consulta, oficinaEfectiva, cobroExtras)

  const rutaWa = useMutation({
    mutationFn: (ids: number[]) => {
      const v = form.getFieldsValue()
      const usuarioId = v.usuarioId ?? session?.usuarioId
      const oficinaId = v.oficinaId ?? session?.oficinaId
      if (!usuarioId || usuarioId < 1 || !oficinaId) {
        throw new Error('Seleccione un gestor y oficina antes de generar la ruta')
      }
      return generarRutaCobros(ids, { usuarioId, oficinaId })
    },
    onSuccess: (res) => {
      if (!res?.exito) {
        message.error(
          res?.mensaje?.trim() ||
            'No se pudo generar la ruta. Compruebe que los créditos pertenecen a la cartera del gestor.',
        )
        return
      }
      if (!res.paradas?.length) {
        message.error(
          res.mensaje?.trim() ||
            'La API aún no devolvió paradas del mapa (despliegue Azure pendiente). Intente de nuevo en unos minutos.',
        )
        return
      }
      const origen =
        res.latitudOrigen != null &&
        res.longitudOrigen != null &&
        res.latitudOrigen !== 0 &&
        res.longitudOrigen !== 0
          ? { lat: Number(res.latitudOrigen), lng: Number(res.longitudOrigen) }
          : null
      setRutaResult({
        paradas: res.paradas.map((p) => ({
          orden: p.orden,
          creditoId: p.creditoId,
          cliente: p.cliente,
          montoCobrar: p.montoCobrar,
          direccion: p.direccion,
          latitud: p.latitud,
          longitud: p.longitud,
          tieneGps: p.tieneGps,
        })),
        origen,
        urlNavegacionGoogle: res.urlNavegacionGoogle,
        urlWhatsApp: res.urlCortita ? buildApiUrl(res.urlCortita) : null,
      })
      setRutaOpen(true)
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'Error al generar ruta'),
  })

  const onSelectRows = (keys: Key[]) => {
    const ids = [
      ...new Set(
        keys
          .map((k) => Number(k))
          .filter((id) => Number.isFinite(id) && id > 0),
      ),
    ]
    if (ids.length > MAX_RUTA_WA) {
      message.warning(`Máximo ${MAX_RUTA_WA} créditos por ruta`)
      setSelectedIds(ids.slice(0, MAX_RUTA_WA))
      return
    }
    setSelectedIds(ids)
  }

  return (
    <>
    <CredixInformePage
      title={pageTitle}
      subtitle={
        soloMora
          ? 'Misma consulta de cobro diario, limitada a créditos con mora pendiente'
          : 'Qué debe cobrar el gestor hoy (montos, mora, contacto)'
      }
      breadcrumb={reportesCreditoBreadcrumb(pageTitle)}
      stats={cobroStats}
      panelTitle={panelTitle}
      tableResultCount={filas.length}
      enableTableSearch={queried && filas.length > 0}
      searchPlaceholder="Buscar por crédito, cliente, dirección o celular…"
      filters={
        <>
          <Form
            form={form}
            layout="inline"
            initialValues={defaultValues}
            onFinish={(v) => consulta.mutate({ ...v, soloMora })}
          >
            <Form.Item name="oficinaId" label="Oficina">
              <OficinaSelect disabled={!esCentral} size="middle" />
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
            <Form.Item label="Solo mora">
              <Switch checked={soloMora} onChange={setSoloMora} />
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
          <Button
            disabled={selectedIds.length === 0}
            loading={rutaWa.isPending}
            onClick={() => rutaWa.mutate(selectedIds)}
          >
            Ruta del cobrador ({selectedIds.length})
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
          Fecha {formatFecha(dayjs().format('YYYY-MM-DD'))} · {oficinaLabel} ·{' '}
          {gestorLabelFromId(gestorId)}
          {soloMora ? ' · Solo mora' : ''}
          {cajaQuery.data?.denominacion
            ? ` · Caja ${cajaQuery.data.denominacion}`
            : gestorId != null && gestorId > 0
              ? ' · Caja —'
              : ''}
          {selectedIds.length > 0
            ? ` · ${selectedIds.length} seleccionado(s) para ruta`
            : ''}
        </Typography.Text>
      ) : null}

      <CredixDataTable<RptCobroDiarioRow>
        rowKey="creditoId"
        rowSelection={{
          selectedRowKeys: selectedIds,
          onChange: onSelectRows,
          preserveSelectedRowKeys: true,
          getCheckboxProps: (record) => ({
            name: record.cliente ?? String(record.creditoId),
          }),
        }}
        columns={COBRO_DIARIO_COLUMNS}
        dataSource={filas}
        loading={consulta.isPending}
        pagination={{
          pageSize: 25,
          showSizeChanger: true,
          pageSizeOptions: ['10', '25', '50', '100'],
        }}
        locale={{ emptyText: 'Pulse Consultar para cargar resultados.' }}
      />
    </CredixInformePage>
    {rutaOpen ? (
      <Suspense
        fallback={
          <div style={{ display: 'grid', placeItems: 'center', padding: 24 }}>
            <Spin />
          </div>
        }
      >
        <RutaCobradorResultModal
          open={rutaOpen}
          result={rutaResult}
          onClose={() => {
            setRutaOpen(false)
            setRutaResult(null)
          }}
        />
      </Suspense>
    ) : null}
    </>
  )
}
