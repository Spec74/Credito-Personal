import { useMemo, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import {
  CalendarOutlined,
  ReloadOutlined,
  RiseOutlined,
  SwapOutlined,
  TeamOutlined,
  WalletOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import { Alert, Button, Input, Skeleton, Tag, Typography } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import {
  fetchDashboardAdminDetalle,
  fetchDashboardAdminShell,
  type DashboardAdminAnalistaRow,
  type DashboardAdminFlujoRow,
} from '../../api/dashboard'
import { ApiError } from '../../api/errors'
import { DualMetricChart } from '../../components/dashboard/DualMetricChart'
import { CredixDataTable, CredixPage } from '../../components/credix'
import { useAuth } from '../../auth/useAuth'
import { esCreditoAnalista } from '../../utils/creditoOperacionPermisos'
import { formatMoney } from '../../utils/formatMoney'
import '../../styles/dashboard-analista.css'

const { Text } = Typography

const queryOpts = {
  staleTime: 5 * 60_000,
  gcTime: 15 * 60_000,
  placeholderData: keepPreviousData,
} as const

export function AdminDashboardPage() {
  const { session } = useAuth()
  const [buscar, setBuscar] = useState('')
  const oficinaKey = session?.oficinaId
  const enabled = (oficinaKey ?? 0) > 0

  const shellQuery = useQuery({
    queryKey: ['dashboard-admin-shell', oficinaKey],
    queryFn: fetchDashboardAdminShell,
    ...queryOpts,
    enabled,
    retry: 1,
  })

  const detalleQuery = useQuery({
    queryKey: ['dashboard-admin-detalle', oficinaKey],
    queryFn: fetchDashboardAdminDetalle,
    ...queryOpts,
    enabled,
    retry: 1,
  })

  const shell = shellQuery.data
  const r = shell?.resumen
  const detalle = detalleQuery.data

  const analistas = useMemo(() => {
    const q = buscar.trim().toLowerCase()
    const rows = detalle?.analistas ?? []
    if (!q) {
      return rows
    }
    return rows.filter((a) => a.nombreCompleto.toLowerCase().includes(q))
  }, [detalle?.analistas, buscar])

  const entradas = useMemo(
    () => (detalle?.flujoCaja ?? []).filter((x) => x.indEntrada && !x.esTransferencia),
    [detalle?.flujoCaja],
  )
  const salidas = useMemo(
    () => (detalle?.flujoCaja ?? []).filter((x) => !x.indEntrada && !x.esTransferencia),
    [detalle?.flujoCaja],
  )
  const transferencias = useMemo(
    () => (detalle?.flujoCaja ?? []).filter((x) => x.esTransferencia),
    [detalle?.flujoCaja],
  )

  const histPuntos = useMemo(
    () =>
      (detalle?.historico ?? []).map((p) => ({
        fecha: p.fecha,
        etiqueta: p.etiqueta,
        cobrado: p.cobrado,
        desembolsado: p.desembolsado,
      })),
    [detalle?.historico],
  )
  const mesPuntos = useMemo(
    () =>
      (detalle?.historicoMensual ?? []).map((p) => ({
        fecha: p.fechaMes,
        etiqueta: p.etiqueta.replace(/^\w/, (c) => c.toUpperCase()),
        cobrado: p.cobrado,
        desembolsado: p.desembolsado,
      })),
    [detalle?.historicoMensual],
  )

  const columns: ColumnsType<DashboardAdminAnalistaRow> = useMemo(
    () => [
      { title: 'Analista', dataIndex: 'nombreCompleto', ellipsis: true, width: 180 },
      { title: 'Clientes', dataIndex: 'totalClientes', width: 88, align: 'right', render: (v: number) => formatEntero(v) },
      { title: 'Nuevos', dataIndex: 'clientesNuevosMes', width: 80, align: 'right', render: (v: number) => formatEntero(v) },
      {
        title: 'Coloc. hoy',
        dataIndex: 'colocacionesHoy',
        width: 88,
        align: 'right',
        render: (v: number) => formatEntero(v),
      },
      {
        title: 'Coloc. mes',
        dataIndex: 'colocacionesMes',
        width: 88,
        align: 'right',
        render: (v: number) => formatEntero(v),
      },
      {
        title: 'Desemb. hoy',
        dataIndex: 'desembolsoHoy',
        width: 118,
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Desemb. mes',
        dataIndex: 'desembolsoMes',
        width: 118,
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Cobrado hoy',
        dataIndex: 'cobradoHoy',
        width: 118,
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Cobrado mes',
        dataIndex: 'cobradoMes',
        width: 118,
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Mora',
        dataIndex: 'clientesMora',
        width: 100,
        align: 'right',
        render: (_: number, row) => (
          <Tag color={row.porcentajeMora >= 30 ? 'red' : row.clientesMora > 0 ? 'gold' : 'green'}>
            {formatEntero(row.clientesMora)} ({row.porcentajeMora.toFixed(1)}%)
          </Tag>
        ),
      },
      {
        title: 'Monto mora',
        dataIndex: 'montoMora',
        width: 118,
        align: 'right',
        render: (v: number) => `S/ ${formatMoney(v)}`,
      },
      {
        title: 'Tendencia',
        dataIndex: 'variacionCobranzaPct',
        width: 110,
        render: (v: number | null) => <Variacion pct={v} />,
      },
    ],
    [],
  )

  const refetchAll = () => {
    void shellQuery.refetch()
    void detalleQuery.refetch()
  }

  const isFetching = shellQuery.isFetching || detalleQuery.isFetching

  if (!enabled) {
    return (
      <CredixPage title="Inicio" subtitle="Seleccione una oficina para ver el tablero gerencial.">
        <Alert
          type="warning"
          showIcon
          message="Sin oficina en la sesión"
          description="El token no trae oficina activa. Cierre sesión e ingrese de nuevo, o cambie de oficina si su usuario tiene varias."
        />
      </CredixPage>
    )
  }

  if (shellQuery.isLoading && !shell) {
    return (
      <CredixPage title="Inicio" subtitle="Cargando el tablero gerencial…">
        <Skeleton active paragraph={{ rows: 12 }} />
      </CredixPage>
    )
  }

  if (shellQuery.isError || !shell || !r) {
    return (
      <CredixPage
        title="Inicio"
        subtitle="No se pudieron cargar los indicadores de la oficina."
        actions={
          <Button icon={<ReloadOutlined />} onClick={refetchAll}>
            Reintentar
          </Button>
        }
      >
        <Alert type="error" showIcon message={errMsg(shellQuery.error)} />
      </CredixPage>
    )
  }

  const fechaLarga = formatFechaLarga(shell.fechaConsulta)
  const esAnalista = esCreditoAnalista(session?.roles ?? [])
  const detalleReady = !!detalle
  const detalleError = detalleQuery.isError

  return (
    <CredixPage
      title="Inicio"
      subtitle={`Indicadores de cobranza, colocación y cartera · ${shell.nombreOficina}`}
      actions={
        <>
          <Link to="/inicio?vista=modulos">
            <Button>Mapa de módulos</Button>
          </Link>
          {esAnalista ? (
            <Link to="/inicio?vista=analista">
              <Button>Mi tablero</Button>
            </Link>
          ) : null}
          <Button icon={<ReloadOutlined />} onClick={refetchAll} loading={isFetching}>
            Actualizar
          </Button>
        </>
      }
    >
      <div className="dash-analista dash-admin">
        {isFetching && shell ? (
          <p className="dash-refresh-hint" role="status">
            Actualizando indicadores…
          </p>
        ) : null}
        <header className="dash-head">
          <div>
            <p className="dash-kicker">Tablero gerencial</p>
            <h2 className="dash-hello">{shell.nombreOficina}</h2>
            <p className="dash-sub">
              Vista completa de la oficina: operación del día, acumulado del mes, flujo de caja, tendencia y
              rendimiento por analista.
            </p>
          </div>
          <div className="dash-date">{fechaLarga}</div>
        </header>

        <h3 className="dash-section-title">Estado del día</h3>
        <p className="dash-section-hint">Hoy, ayer y anteayer · variación entre los dos últimos días cerrados</p>
        <section className="dash-kpis dash-kpis-4" aria-label="Estado del día">
          <KpiCard
            accent="#059669"
            icon={<WalletOutlined />}
            label="Cobrado hoy"
            value={`S/ ${formatMoney(r.cobradoHoy)}`}
            meta={
              <>
                <Variacion pct={r.variacionCobradoHoyPct} />
                <span>
                  Ayer S/ {formatMoney(r.cobradoAyer)} · Anteayer S/ {formatMoney(r.cobradoAnteayer)}
                </span>
              </>
            }
          />
          <KpiCard
            accent="#2563eb"
            icon={<RiseOutlined />}
            label="Desembolsado hoy"
            value={`S/ ${formatMoney(r.desembolsoHoy)}`}
            meta={
              <>
                <Variacion pct={r.variacionDesembolsoHoyPct} />
                <span>
                  Ayer S/ {formatMoney(r.desembolsoAyer)} · Anteayer S/ {formatMoney(r.desembolsoAnteayer)}
                </span>
              </>
            }
          />
          <KpiCard
            accent="#7c3aed"
            icon={<CalendarOutlined />}
            label="Colocaciones hoy"
            value={formatEntero(r.creditosHoy)}
            meta={
              <>
                <Variacion pct={r.variacionCreditosHoyPct} />
                <span>
                  Ayer {formatEntero(r.creditosAyer)} · Anteayer {formatEntero(r.creditosAnteayer)}
                </span>
              </>
            }
          />
          <KpiCard
            accent="#0f766e"
            icon={<SwapOutlined />}
            label="Flujo neto hoy"
            value={`S/ ${formatMoney(r.flujoNetoHoy)}`}
            meta={
              <>
                <Variacion pct={r.variacionFlujoHoyPct} />
                <span>
                  Ayer S/ {formatMoney(r.flujoNetoAyer)} · Anteayer S/ {formatMoney(r.flujoNetoAnteayer)}
                </span>
              </>
            }
          />
        </section>

        <h3 className="dash-section-title">Acumulado mensual</h3>
        <p className="dash-section-hint">Comparación contra el mismo número de días del mes anterior</p>
        <section className="dash-kpis dash-kpis-4" aria-label="Acumulado mensual">
          <KpiCard
            accent="#059669"
            icon={<WalletOutlined />}
            label="Cobranza acumulada"
            value={`S/ ${formatMoney(r.cobradoMesActual)}`}
            meta={
              <>
                <Variacion pct={r.variacionCobradoMesPct} />
                <span>Período anterior S/ {formatMoney(r.cobradoMesAnteriorComparable)}</span>
              </>
            }
          />
          <KpiCard
            accent="#2563eb"
            icon={<RiseOutlined />}
            label="Desembolsado acumulado"
            value={`S/ ${formatMoney(r.desembolsoMesActual)}`}
            meta={
              <>
                <Variacion pct={r.variacionDesembolsoMesPct} />
                <span>Período anterior S/ {formatMoney(r.desembolsoMesAnteriorComparable)}</span>
              </>
            }
          />
          <KpiCard
            accent="#7c3aed"
            icon={<CalendarOutlined />}
            label="Colocaciones acumuladas"
            value={formatEntero(r.creditosMesActual)}
            meta={
              <>
                <Variacion pct={r.variacionCreditosMesPct} />
                <span>Período anterior {formatEntero(r.creditosMesAnteriorComparable)}</span>
              </>
            }
          />
          <KpiCard
            accent="#0f766e"
            icon={<SwapOutlined />}
            label="Flujo neto acumulado"
            value={`S/ ${formatMoney(r.flujoNetoMesActual)}`}
            meta={
              <>
                <Variacion pct={r.variacionFlujoMesPct} />
                <span>Período anterior S/ {formatMoney(r.flujoNetoMesAnteriorComparable)}</span>
              </>
            }
          />
        </section>

        {detalleError ? (
          <Alert
            type="warning"
            showIcon
            style={{ marginBottom: 16 }}
            message="No se pudo cargar el detalle (flujo, gráficos y analistas)."
            description={errMsg(detalleQuery.error)}
            action={
              <Button size="small" onClick={() => void detalleQuery.refetch()}>
                Reintentar detalle
              </Button>
            }
          />
        ) : null}

        <div className="dash-grid">
          <section className="dash-panel">
            <div className="dash-panel-head">
              <div>
                <h2>Entradas de caja (hoy)</h2>
                <p>Sin transferencias internas</p>
              </div>
              {detalleReady ? <Text type="secondary">S/ {formatMoney(sumaHoy(entradas))}</Text> : null}
            </div>
            <div className="dash-panel-body">
              {detalleReady ? <FlujoLista rows={entradas} /> : <Skeleton active paragraph={{ rows: 4 }} />}
            </div>
          </section>
          <section className="dash-panel">
            <div className="dash-panel-head">
              <div>
                <h2>Salidas de caja (hoy)</h2>
                <p>Sin transferencias internas</p>
              </div>
              {detalleReady ? <Text type="secondary">S/ {formatMoney(sumaHoy(salidas))}</Text> : null}
            </div>
            <div className="dash-panel-body">
              {detalleReady ? <FlujoLista rows={salidas} /> : <Skeleton active paragraph={{ rows: 4 }} />}
            </div>
          </section>
        </div>

        {detalleReady && transferencias.length > 0 ? (
          <p className="dash-section-hint">
            Transferencias hoy: S/{' '}
            {formatMoney(sumaHoy(transferencias.filter((x) => x.indEntrada)))} entrada · S/{' '}
            {formatMoney(sumaHoy(transferencias.filter((x) => !x.indEntrada)))} salida
          </p>
        ) : null}

        <div className="dash-grid">
          <section className="dash-panel">
            <div className="dash-panel-head">
              <div>
                <h2>Últimos 30 días</h2>
                <p>Cobranza y desembolso diarios de la oficina</p>
              </div>
            </div>
            <div className="dash-panel-body">
              {detalleReady ? (
                <DualMetricChart puntos={histPuntos} ariaLabel="Cobranza y desembolso de los últimos 30 días" />
              ) : (
                <Skeleton active paragraph={{ rows: 6 }} />
              )}
            </div>
          </section>
          <section className="dash-panel">
            <div className="dash-panel-head">
              <div>
                <h2>Últimos 12 meses</h2>
                <p>El mes actual está incompleto hasta hoy</p>
              </div>
            </div>
            <div className="dash-panel-body">
              {detalleReady ? (
                <DualMetricChart puntos={mesPuntos} ariaLabel="Cobranza y desembolso de los últimos 12 meses" />
              ) : (
                <Skeleton active paragraph={{ rows: 6 }} />
              )}
            </div>
          </section>
        </div>

        <h3 className="dash-section-title">Situación de cartera</h3>
        <section className="dash-kpis dash-kpis-status" aria-label="Cartera">
          <StatusItem label="Cartera total" value={`S/ ${formatMoney(r.saldoCartera)}`} hint="Saldo pendiente" />
          <StatusItem label="Sin mora" value={`S/ ${formatMoney(r.saldoCreditos)}`} hint="Créditos al día" />
          <StatusItem label="Con mora" value={`S/ ${formatMoney(r.saldoMoraCartera)}`} hint="Créditos con cuota vencida" />
          <StatusItem label="Saldo vencido" value={`S/ ${formatMoney(r.saldoVencido)}`} hint="Vencimiento del crédito" />
          <StatusItem label="Cuotas en atraso" value={`S/ ${formatMoney(r.saldoMorosidad)}`} hint="Importe de cuotas PEN vencidas" />
          <StatusItem
            label="Clientes activos"
            value={formatEntero(r.totalClientes)}
            hint={`${formatEntero(r.clientesMora)} en mora · ${formatEntero(r.creditosPorVencerSemana)} vencen esta semana · ${formatEntero(r.totalAnalistas)} analistas`}
            icon={<TeamOutlined />}
          />
        </section>

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Rendimiento por analista</h2>
              <p>Cobranza, colocaciones y mora de la oficina</p>
            </div>
            <Input
              allowClear
              placeholder="Buscar analista"
              value={buscar}
              onChange={(e) => setBuscar(e.target.value)}
              style={{ width: 220 }}
            />
          </div>
          <div className="dash-panel-body">
            {detalleReady ? (
              <CredixDataTable<DashboardAdminAnalistaRow>
                mode="operacion"
                rowKey="usuarioId"
                pagination={false}
                columns={columns}
                dataSource={analistas}
                locale={{ emptyText: 'No hay analistas activos en esta oficina' }}
                scroll={{ x: 1400 }}
              />
            ) : (
              <Skeleton active paragraph={{ rows: 8 }} />
            )}
          </div>
        </section>

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Atajos operativos</h2>
              <p>Acceso rápido a las pantallas que el gerente usa después de revisar el tablero.</p>
            </div>
          </div>
          <div className="dash-panel-body">
            <nav className="dash-actions" aria-label="Atajos gerenciales">
              <Link to="/informes/morosidad-gestor">
                <Button icon={<WarningOutlined />}>Vencidos</Button>
              </Link>
              <Link to="/informes/cobro-diario">
                <Button>Cobro diario</Button>
              </Link>
              <Link to="/caja/saldos">
                <Button>Saldos y cierres</Button>
              </Link>
              <Link to="/inicio?vista=modulos">
                <Button type="primary">Mapa de módulos</Button>
              </Link>
            </nav>
          </div>
        </section>
      </div>
    </CredixPage>
  )
}

function FlujoLista({ rows }: { rows: DashboardAdminFlujoRow[] }) {
  if (rows.length === 0) {
    return <p className="dash-empty">Sin movimientos hoy.</p>
  }
  return (
    <ul className="dash-flujo-list">
      {rows.map((row) => (
        <li key={`${row.operacion}-${row.indEntrada ? 'e' : 's'}`}>
          <span>{row.concepto}</span>
          <strong>
            {row.cantidadHoy} · S/ {formatMoney(row.importeHoy)}
          </strong>
        </li>
      ))}
    </ul>
  )
}

function StatusItem({
  label,
  value,
  hint,
  icon,
}: {
  label: string
  value: string
  hint: string
  icon?: ReactNode
}) {
  return (
    <article className="dash-status">
      <div className="dash-kpi-label">
        {icon} {label}
      </div>
      <div className="dash-kpi-value">{value}</div>
      <div className="dash-kpi-meta">{hint}</div>
    </article>
  )
}

function KpiCard({
  accent,
  icon,
  label,
  value,
  meta,
}: {
  accent: string
  icon: ReactNode
  label: string
  value: string
  meta: ReactNode
}) {
  return (
    <article className="dash-kpi" style={{ ['--dash-accent' as string]: accent }}>
      <div className="dash-kpi-label">
        {icon} {label}
      </div>
      <div className="dash-kpi-value">{value}</div>
      <div className="dash-kpi-meta">{meta}</div>
    </article>
  )
}

function Variacion({ pct }: { pct: number | null }) {
  if (pct == null) {
    return <span className="dash-var is-new">Nuevo</span>
  }
  if (pct > 0) {
    return <span className="dash-var is-up">▲ {pct.toFixed(1)}%</span>
  }
  if (pct < 0) {
    return <span className="dash-var is-down">▼ {Math.abs(pct).toFixed(1)}%</span>
  }
  return <span className="dash-var">● 0.0%</span>
}

function sumaHoy(rows: DashboardAdminFlujoRow[]): number {
  return rows.reduce((acc, r) => acc + r.importeHoy, 0)
}

function formatEntero(value: number): string {
  return value.toLocaleString('es-PE')
}

function formatFechaLarga(iso: string): string {
  const d = iso.slice(0, 10)
  const [y, m, day] = d.split('-').map(Number)
  if (!y || !m || !day) {
    return iso
  }
  return new Date(y, m - 1, day).toLocaleDateString('es-PE', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'No se pudieron obtener los indicadores gerenciales.'
}
