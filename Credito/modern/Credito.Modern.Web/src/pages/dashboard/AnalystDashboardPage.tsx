import { useState, type ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  AlertOutlined,
  CalendarOutlined,
  ReloadOutlined,
  RiseOutlined,
  StopOutlined,
  TeamOutlined,
  WalletOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import { Alert, Button, Skeleton } from 'antd'
import {
  fetchDashboardAnalista,
  type DashboardMoraTipo,
} from '../../api/dashboard'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CobranzaAreaChart } from '../../components/dashboard/CobranzaAreaChart'
import { DashboardClientesMoraModal } from '../../components/dashboard/DashboardClientesMoraModal'
import { CredixPage } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import '../../styles/dashboard-analista.css'

const ACCIONES = [
  { to: '/informes/morosidad-gestor', label: 'Vencidos' },
  { to: '/informes/creditos-observados', label: 'Observados' },
  { to: '/informes/clientes-inactivos', label: 'Clientes inactivos' },
  { to: '/credito/simulador', label: 'Simulador' },
] as const

export function AnalystDashboardPage() {
  const { session } = useAuth()
  const [params] = useSearchParams()
  const desdeAdmin = params.get('vista') === 'analista'
  const [moraOpen, setMoraOpen] = useState(false)
  const [moraTipo, setMoraTipo] = useState<DashboardMoraTipo>('TODOS')

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

  return (
    <CredixPage
      title="Inicio"
      subtitle="Resumen de tus indicadores al cierre del día operativo."
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
          <Button icon={<ReloadOutlined />} onClick={() => void query.refetch()} loading={query.isFetching}>
            Actualizar
          </Button>
        </>
      }
    >
      <div className="dash-analista">
        <header className="dash-head">
          <div>
            <p className="dash-kicker">Tablero del analista</p>
            <h2 className="dash-hello">
              {saludo()}, {data.nombreAnalista}
            </h2>
            <p className="dash-sub">Cartera, cobranza y colocación de tu oficina, solo de tus créditos.</p>
          </div>
          <div className="dash-date">{fechaLarga}</div>
        </header>

        <section className="dash-kpis" aria-label="Indicadores principales">
          <KpiCard
            accent="#0ea5e9"
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
            accent="#2563eb"
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
            accent="#059669"
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
            accent="#7c3aed"
            icon={<WalletOutlined />}
            label="Cartera vigente"
            value={`S/ ${formatMoney(kpis.saldoActual)}`}
            meta={<span>Saldo de créditos desembolsados a tu cargo</span>}
          />
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
            accent="#dc2626"
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
            accent="#d97706"
            icon={<CalendarOutlined />}
            label="Vencen esta semana"
            value={formatEntero(kpis.porVencerSemana)}
            meta={<span>Próximos 7 días</span>}
          />
        </section>

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Cobranza de los últimos 30 días</h2>
              <p>Pagos CUO de tus créditos. El legado decía “mensual”; el dato es diario.</p>
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
            <CobranzaAreaChart puntos={data.productividad} />
          </div>
        </section>

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Resumen y acciones prioritarias</h2>
              <p>Recomendaciones generadas según tus indicadores</p>
            </div>
          </div>
          <div className="dash-panel-body">
            <div className="dash-insights">
              {data.insights.map((insight) => (
                <article key={insight.titulo} className={`dash-insight is-${insight.tipo}`}>
                  <AlertOutlined aria-hidden />
                  <div>
                    <h3>{insight.titulo}</h3>
                    <p>{insight.mensaje}</p>
                    {insight.accion ? (
                      <Link to={insight.accion}>Ir a la gestión →</Link>
                    ) : insight.titulo === 'Cartera en mora' ? (
                      <button type="button" className="dash-insight-link" onClick={() => abrirMora('TODOS')}>
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
              <p>El menú lateral cubre el resto de módulos. Estos atajos salen de tus indicadores.</p>
            </div>
          </div>
          <div className="dash-panel-body">
            <nav className="dash-actions" aria-label="Seguimiento">
              {ACCIONES.map((a) => (
                <Link key={a.to} to={a.to}>
                  <Button>{a.label}</Button>
                </Link>
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

function KpiCard({
  accent,
  icon,
  label,
  value,
  meta,
  onActivate,
  actionHint,
}: {
  accent: string
  icon: ReactNode
  label: string
  value: string
  meta: ReactNode
  onActivate?: () => void
  actionHint?: string
}) {
  const interactive = Boolean(onActivate)
  const body = (
    <>
      <div className="dash-kpi-label">
        {icon} {label}
      </div>
      <div className="dash-kpi-value">{value}</div>
      <div className="dash-kpi-meta">{meta}</div>
      {interactive && actionHint ? <div className="dash-kpi-action">{actionHint}</div> : null}
    </>
  )

  if (!interactive) {
    return (
      <article className="dash-kpi" style={{ ['--dash-accent' as string]: accent }}>
        {body}
      </article>
    )
  }

  return (
    <button
      type="button"
      className="dash-kpi is-action"
      style={{ ['--dash-accent' as string]: accent }}
      onClick={onActivate}
      aria-label={actionHint ?? label}
    >
      {body}
    </button>
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
