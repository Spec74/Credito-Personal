import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Alert, Button, Form, InputNumber, Spin, Tag, Tooltip, Typography, message } from 'antd'
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table'
import { FileExcelOutlined, SearchOutlined } from '@ant-design/icons'
import {
  downloadCobranzaPagosExcel,
  fetchCobranzaPagos,
} from '../../api/cobranzaPagos'
import type { CobranzaPagosRow } from '../../api/cobranzaPagos'
import { fetchOficinas } from '../../api/oficinas'
import { fetchUsuariosReporteGestores } from '../../api/usuariosAdmin'
import { useAuth } from '../../auth/useAuth'
import { ApiError } from '../../api/errors'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import {
  CobranzaDecisionStrip,
  CobranzaMixBar,
  CobranzaSegmentChips,
} from '../../components/reportes/cobranza/CobranzaDecisionStrip'
import { CobranzaKpiStrip } from '../../components/reportes/cobranza/CobranzaKpiStrip'
import { CobranzaPagosDetalle } from '../../components/reportes/cobranza/CobranzaPagosDetalle'
import {
  buildCobranzaInsights,
  filterCobranzaSegmento,
  sortCobranzaPrioridad,
  type CobranzaSegmento,
} from '../../components/reportes/cobranza/cobranzaPagosInsights'
import {
  GestorSelect,
  OficinaSelect,
} from '../../components/reportes/ReporteFiltrosMaestros'
import { useEsCentral } from '../../hooks/useEsCentral'
import { usePuedeElegirGestorInforme } from '../../hooks/usePuedeElegirGestorInforme'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { reportesCobranzaBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { TipoPagoCell } from '../tesoreria/components/TipoPagoBrand'

/** 25 filas por página: equilibrio entre lectura y scroll (106 clientes ≈ 5 páginas). Use 10 si expande muchas filas. */
const DEFAULT_PAGE_SIZE = 25

type CobranzaFiltrosForm = {
  oficinaId?: number
  usuarioId?: number
}

function rowKey(r: CobranzaPagosRow) {
  return `${r.nro}-${r.cliente}`
}

function toApiFiltros(values: CobranzaFiltrosForm) {
  return {
    oficinaId: values.oficinaId && values.oficinaId > 0 ? values.oficinaId : undefined,
    usuarioId: values.usuarioId && values.usuarioId > 0 ? values.usuarioId : undefined,
  }
}

export function CobranzaPagosPage() {
  const { session } = useAuth()
  const puedeElegirGestor = usePuedeElegirGestorInforme()
  const { esCentral } = useEsCentral()
  const puedeElegirSede = puedeElegirGestor && esCentral
  const [form] = Form.useForm<CobranzaFiltrosForm>()
  const [expandedKeys, setExpandedKeys] = useState<string[]>([])
  const [segmento, setSegmento] = useState<CobranzaSegmento>('todos')
  const autoLoaded = useRef(false)

  const gestores = useQuery({
    queryKey: ['usuarios-gestores-reporte', 'legacy'],
    queryFn: fetchUsuariosReporteGestores,
  })

  const oficinas = useQuery({
    queryKey: ['oficinas', 'cobranza'],
    queryFn: fetchOficinas,
  })

  useEffect(() => {
    if (!session) return
    form.setFieldsValue({
      oficinaId: session.oficinaId,
      usuarioId: puedeElegirGestor ? form.getFieldValue('usuarioId') : session.usuarioId,
    })
  }, [session, form, puedeElegirGestor])

  const consulta = useMutation({
    mutationFn: (params: { usuarioId?: number; oficinaId?: number }) =>
      fetchCobranzaPagos(params),
    onSuccess: (data) => {
      setExpandedKeys([])
      setSegmento('todos')
      message.success(`${data.resumen.totalClientes} cliente(s) cargados`)
    },
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'No se pudo generar el reporte'),
  })

  /** Gestor sin combo: carga sola su cartera (valor inmediato al abrir el módulo). */
  useEffect(() => {
    if (!session || puedeElegirGestor || autoLoaded.current) return
    if ((session.usuarioId ?? 0) <= 0) return
    autoLoaded.current = true
    consulta.mutate({
      oficinaId: session.oficinaId,
      usuarioId: session.usuarioId,
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps -- solo al montar sesión de gestor
  }, [session, puedeElegirGestor])

  const excel = useMutation({
    mutationFn: downloadCobranzaPagosExcel,
    onSuccess: () => message.success('Excel descargado'),
    onError: (e) =>
      message.error(e instanceof ApiError ? e.message : 'No se pudo exportar a Excel'),
  })

  const resumen = consulta.data?.resumen
  const filtrosAplicados = consulta.data?.filtrosAplicados
  const filas = consulta.data?.data ?? []
  const queried = consulta.isSuccess || consulta.isError

  const insights = useMemo(
    () =>
      resumen
        ? buildCobranzaInsights(filas, resumen)
        : null,
    [filas, resumen],
  )

  const filasVista = useMemo(() => {
    const filtered = filterCobranzaSegmento(filas, segmento)
    return segmento === 'todos' || segmento === 'sin_saldo'
      ? filtered
      : sortCobranzaPrioridad(filtered)
  }, [filas, segmento])

  const gestorAplicadoLabel = useMemo(() => {
    const uid = filtrosAplicados?.usuarioId
    if (uid == null || uid < 1) return 'Todos los gestores'
    const g = gestores.data?.find((u) => u.usuarioId === uid)
    return g?.nombreCompleto || g?.nombreUsuario || `Gestor #${uid}`
  }, [filtrosAplicados?.usuarioId, gestores.data])

  const oficinaAplicadaLabel = useMemo(() => {
    const oid = filtrosAplicados?.oficinaId
    if (oid == null || oid < 1) return 'Todas las oficinas'
    const o = oficinas.data?.find((x) => x.oficinaId === oid)
    return o?.denominacion ?? `Oficina #${oid}`
  }, [filtrosAplicados?.oficinaId, oficinas.data])

  const runConsulta = useCallback(async () => {
    const values = await form.validateFields()
    consulta.mutate(toApiFiltros(values))
  }, [consulta, form])

  const handleExcel = async () => {
    const values = await form.validateFields()
    excel.mutate(toApiFiltros(values))
  }

  const columns: ColumnsType<CobranzaPagosRow> = [
    { title: 'Nro', dataIndex: 'nro', width: 48, align: 'center' },
    {
      title: 'Cliente',
      dataIndex: 'cliente',
      ellipsis: true,
      render: (v) => <span className="credix-cobranza-cliente">{v}</span>,
    },
    {
      title: 'Tipo',
      dataIndex: 'formaPago',
      width: 140,
      render: (v: string) => <TipoPagoCell tipoPago={v} />,
    },
    {
      title: 'Crédito',
      dataIndex: 'montoCredito',
      width: 92,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Interés',
      dataIndex: 'interes',
      width: 80,
      align: 'right',
      render: formatMoney,
    },
    {
      title: 'Total',
      dataIndex: 'montoTotal',
      width: 92,
      align: 'right',
      render: formatMoney,
    },
    {
      title: '1.er pago',
      dataIndex: 'fechaPrimerPago',
      width: 108,
      align: 'center',
      render: (v) => <span className="credix-cobranza-date">{formatFecha(v)}</span>,
    },
    {
      title: 'Vcto.',
      dataIndex: 'fechaVencimiento',
      width: 108,
      align: 'center',
      render: (v) => <span className="credix-cobranza-date">{formatFecha(v)}</span>,
    },
    {
      title: 'Riesgo',
      key: 'riesgo',
      width: 108,
      align: 'center',
      render: (_, row) => {
        const mora = row.diasAtrazoMora ?? 0
        if (mora >= 30) {
          return <Tag className="credix-cobranza-tag credix-cobranza-tag--mora">{mora}d</Tag>
        }
        if (mora > 0) {
          return <Tag className="credix-cobranza-tag credix-cobranza-tag--warn">{mora}d</Tag>
        }
        if ((row.saldo ?? 0) <= 0.009) {
          return <Tag className="credix-cobranza-tag">Liquidado</Tag>
        }
        return <Tag className="credix-cobranza-tag credix-cobranza-tag--ok">Al día</Tag>
      },
    },
    {
      title: 'Cuotas',
      key: 'cuotas',
      width: 148,
      render: (_, row) => {
        const total = (row.pagosCount ?? 0) + (row.impagosCount ?? 0)
        const pend = row.impagosCount ?? 0
        return (
          <div className="credix-cobranza-cuotas-cell" title="Expandir fila para ver cada cuota">
            <span className="credix-cobranza-cuotas-line">
              <strong>
                {row.pagosCount ?? 0}/{total || '—'}
              </strong>{' '}
              pag.
              {pend > 0 ? <span className="credix-cobranza-cuotas-pend"> · {pend} pend.</span> : null}
            </span>
          </div>
        )
      },
    },
    {
      title: 'Pagado',
      dataIndex: 'totalPago',
      width: 90,
      align: 'right',
      render: (v) => <span className="credix-cobranza-pagado">{formatMoney(v)}</span>,
    },
    {
      title: 'Saldo',
      dataIndex: 'saldo',
      width: 90,
      align: 'right',
      render: (v) => <span className="credix-cobranza-saldo">{formatMoney(v)}</span>,
    },
  ]

  const tablePagination: TablePaginationConfig = {
    defaultPageSize: DEFAULT_PAGE_SIZE,
    pageSizeOptions: ['10', '25', '50', '100'],
    showSizeChanger: true,
    position: ['topRight', 'bottomRight'],
    showTotal: (total: number, range: [number, number]) =>
      `${range[0]}–${range[1]} de ${total} clientes`,
  }

  return (
    <CredixInformePage
      title="Cobranza pagos por gestor"
      subtitle="Cartera, recuperación y mora — priorice visitas con datos accionables"
      breadcrumb={reportesCobranzaBreadcrumb()}
      enableTableSearch={queried && filasVista.length > 0}
      tableSearchVariant="compact"
      panelSearchFirst
      searchPlaceholder="Buscar cliente o nro…"
      panelTitle={
        segmento === 'todos'
          ? 'Clientes del gestor'
          : `Clientes · ${filasVista.length} de ${filas.length}`
      }
      panelActions={
        <Tooltip title="Descarga .xlsx con título, colores y detalle de cuotas">
          <Button
            icon={<FileExcelOutlined />}
            loading={excel.isPending}
            className="credix-report-btn credix-report-btn--xls"
            onClick={() => void handleExcel()}
          >
            Excel
          </Button>
        </Tooltip>
      }
      filters={
        <div className="credix-cobranza-filtros-card">
          <Form
            form={form}
            layout="vertical"
            className="credix-cobranza-filtros-form"
            initialValues={{
              oficinaId: session?.oficinaId,
              usuarioId: puedeElegirGestor ? undefined : session?.usuarioId,
            }}
            onFinish={() => void runConsulta()}
            onValuesChange={(changed) => {
              if (!('oficinaId' in changed) && !('usuarioId' in changed)) return
              const gestorId = form.getFieldValue('usuarioId')
              if (gestorId != null && gestorId > 0 && !consulta.isPending) {
                void runConsulta()
              }
            }}
          >
            <div className="credix-cobranza-filtros-fields">
              {puedeElegirSede ? (
                <Form.Item label="Oficina" name="oficinaId" className="credix-cobranza-field">
                  <OficinaSelect allowAll size="middle" />
                </Form.Item>
              ) : (
                <Form.Item name="oficinaId" hidden>
                  <InputNumber />
                </Form.Item>
              )}
              {puedeElegirGestor ? (
                <Form.Item label="Gestor" name="usuarioId" className="credix-cobranza-field">
                  <GestorSelect
                    allowAll
                    legacyList
                    size="middle"
                    onEnter={() => void runConsulta()}
                    onGestorResolved={() => void runConsulta()}
                  />
                </Form.Item>
              ) : (
                <Form.Item name="usuarioId" hidden>
                  <InputNumber />
                </Form.Item>
              )}
            </div>
            <div className="credix-cobranza-filtros-actions">
              <Button
                type="primary"
                htmlType="submit"
                icon={<SearchOutlined />}
                loading={consulta.isPending}
                size="middle"
              >
                Generar reporte
              </Button>
            </div>
          </Form>
        </div>
      }
      error={
        consulta.isError ? (
          <Alert
            type="error"
            showIcon
            message="Error al cargar cobranza"
            description={
              consulta.error instanceof ApiError
                ? consulta.error.message
                : 'Revise filtros y conexión con la API.'
            }
          />
        ) : null
      }
    >
      <div className="credix-cobranza-dashboard">
        {!queried && !consulta.isPending ? (
          <Alert
            type="info"
            showIcon
            className="credix-cobranza-hint"
            message={
              puedeElegirGestor
                ? 'Elija gestor (y sede si es Central) y genere el reporte'
                : 'Cargando su cartera…'
            }
          />
        ) : null}

        {consulta.data ? (
          <div className="credix-cobranza-context">
            <Tag className="credix-cobranza-context-tag">{gestorAplicadoLabel}</Tag>
            <Tag className="credix-cobranza-context-tag credix-cobranza-context-tag--muted">
              {oficinaAplicadaLabel}
            </Tag>
          </div>
        ) : null}

        {resumen && insights ? (
          <>
            <CobranzaKpiStrip resumen={resumen} insights={insights} />
            <CobranzaMixBar insights={insights} />
            <CobranzaDecisionStrip decisions={insights.decisions} />
            <CobranzaSegmentChips
              insights={insights}
              total={filas.length}
              value={segmento}
              onChange={(s) => {
                setSegmento(s)
                setExpandedKeys([])
              }}
            />
          </>
        ) : null}

        {filasVista.length > 0 ? (
          <Typography.Text type="secondary" className="credix-cobranza-table-tip">
            {segmento !== 'todos'
              ? 'Lista priorizada por días de mora y saldo. '
              : null}
            Use <strong>+</strong> para ver cuotas (una fila a la vez).
          </Typography.Text>
        ) : null}

        {consulta.isSuccess && filas.length === 0 ? (
          <Alert
            type="warning"
            showIcon
            message="Sin clientes para estos filtros"
            description="Cambie de gestor u oficina, o use «Todos» y Generar reporte."
          />
        ) : null}

        {consulta.isSuccess && filas.length > 0 && filasVista.length === 0 ? (
          <Alert
            type="info"
            showIcon
            message="Ningún cliente en este segmento"
            description="Elija otro filtro rápido (Todos / En mora / Al día)."
          />
        ) : null}

        <Spin spinning={consulta.isPending}>
          {filasVista.length > 0 ? (
            <CredixDataTable<CobranzaPagosRow>
              className="credix-cobranza-table"
              rowKey={rowKey}
              columns={columns}
              dataSource={filasVista}
              pagination={tablePagination}
              scroll={{ x: 1140 }}
              rowClassName={(row) => {
                const mora = row.diasAtrazoMora ?? 0
                if (mora >= 30) return 'credix-cobranza-row is-mora-grave'
                if (mora > 0) return 'credix-cobranza-row is-mora'
                return 'credix-cobranza-row'
              }}
              expandable={{
                expandedRowKeys: expandedKeys,
                onExpandedRowsChange: (keys) => {
                  const next = keys.map(String)
                  setExpandedKeys(next.length > 1 ? [next[next.length - 1]!] : next)
                },
                expandedRowRender: (row) => (
                  <CobranzaPagosDetalle
                    pagosLista={row.pagosLista ?? []}
                    diasAtrazoMora={row.diasAtrazoMora}
                    cliente={row.cliente}
                  />
                ),
                rowExpandable: () => true,
                columnWidth: 44,
              }}
            />
          ) : null}
        </Spin>
      </div>
    </CredixInformePage>
  )
}
