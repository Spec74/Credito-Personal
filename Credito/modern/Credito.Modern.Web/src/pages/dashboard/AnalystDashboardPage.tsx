import { useMemo, useState, type ReactNode } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  AlertOutlined,
  CalendarOutlined,
  EnvironmentOutlined,
  FileSearchOutlined,
  PercentageOutlined,
  ReloadOutlined,
  RiseOutlined,
  StopOutlined,
  TeamOutlined,
  UserDeleteOutlined,
  WalletOutlined,
  WarningOutlined,
  CalculatorOutlined,
} from '@ant-design/icons'
import { Alert, Button, Progress, Skeleton } from 'antd'
import {
  fetchDashboardAnalista,
  type DashboardMoraTipo,
  type DashboardProductividadPunto,
} from '../../api/dashboard'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CobranzaAreaChart } from '../../components/dashboard/CobranzaAreaChart'
import { DashboardClientesMoraModal } from '../../components/dashboard/DashboardClientesMoraModal'
import { CredixPage } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import {
  buildAnalistaCarteraActions,
  runDashboardCarteraAction,
} from './dashboardCarteraActions'
import '../../styles/dashboard-analista.css'

const ACCION_ICONS: Record<string, ReactNode> = {
  'cobro-diario': <EnvironmentOutlined />,
  vencidos: <WarningOutlined />,
  observados: <FileSearchOutlined />,
  inactivos: <UserDeleteOutlined />,
  simulador: <CalculatorOutlined />,
  'caja-diario': <WalletOutlined />,
}

export function AnalystDashboardPage() {
  const { session } = useAuth()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const desdeAdmin = params.get('vista') === 'analista'
  const [moraOpen, setMoraOpen] = useState(false)
  const [moraTipo, setMoraTipo] = useState<DashboardMoraTipo>('TODOS')

  const roles = session?.roles ?? []
  const carteraActions = useMemo(() => buildAnalistaCarteraActions(roles), [roles])

  const query = useQuery({
    queryKey: ['dashboard-analista', session?.usuarioId, session?.oficinaId],
    queryFn: fetchDashboardAnalista,
    staleTime: 60_000,
    enabled: (session?.usuarioId ?? 0) > 0,
  })

  const data = query.data

  const abrirMora = (tipo: DashboardMoraTipo) => {
    setMoraTipo(tipo)
    setMoraOpen(true)
  }

  const onCarteraAction = (action: (typeof carteraActions)[number]) => {
    runDashboardCarteraAction(action, {
      oficinaId: session?.oficinaId ?? 0,
      usuarioId: session?.usuarioId ?? 0,
      navigate,
    })
  }

  const prodStats = useMemo(
    () => resumenProductividad(data?.productividad ?? []),
    [data?.productividad],
  )

  if (query.isLoading) {
    return (
      <CredixPage title="Inicio" subtitle="Cargando tus indicadores…">
        <Skeleton active paragraph={{ rows: 10 }} />
      </CredixPage>
    )
  }

  if (query.isError || !data) {
    return (
      <CredixPage
        title="Inicio"
        subtitle="No se pudieron cargar tus indicadores."
        actions={
          <Button icon={<ReloadOutlined />} onClick={() => void query.refetch()}>
            Reintentar
          </Button>
        }
      >
        <Alert type="error" showIcon message={errMsg(query.error)} />
      </CredixPage>
    )
  }

  const { kpis } = data
  const fechaLarga = formatFechaLarga(data.fechaConsulta)
  const vsAyer = resumenVsAyer(kpis.cobradoHoy, kpis.cobradoAyer)
  const progresoHoy =
    kpis.cobradoAyer > 0
      ? Math.min(100, Math.round((kpis.cobradoHoy / kpis.cobradoAyer) * 100))
      : kpis.cobradoHoy > 0
        ? 100
        : 0

  return (
    <CredixPage
      title="Inicio"
      subtitle="Seguimiento de tu cobranza, cartera y prioridades del día."
      actions={
        <>
          {desdeAdmin ? (
            <>
              <Link to="/inicio">
                <Button>Tablero gerencial</Button>
              </Link>
              <Link to="/inicio?vista=modulos">
                <Button>Mapa de módulos</Button>
              </Link>
            </>
          ) : null}
          <Button
            icon={<ReloadOutlined />}
            onClick={() => void query.refetch()}
            loading={query.isFetching}
          >
            Actualizar
          </Button>
        </>
      }
    >
      <div className="dash-analista">
        <header className="dash-head">
          <div>
            <p className="dash-kicker">Tablero del gestor</p>
            <h2 className="dash-hello">
              {saludo()}, {data.nombreAnalista}
            </h2>
            <p className="dash-sub">
              Indicadores de tus créditos en esta oficina — paridad del dashboard
              legado, con seguimiento y acciones priorizadas.
            </p>
          </div>
          <div className="dash-date">{fechaLarga}</div>
        </header>

        <h3 className="dash-section-title">Operación del mes</h3>
        <section className="dash-kpis dash-kpis-4" aria-label="Operación del mes">
          <KpiCard
            accent="#2e69ae"
            icon={<TeamOutlined />}
            label="Total clientes"
            value={formatEntero(kpis.totalClientes)}
            meta={
              <>
                <span>Nuevos este mes: {formatEntero(kpis.clientesNuevosActual)}</span>
                <Variacion pct={kpis.variacionClientesNuevosPct} />
              </>
            }
          />
          <KpiCard
            accent="#114885"
            icon={<RiseOutlined />}
            label="Créditos colocados"
            value={formatEntero(kpis.creditosActual)}
            meta={
              <>
                <span>{formatEntero(kpis.creditosAnterior)} el mes pasado</span>
                <Variacion pct={kpis.variacionCreditosPct} />
              </>
            }
          />
          <KpiCard
            accent="#15803d"
            icon={<WalletOutlined />}
            label="Cobrado del mes"
            value={`S/ ${formatMoney(kpis.cobradoActual)}`}
            meta={
              <>
                <span>S/ {formatMoney(kpis.cobradoAnterior)} el mes pasado</span>
                <Variacion pct={kpis.variacionCobradoPct} />
              </>
            }
          />
          <KpiCard
            accent="#0f4c81"
            icon={<PercentageOutlined />}
            label="Cartera vigente"
            value={`S/ ${formatMoney(kpis.saldoActual)}`}
            meta={<span>Saldo pendiente de créditos desembolsados a tu cargo</span>}
          />
        </section>

        <h3 className="dash-section-title">Riesgo de cartera</h3>
        <section className="dash-kpis dash-kpis-4" aria-label="Riesgo de cartera">
          <KpiCard
            accent="#b91c1c"
            icon={<WarningOutlined />}
            label="Saldo en mora"
            value={`S/ ${formatMoney(kpis.montoMora)}`}
            meta={<span>Saldo pendiente de créditos con cuotas vencidas</span>}
            onActivate={() => abrirMora('TODOS')}
            actionHint="Ver clientes morosos"
          />
          <KpiCard
            accent="#9f1239"
            icon={<TeamOutlined />}
            label="Clientes en mora"
            value={formatEntero(kpis.clientesMora)}
            meta={
              <span>
                {kpis.porcentajeMora.toFixed(1)}% de tu cartera
                {kpis.clientesMoraPagandoConAtraso > 0
                  ? ` · ${formatEntero(kpis.clientesMoraPagandoConAtraso)} pagan con atraso`
                  : ''}
              </span>
            }
            onActivate={() => abrirMora('TODOS')}
            actionHint="Ver clientes morosos"
          />
          <KpiCard
            accent="#7f1d1d"
            icon={<StopOutlined />}
            label="Morosos sin pagar"
            value={formatEntero(kpis.clientesMoraSinPago)}
            meta={
              <span>
                {formatEntero(kpis.clientesMoraNuncaPagaron)} nunca pagaron ·{' '}
                {formatEntero(kpis.clientesMoraDejaronPagar)} dejaron de pagar
              </span>
            }
            onActivate={() => abrirMora('SIN_PAGO')}
            actionHint="Ver clientes sin pago"
          />
          <KpiCard
            accent="#114885"
            icon={<CalendarOutlined />}
            label="Vencen esta semana"
            value={formatEntero(kpis.porVencerSemana)}
            meta={<span>Próximos 7 días — seguimiento preventivo</span>}
            href="/informes/morosidad-gestor"
            actionHint="Abrir vencidos"
          />
        </section>

        {kpis.clientesMora > 0 ? (
          <section className="dash-mora-strip" aria-label="Desglose de mora">
            <div className="dash-mora-strip__head">
              <strong>Desglose de mora</strong>
              <span>Filtros rápidos del listado (valor agregado sobre el legado)</span>
            </div>
            <div className="dash-mora-strip__chips">
              <MoraChip
                label="Todos"
                count={kpis.clientesMora}
                onClick={() => abrirMora('TODOS')}
              />
              <MoraChip
                label="Nunca pagaron"
                count={kpis.clientesMoraNuncaPagaron}
                onClick={() => abrirMora('NUNCA_PAGO')}
              />
              <MoraChip
                label="Dejaron de pagar"
                count={kpis.clientesMoraDejaronPagar}
                onClick={() => abrirMora('DEJO_PAGAR')}
              />
              <MoraChip
                label="Pagan con atraso"
                count={kpis.clientesMoraPagandoConAtraso}
                onClick={() => abrirMora('PAGA_CON_ATRASO')}
              />
            </div>
          </section>
        ) : null}

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Mi productividad diaria</h2>
              <p>Cobranza CUO de tus créditos · últimos 30 días</p>
            </div>
            <div className="dash-daily-summary" aria-live="polite">
              <div>
                <span>Hoy</span>
                <strong>S/ {formatMoney(kpis.cobradoHoy)}</strong>
              </div>
              <div>
                <span>Ayer</span>
                <strong>S/ {formatMoney(kpis.cobradoAyer)}</strong>
              </div>
              <div className={`dash-daily-goal is-${vsAyer.tone}`}>{vsAyer.text}</div>
            </div>
          </div>
          <div className="dash-panel-body">
            <div className="dash-prod-progress">
              <div className="dash-prod-progress__label">
                <span>Avance vs cobranza de ayer</span>
                <strong>{progresoHoy}%</strong>
              </div>
              <Progress
                percent={progresoHoy}
                showInfo={false}
                strokeColor="#114885"
                trailColor="#e8f2fc"
                size="small"
              />
            </div>
            <CobranzaAreaChart puntos={data.productividad} />
            <div className="dash-prod-stats" aria-label="Resumen de productividad">
              <div>
                <span>Total 30 días</span>
                <strong>S/ {formatMoney(prodStats.total)}</strong>
              </div>
              <div>
                <span>Promedio diario</span>
                <strong>S/ {formatMoney(prodStats.promedio)}</strong>
              </div>
              <div>
                <span>Mejor día</span>
                <strong>
                  {prodStats.mejorEtiqueta
                    ? `${prodStats.mejorEtiqueta} · S/ ${formatMoney(prodStats.mejor)}`
                    : '—'}
                </strong>
              </div>
              <div>
                <span>Días con cobro</span>
                <strong>
                  {prodStats.diasConCobro} / {prodStats.dias}
                </strong>
              </div>
            </div>
          </div>
        </section>

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Resumen y acciones prioritarias</h2>
              <p>Recomendaciones generadas según tus indicadores</p>
            </div>
            <span className="dash-priorities-badge">
              <AlertOutlined aria-hidden /> Prioridades
            </span>
          </div>
          <div className="dash-panel-body">
            <div className="dash-insights">
              {data.insights.map((insight) => (
                <article
                  key={`${insight.titulo}-${insight.tipo}`}
                  className={`dash-insight is-${insight.tipo}`}
                >
                  <AlertOutlined aria-hidden />
                  <div>
                    <h3>{insight.titulo}</h3>
                    <p>{insight.mensaje}</p>
                    {insight.accion ? (
                      <Link to={insight.accion}>Ir a la gestión →</Link>
                    ) : insight.titulo === 'Cartera en mora' ? (
                      <button
                        type="button"
                        className="dash-insight-link"
                        onClick={() => abrirMora('TODOS')}
                      >
                        Ver clientes morosos →
                      </button>
                    ) : null}
                  </div>
                </article>
              ))}
            </div>
          </div>
        </section>

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Seguimiento de cartera</h2>
              <p>
                Atajos operativos según el legado (vencidos, observados, inactivos) más
                cobro y caja del día.
              </p>
            </div>
          </div>
          <div className="dash-panel-body">
            <nav className="dash-actions" aria-label="Seguimiento">
              {carteraActions.map((a) => (
                <Button
                  key={a.id}
                  icon={ACCION_ICONS[a.id]}
                  onClick={() => onCarteraAction(a)}
                >
                  {a.label}
                </Button>
              ))}
            </nav>
          </div>
        </section>
      </div>

      <DashboardClientesMoraModal
        open={moraOpen}
        tipoInicial={moraTipo}
        onClose={() => setMoraOpen(false)}
      />
    </CredixPage>
  )
}

function MoraChip({
  label,
  count,
  onClick,
}: {
  label: string
  count: number
  onClick: () => void
}) {
  return (
    <button type="button" className="dash-mora-chip" onClick={onClick}>
      <span>{label}</span>
      <strong>{formatEntero(count)}</strong>
    </button>
  )
}

function KpiCard({
  accent,
  icon,
  label,
  value,
  meta,
  onActivate,
  href,
  actionHint,
}: {
  accent: string
  icon: ReactNode
  label: string
  value: string
  meta: ReactNode
  onActivate?: () => void
  href?: string
  actionHint?: string
}) {
  const body = (
    <>
      <div className="dash-kpi-label">
        {icon} {label}
      </div>
      <div className="dash-kpi-value">{value}</div>
      <div className="dash-kpi-meta">{meta}</div>
      {actionHint ? <div className="dash-kpi-action">{actionHint}</div> : null}
    </>
  )

  const style = { ['--dash-accent' as string]: accent }

  if (href) {
    return (
      <Link
        to={href}
        className="dash-kpi is-action"
        style={style}
        aria-label={actionHint ?? label}
      >
        {body}
      </Link>
    )
  }

  if (onActivate) {
    return (
      <button
        type="button"
        className="dash-kpi is-action"
        style={style}
        onClick={onActivate}
        aria-label={actionHint ?? label}
      >
        {body}
      </button>
    )
  }

  return (
    <article className="dash-kpi" style={style}>
      {body}
    </article>
  )
}

function Variacion({ pct }: { pct: number | null }) {
  if (pct === null) {
    return <span className="dash-var is-new">Sin base comparable</span>
  }
  if (Math.abs(pct) < 0.05) {
    return <span className="dash-var">Estable</span>
  }
  const up = pct > 0
  return (
    <span className={`dash-var ${up ? 'is-up' : 'is-down'}`}>
      {up ? '▲' : '▼'} {Math.abs(pct).toFixed(1)}%
    </span>
  )
}

function resumenProductividad(puntos: DashboardProductividadPunto[]) {
  if (puntos.length === 0) {
    return {
      total: 0,
      promedio: 0,
      mejor: 0,
      mejorEtiqueta: '',
      diasConCobro: 0,
      dias: 0,
    }
  }
  let total = 0
  let mejor = 0
  let mejorEtiqueta = ''
  let diasConCobro = 0
  for (const p of puntos) {
    total += p.montoCobrado
    if (p.montoCobrado > 0) {
      diasConCobro += 1
    }
    if (p.montoCobrado > mejor) {
      mejor = p.montoCobrado
      mejorEtiqueta = p.etiqueta
    }
  }
  return {
    total,
    promedio: total / puntos.length,
    mejor,
    mejorEtiqueta,
    diasConCobro,
    dias: puntos.length,
  }
}

function resumenVsAyer(hoy: number, ayer: number): { text: string; tone: string } {
  if (ayer <= 0 && hoy <= 0) {
    return { text: 'Sin cobranza hoy ni ayer', tone: 'muted' }
  }
  if (ayer <= 0) {
    return { text: 'Primera cobranza del periodo comparable', tone: 'up' }
  }
  const pct = ((hoy - ayer) / Math.abs(ayer)) * 100
  if (Math.abs(pct) < 0.5) {
    return { text: 'Similar a ayer', tone: 'muted' }
  }
  if (pct > 0) {
    return { text: `+${pct.toFixed(0)}% vs ayer`, tone: 'up' }
  }
  return { text: `${pct.toFixed(0)}% vs ayer`, tone: 'down' }
}

function formatEntero(n: number) {
  return new Intl.NumberFormat('es-PE').format(n)
}

function formatFechaLarga(iso: string) {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleDateString('es-PE', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

function saludo() {
  const h = new Date().getHours()
  if (h < 12) return 'Buenos días'
  if (h < 19) return 'Buenas tardes'
  return 'Buenas noches'
}

function errMsg(error: unknown) {
  if (error instanceof ApiError) return error.message
  if (error instanceof Error) return error.message
  return 'Error desconocido'
}
