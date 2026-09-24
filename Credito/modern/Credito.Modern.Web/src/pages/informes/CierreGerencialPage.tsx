import { useDeferredValue, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Empty,
  Form,
  Input,
  InputNumber,
  Select,
  Skeleton,
  Space,
  Tabs,
  Tag,
  Tooltip,
  message,
} from 'antd'
import {
  CheckCircleOutlined,
  DownloadOutlined,
  ReloadOutlined,
  SaveOutlined,
  SearchOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import {
  downloadCierreGerencialExcel,
  fetchCierreGerencialAvance,
  fetchCierreGerencialMetas,
  fetchCierreGerencialPermisos,
  guardarCierreGerencialMetas,
  type AvanceMetaGerencialRow,
  type MetaGerencialRow,
} from '../../api/cierreGerencial'
import { ApiError } from '../../api/errors'
import {
  CredixDataTable,
  CredixDatePicker,
  CredixFilterBar,
  CredixPage,
  CredixPanel,
} from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'

type TipoFiltro = '' | 'PRODUCTIVA' | 'ESPECIAL'
type TabKey = 'avance' | 'metas'

function periodoIso(d: Dayjs): string {
  return d.startOf('month').format('YYYY-MM-DD')
}

function estadoTone(estado?: string): 'success' | 'warning' | 'error' | 'default' {
  const e = (estado ?? '').toUpperCase()
  if (e.includes('CUMPL') || e.includes('OK') || e.includes('VERDE')) return 'success'
  if (e.includes('ALERT') || e.includes('AMAR') || e.includes('RIESGO') || e.includes('PARCIAL')) {
    return 'warning'
  }
  if (e.includes('NO') || e.includes('ROJO') || e.includes('CRIT') || e.includes('INCUMPL')) {
    return 'error'
  }
  return 'default'
}

function pctClass(pct?: number | null): string {
  if (pct == null) return 'cierre-gerencial-pct'
  if (pct >= 100) return 'cierre-gerencial-pct cierre-gerencial-pct--ok'
  if (pct >= 80) return 'cierre-gerencial-pct cierre-gerencial-pct--warn'
  return 'cierre-gerencial-pct cierre-gerencial-pct--bad'
}

function esEspecial(tipo?: string): boolean {
  return (tipo ?? '').toUpperCase() === 'ESPECIAL'
}

function coincideFiltro(
  row: {
    tipoCartera: string
    nombreCompleto?: string
    nombreUsuario?: string
    mercado?: string
    supervisor?: string
  },
  tipo: TipoFiltro,
  buscar: string,
): boolean {
  if (tipo && row.tipoCartera?.toUpperCase() !== tipo) return false
  const q = buscar.trim().toUpperCase()
  if (!q) return true
  const haystack = [row.nombreCompleto, row.nombreUsuario, row.mercado, row.supervisor]
    .filter(Boolean)
    .join(' ')
    .toUpperCase()
  return haystack.includes(q)
}

function moneyCell(v: number | null | undefined): string {
  return v == null ? '—' : `S/ ${formatMoney(v)}`
}

function MetaInput(props: {
  editable: boolean
  noAplica?: boolean
  value: number | null
  integer?: boolean
  onChange: (v: number | null) => void
}) {
  if (props.noAplica) {
    return <span className="cierre-gerencial-no-aplica">NO APLICA</span>
  }
  return (
    <InputNumber
      className="cierre-gerencial-meta-input"
      min={0}
      step={props.integer ? 1 : 0.01}
      precision={props.integer ? 0 : 2}
      disabled={!props.editable}
      value={props.value ?? undefined}
      onChange={(v) => props.onChange(v == null ? null : Number(v))}
      controls={false}
    />
  )
}

function TipoBadge({ tipo }: { tipo?: string }) {
  const especial = esEspecial(tipo)
  return (
    <span
      className={`cierre-gerencial-tipo ${
        especial ? 'cierre-gerencial-tipo--especial' : 'cierre-gerencial-tipo--productiva'
      }`}
    >
      {especial ? 'Especial' : 'Productiva'}
    </span>
  )
}

function EstadoTag({ estado }: { estado?: string }) {
  const label = (estado ?? '').trim() || '—'
  return <Tag color={estadoTone(estado)}>{label}</Tag>
}

export function CierreGerencialPage() {
  const queryClient = useQueryClient()
  const [periodo, setPeriodo] = useState<Dayjs>(() => dayjs().startOf('month'))
  const periodoKey = periodoIso(periodo)
  const [tipoFiltro, setTipoFiltro] = useState<TipoFiltro>('')
  const [buscar, setBuscar] = useState('')
  const buscarDeferred = useDeferredValue(buscar)
  const [tab, setTab] = useState<TabKey>('avance')
  const [metasEdit, setMetasEdit] = useState<MetaGerencialRow[]>([])

  const permisos = useQuery({
    queryKey: ['cierre-gerencial-permisos'],
    queryFn: fetchCierreGerencialPermisos,
  })

  const avance = useQuery({
    queryKey: ['cierre-gerencial-avance', periodoKey],
    queryFn: () => fetchCierreGerencialAvance(periodoKey),
    enabled: permisos.data?.puedeConsultar === true,
  })

  const metas = useQuery({
    queryKey: ['cierre-gerencial-metas', periodoKey],
    queryFn: () => fetchCierreGerencialMetas(periodoKey),
    enabled: permisos.data?.puedeConsultar === true,
  })

  useEffect(() => {
    if (metas.data?.metas) {
      setMetasEdit(metas.data.metas.map((m) => ({ ...m })))
    }
  }, [metas.data])

  const excel = useMutation({
    mutationFn: () => downloadCierreGerencialExcel(periodoKey),
    onSuccess: () => message.success('Excel descargado'),
    onError: (e) => message.error(e instanceof ApiError ? e.message : 'No se pudo exportar'),
  })

  const guardar = useMutation({
    mutationFn: () =>
      guardarCierreGerencialMetas({
        periodo: periodoKey,
        metas: metasEdit.map((m) => ({
          usuarioId: m.usuarioId,
          tipoCartera: m.tipoCartera,
          metaCapitalCierre: esEspecial(m.tipoCartera) ? null : m.metaCapitalCierre,
          metaClientesActivosCierre: esEspecial(m.tipoCartera) ? null : m.metaClientesActivosCierre,
          metaVencidosMaximoCierre: m.metaVencidosMaximoCierre,
          metaRecuperacionVencidosMes: m.metaRecuperacionVencidosMes,
        })),
      }),
    onSuccess: async (r) => {
      message.success(r.message)
      await queryClient.invalidateQueries({ queryKey: ['cierre-gerencial-metas', periodoKey] })
      await queryClient.invalidateQueries({ queryKey: ['cierre-gerencial-avance', periodoKey] })
    },
    onError: (e) => message.error(e instanceof ApiError ? e.message : 'No se pudo guardar'),
  })

  const avanceFiltrado = useMemo(
    () => (avance.data?.filas ?? []).filter((f) => coincideFiltro(f, tipoFiltro, buscarDeferred)),
    [avance.data, tipoFiltro, buscarDeferred],
  )

  const metasFiltradas = useMemo(
    () => metasEdit.filter((m) => coincideFiltro(m, tipoFiltro, buscarDeferred)),
    [metasEdit, tipoFiltro, buscarDeferred],
  )

  const kpis = useMemo(() => {
    const filas = avanceFiltrado
    const total = filas.length
    const metasOk = filas.filter((f) => f.metaConfigurada).length
    const productivas = filas.filter((f) => !esEspecial(f.tipoCartera)).length
    return {
      capital: filas.reduce((s, f) => s + (f.capitalActual || 0), 0),
      clientes: filas.reduce((s, f) => s + (f.clientesActivosActual || 0), 0),
      vencidos: filas.reduce((s, f) => s + (f.vencidosActual || 0), 0),
      metasOk,
      total,
      productivas,
      especiales: total - productivas,
      metasPct: total > 0 ? Math.round((metasOk / total) * 100) : 0,
    }
  }, [avanceFiltrado])

  const puedeEditarMetas = metas.data?.puedeEditar === true
  const filtroActivo = Boolean(tipoFiltro || buscarDeferred.trim())

  const patchMeta = (
    usuarioId: number,
    tipoCartera: string,
    patch: Partial<MetaGerencialRow>,
  ) => {
    setMetasEdit((prev) => {
      const idx = prev.findIndex((m) => m.usuarioId === usuarioId && m.tipoCartera === tipoCartera)
      if (idx < 0) return prev
      const next = [...prev]
      next[idx] = { ...next[idx], ...patch }
      return next
    })
  }

  const avanceColumns: ColumnsType<AvanceMetaGerencialRow> = useMemo(
    () => [
      { title: '#', dataIndex: 'orden', width: 48, align: 'center', fixed: 'left' },
      {
        title: 'Analista',
        key: 'analista',
        ellipsis: true,
        width: 210,
        fixed: 'left',
        render: (_, r) => (
          <div className="cierre-gerencial-analista">
            <span className="cierre-gerencial-analista-name">
              {r.nombreCompleto || r.nombreUsuario}
            </span>
            <span className="cierre-gerencial-analista-meta">
              {[r.supervisor, r.mercado].filter(Boolean).join(' · ') || 'Sin supervisor / mercado'}
            </span>
          </div>
        ),
      },
      {
        title: 'Cartera',
        dataIndex: 'tipoCartera',
        width: 108,
        render: (v: string) => <TipoBadge tipo={v} />,
      },
      {
        title: 'Capital base',
        dataIndex: 'capitalBase',
        align: 'right',
        width: 118,
        render: moneyCell,
      },
      {
        title: 'Meta capital',
        dataIndex: 'metaCapitalCierre',
        align: 'right',
        width: 118,
        render: moneyCell,
      },
      {
        title: 'Capital actual',
        dataIndex: 'capitalActual',
        align: 'right',
        width: 118,
        render: (v: number) => moneyCell(v),
      },
      {
        title: '% capital',
        dataIndex: 'cumplimientoCapitalPct',
        width: 88,
        align: 'right',
        render: (v: number | null) =>
          v == null ? '—' : <span className={pctClass(v)}>{v.toFixed(1)}%</span>,
      },
      {
        title: 'Estado capital',
        dataIndex: 'estadoCapital',
        width: 118,
        render: (v: string) => <EstadoTag estado={v} />,
      },
      {
        title: 'Clientes base',
        dataIndex: 'clientesBase',
        width: 96,
        align: 'right',
        render: (v: number | null) => (v == null ? '—' : v),
      },
      {
        title: 'Clientes',
        dataIndex: 'clientesActivosActual',
        width: 84,
        align: 'right',
      },
      {
        title: 'Estado clientes',
        dataIndex: 'estadoClientes',
        width: 118,
        render: (v: string) => <EstadoTag estado={v} />,
      },
      {
        title: 'Base vencidos',
        dataIndex: 'vencidosBaseComparable',
        align: 'right',
        width: 118,
        render: moneyCell,
      },
      {
        title: 'Límite vencidos',
        dataIndex: 'metaVencidosMaximoCierre',
        align: 'right',
        width: 118,
        render: moneyCell,
      },
      {
        title: 'Vencidos',
        dataIndex: 'vencidosActual',
        align: 'right',
        width: 110,
        render: (v: number) => moneyCell(v),
      },
      {
        title: 'Estado vencidos',
        dataIndex: 'estadoVencidos',
        width: 120,
        render: (v: string) => <EstadoTag estado={v} />,
      },
      {
        title: 'Recuperación',
        dataIndex: 'recuperacionVencidosActual',
        align: 'right',
        width: 118,
        render: moneyCell,
      },
      {
        title: 'Estado recuperación',
        dataIndex: 'estadoRecuperacion',
        width: 128,
        render: (v: string) => <EstadoTag estado={v} />,
      },
    ],
    [],
  )

  const metasColumns: ColumnsType<MetaGerencialRow> = useMemo(
    () => [
      { title: '#', dataIndex: 'orden', width: 48, align: 'center', fixed: 'left' },
      {
        title: 'Analista',
        dataIndex: 'nombreCompleto',
        ellipsis: true,
        width: 190,
        fixed: 'left',
        render: (v: string, r) => (
          <div className="cierre-gerencial-analista">
            <span className="cierre-gerencial-analista-name">{v || r.nombreUsuario}</span>
            <span className="cierre-gerencial-analista-meta">{r.nombreUsuario}</span>
          </div>
        ),
      },
      {
        title: 'Cartera',
        dataIndex: 'tipoCartera',
        width: 108,
        render: (v: string) => <TipoBadge tipo={v} />,
      },
      {
        title: 'Base capital',
        dataIndex: 'capitalBase',
        align: 'right',
        width: 118,
        render: (v: number) => moneyCell(v),
      },
      {
        title: 'Meta capital',
        key: 'metaCapital',
        width: 128,
        render: (_, row) => (
          <MetaInput
            editable={puedeEditarMetas}
            noAplica={esEspecial(row.tipoCartera)}
            value={row.metaCapitalCierre}
            onChange={(v) => patchMeta(row.usuarioId, row.tipoCartera, { metaCapitalCierre: v })}
          />
        ),
      },
      {
        title: 'Base clientes',
        dataIndex: 'clientesActivosBase',
        width: 100,
        align: 'right',
      },
      {
        title: 'Meta clientes',
        key: 'metaClientes',
        width: 120,
        render: (_, row) => (
          <MetaInput
            editable={puedeEditarMetas}
            noAplica={esEspecial(row.tipoCartera)}
            integer
            value={row.metaClientesActivosCierre}
            onChange={(v) =>
              patchMeta(row.usuarioId, row.tipoCartera, { metaClientesActivosCierre: v })
            }
          />
        ),
      },
      {
        title: 'Base vencidos',
        dataIndex: 'vencidosBaseComparable',
        align: 'right',
        width: 118,
        render: moneyCell,
      },
      {
        title: 'Límite vencidos',
        key: 'metaVenc',
        width: 128,
        render: (_, row) => (
          <MetaInput
            editable={puedeEditarMetas}
            value={row.metaVencidosMaximoCierre}
            onChange={(v) =>
              patchMeta(row.usuarioId, row.tipoCartera, { metaVencidosMaximoCierre: v })
            }
          />
        ),
      },
      {
        title: 'Meta recuperación',
        key: 'metaRec',
        width: 128,
        render: (_, row) => (
          <MetaInput
            editable={puedeEditarMetas}
            value={row.metaRecuperacionVencidosMes}
            onChange={(v) =>
              patchMeta(row.usuarioId, row.tipoCartera, { metaRecuperacionVencidosMes: v })
            }
          />
        ),
      },
      {
        title: 'Configuración',
        dataIndex: 'configurada',
        width: 124,
        align: 'center',
        render: (v: boolean) => (
          <Tag color={v ? 'success' : 'warning'}>{v ? 'CONFIGURADA' : 'PENDIENTE'}</Tag>
        ),
      },
    ],
    [puedeEditarMetas],
  )

  const breadcrumb = [
    { title: <Link to="/inicio">Inicio</Link> },
    { title: <Link to="/informes">Informes</Link> },
    { title: 'Cierre gerencial' },
  ]

  if (permisos.isLoading) {
    return (
      <CredixPage
        className="cierre-gerencial-page"
        title="Cierre y metas gerenciales"
        breadcrumb={breadcrumb}
      >
        <div className="cierre-gerencial-skeleton">
          <Skeleton active paragraph={{ rows: 2 }} />
          <Skeleton active paragraph={{ rows: 4 }} />
        </div>
      </CredixPage>
    )
  }

  if (permisos.isError || permisos.data?.puedeConsultar === false) {
    return (
      <CredixPage
        className="cierre-gerencial-page"
        title="Cierre y metas gerenciales"
        breadcrumb={breadcrumb}
      >
        <Alert
          type="warning"
          showIcon
          message="Sin permiso de consulta"
          description={
            permisos.error instanceof ApiError
              ? permisos.error.message
              : 'Su usuario no está autorizado para consultar el módulo gerencial.'
          }
        />
      </CredixPage>
    )
  }

  const oficial = avance.data ? !avance.data.avanceNoOficial : null

  return (
    <CredixPage
      className="cierre-gerencial-page"
      title="Cierre y metas gerenciales"
      subtitle="Seguimiento mensual de capital, clientes, vencidos y recuperación por analista. El cierre oficial se consolida tras el cierre de bóveda (fin de mes o días 1–2)."
      breadcrumb={breadcrumb}
      actions={
        <Space wrap>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => {
              void avance.refetch()
              void metas.refetch()
            }}
          >
            Actualizar
          </Button>
          <Button
            type="primary"
            icon={<DownloadOutlined />}
            loading={excel.isPending}
            onClick={() => void excel.mutateAsync()}
          >
            Exportar Excel
          </Button>
          {puedeEditarMetas ? (
            <Tooltip title="Guarda las metas definitivas del periodo visible">
              <Button
                icon={<SaveOutlined />}
                loading={guardar.isPending}
                onClick={() => {
                  setTab('metas')
                  void guardar.mutateAsync()
                }}
              >
                Guardar metas
              </Button>
            </Tooltip>
          ) : null}
        </Space>
      }
    >
      <div className="cierre-gerencial-toolbar">
        <CredixFilterBar className="credix-informe-filter-bar">
          <Form layout="inline" className="credix-informe-filter-row">
            <Form.Item label="Periodo">
              <CredixDatePicker
                picker="month"
                value={periodo}
                format="MMMM YYYY"
                allowClear={false}
                onChange={(v) => {
                  if (v) setPeriodo(v.startOf('month'))
                }}
              />
            </Form.Item>
            <Form.Item label="Cartera">
              <Select
                style={{ width: 150 }}
                value={tipoFiltro}
                onChange={(v) => setTipoFiltro(v as TipoFiltro)}
                options={[
                  { value: '', label: 'Todas' },
                  { value: 'PRODUCTIVA', label: 'Productiva' },
                  { value: 'ESPECIAL', label: 'Especial' },
                ]}
              />
            </Form.Item>
            <Form.Item label="Buscar">
              <Input
                allowClear
                prefix={<SearchOutlined />}
                placeholder="Analista, mercado, supervisor…"
                style={{ width: 220 }}
                value={buscar}
                onChange={(e) => setBuscar(e.target.value)}
                aria-label="Filtrar analistas"
              />
            </Form.Item>
          </Form>
        </CredixFilterBar>

        {avance.data ? (
          <div
            className={`cierre-gerencial-status ${
              oficial ? 'cierre-gerencial-status--oficial' : 'cierre-gerencial-status--no-oficial'
            }`}
            role="status"
          >
            <span className="cierre-gerencial-status-badge">
              {oficial ? (
                <>
                  <CheckCircleOutlined aria-hidden /> Cierre oficial
                </>
              ) : (
                <>
                  <WarningOutlined aria-hidden /> Avance no oficial
                </>
              )}
            </span>
            <div className="cierre-gerencial-status-copy">
              <strong>
                {periodo.format('MMMM YYYY')}
                {avance.data.fechaCalculo ? ` · calculado ${avance.data.fechaCalculo}` : ''}
              </strong>
              <span>
                {oficial
                  ? 'Las cifras corresponden al cierre mensual consolidado.'
                  : 'Las cifras pueden variar hasta que se genere el cierre mensual (post-cierre de bóveda).'}
                {filtroActivo ? ' · Filtro activo sobre los indicadores y tablas.' : ''}
              </span>
            </div>
          </div>
        ) : null}

        <div className="cierre-gerencial-kpis" aria-live="polite">
          <div className="cierre-gerencial-kpi">
            <div className="cierre-gerencial-kpi-label">Saldo total</div>
            <div className="cierre-gerencial-kpi-value">S/ {formatMoney(kpis.capital)}</div>
            <div className="cierre-gerencial-kpi-note">
              {kpis.productivas} productiva{kpis.productivas === 1 ? '' : 's'} · {kpis.especiales}{' '}
              especial{kpis.especiales === 1 ? '' : 'es'}
            </div>
          </div>
          <div className="cierre-gerencial-kpi">
            <div className="cierre-gerencial-kpi-label">Clientes activos</div>
            <div className="cierre-gerencial-kpi-value">{kpis.clientes.toLocaleString('es-PE')}</div>
            <div className="cierre-gerencial-kpi-note">Personas distintas en cartera visible</div>
          </div>
          <div className="cierre-gerencial-kpi">
            <div className="cierre-gerencial-kpi-label">Vencidos actuales</div>
            <div className="cierre-gerencial-kpi-value">S/ {formatMoney(kpis.vencidos)}</div>
            <div className="cierre-gerencial-kpi-note">Saldo residual de cuotas vencidas</div>
          </div>
          <div className="cierre-gerencial-kpi">
            <div className="cierre-gerencial-kpi-label">Metas configuradas</div>
            <div className="cierre-gerencial-kpi-value">
              {kpis.metasOk}
              {kpis.total > 0 ? ` / ${kpis.total}` : ''}
            </div>
            <div className="cierre-gerencial-kpi-note">
              {kpis.total > 0 ? `${kpis.metasPct}% del universo filtrado` : 'Sin carteras en el periodo'}
            </div>
          </div>
        </div>
      </div>

      <Tabs
        className="cierre-gerencial-tabs"
        activeKey={tab}
        onChange={(k) => setTab(k as TabKey)}
        items={[
          {
            key: 'avance',
            label: `Avance (${avanceFiltrado.length}${
              filtroActivo ? ` de ${avance.data?.total ?? 0}` : ''
            })`,
            children: (
              <CredixPanel title="Cumplimiento por analista">
                <p className="cierre-gerencial-panel-hint">
                  Vista operativa del avance. El Excel exporta el detalle completo (42 columnas) con la
                  misma lógica de negocio.
                </p>
                {avance.isError ? (
                  <Alert
                    type="error"
                    showIcon
                    message="No se pudo cargar el avance"
                    description={
                      avance.error instanceof ApiError ? avance.error.message : 'Error inesperado'
                    }
                  />
                ) : (
                  <CredixDataTable
                    className="cierre-gerencial-table"
                    rowKey={(r) => `${r.usuarioId}-${r.tipoCartera}`}
                    loading={avance.isLoading}
                    columns={avanceColumns}
                    dataSource={avanceFiltrado}
                    pagination={{
                      pageSize: 25,
                      showSizeChanger: true,
                      showTotal: (t) => `${t} cartera${t === 1 ? '' : 's'}`,
                    }}
                    size="middle"
                    scroll={{ x: 1780 }}
                    locale={{
                      emptyText: (
                        <Empty
                          className="cierre-gerencial-empty"
                          description="No hay avance para el periodo o filtro seleccionado"
                        />
                      ),
                    }}
                    rowClassName={(r) =>
                      esEspecial(r.tipoCartera)
                        ? 'cierre-gerencial-row--especial'
                        : 'cierre-gerencial-row--productiva'
                    }
                  />
                )}
              </CredixPanel>
            ),
          },
          {
            key: 'metas',
            label: `Metas definitivas (${metasFiltradas.length})`,
            children: (
              <CredixPanel
                title="Metas del periodo"
                extra={
                  metas.data?.periodoCerrado ? (
                    <Tag>Periodo cerrado</Tag>
                  ) : puedeEditarMetas ? (
                    <Tag color="processing">
                      Editable hasta {metas.data?.fechaLimiteEdicion || 'la fecha límite'}
                    </Tag>
                  ) : (
                    <Tag>Solo lectura</Tag>
                  )
                }
              >
                <p className="cierre-gerencial-panel-hint">
                  En cartera especial, capital y clientes no aplican. Vencidos y recuperación sí se
                  configuran.
                </p>
                {metas.isError ? (
                  <Alert
                    type="error"
                    showIcon
                    message="No se pudieron cargar las metas"
                    description={
                      metas.error instanceof ApiError ? metas.error.message : 'Error inesperado'
                    }
                  />
                ) : (
                  <CredixDataTable
                    className="cierre-gerencial-table"
                    rowKey={(r) => `${r.usuarioId}-${r.tipoCartera}`}
                    loading={metas.isLoading}
                    columns={metasColumns}
                    dataSource={metasFiltradas}
                    pagination={false}
                    size="middle"
                    scroll={{ x: 1480 }}
                    locale={{
                      emptyText: (
                        <Empty
                          className="cierre-gerencial-empty"
                          description="No hay metas para el periodo o filtro seleccionado"
                        />
                      ),
                    }}
                    rowClassName={(r) =>
                      esEspecial(r.tipoCartera)
                        ? 'cierre-gerencial-row--especial'
                        : 'cierre-gerencial-row--productiva'
                    }
                  />
                )}
              </CredixPanel>
            ),
          },
        ]}
      />
    </CredixPage>
  )
}
