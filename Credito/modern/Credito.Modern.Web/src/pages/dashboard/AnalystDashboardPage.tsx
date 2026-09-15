import { Link, useSearchParams } from 'react-router-dom'
import { useMemo, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  AlertOutlined,
  CalendarOutlined,
  ReloadOutlined,
  RiseOutlined,
  TeamOutlined,
  WalletOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import { Alert, Button, Skeleton, Typography } from 'antd'
import { fetchDashboardAnalista, type DashboardRankingRow } from '../../api/dashboard'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CobranzaAreaChart } from '../../components/dashboard/CobranzaAreaChart'
import { CredixPage } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import '../../styles/dashboard-analista.css'

const { Text } = Typography

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
  const query = useQuery({
    queryKey: ['dashboard-analista', session?.usuarioId, session?.oficinaId],
    queryFn: fetchDashboardAnalista,
    staleTime: 60_000,
    enabled: (session?.usuarioId ?? 0) > 0,
  })

  const data = query.data
  const rankingVisible = useMemo(
    () => (data ? rankingConUsuario(data.ranking) : []),
    [data],
  )
  const yo = data?.ranking.find((r) => r.esUsuarioActual)
  const podio = useMemo(() => ordenarPodio(data?.podioMesAnterior ?? []), [data])

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
            accent="#dc2626"
            icon={<WarningOutlined />}
            label="Clientes en mora"
            value={formatEntero(kpis.clientesMora)}
            meta={<span>{kpis.porcentajeMora.toFixed(1)}% de tu cartera</span>}
          />
          <KpiCard
            accent="#d97706"
            icon={<CalendarOutlined />}
            label="Vencen esta semana"
            value={formatEntero(kpis.porVencerSemana)}
            meta={<span>Próximos 7 días</span>}
          />
        </section>

        <div className="dash-grid">
          <section className="dash-panel">
            <div className="dash-panel-head">
              <div>
                <h2>Cobranza de los últimos 30 días</h2>
                <p>Pagos CUO de tus créditos. El legado decía “mensual”; el dato es diario.</p>
              </div>
            </div>
            <div className="dash-panel-body">
              <CobranzaAreaChart puntos={data.productividad} />
            </div>
          </section>

          <section className="dash-panel">
            <div className="dash-panel-head">
              <div>
                <h2>Ranking de analistas</h2>
                <p>Cobranza acumulada del mes en tu oficina</p>
              </div>
              <Text type="secondary">
                Tu posición:{' '}
                {yo ? `#${yo.posicion} de ${data.ranking.length}` : '—'}
              </Text>
            </div>
            <div className="dash-panel-body">
              {rankingVisible.length === 0 ? (
                <p className="dash-empty">No hay analistas activos para comparar.</p>
              ) : (
                <table className="dash-rank-table">
                  <thead>
                    <tr>
                      <th>Pos.</th>
                      <th>Analista</th>
                      <th>Cobrado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rankingVisible.map((row) => (
                      <tr key={row.usuarioId} className={row.esUsuarioActual ? 'is-me' : undefined}>
                        <td>
                          <span className="dash-medal">{medalla(row.posicion)}</span>
                        </td>
                        <td>{row.nombreCompleto}</td>
                        <td>S/ {formatMoney(row.totalCobrado)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </section>
        </div>

        <section className="dash-panel">
          <div className="dash-panel-head">
            <div>
              <h2>Mejores analistas del mes pasado</h2>
              <p>Top 3 por cobranza acumulada</p>
            </div>
          </div>
          <div className="dash-panel-body">
            {podio.length === 0 ? (
              <p className="dash-empty">Aún no hay cobranza del mes anterior para armar el podio.</p>
            ) : (
              <div className="dash-podium">
                {podio.map((item) => (
                  <article key={item.usuarioId} className={`dash-podium-item is-${item.posicion}`}>
                    <div className="dash-podium-place">
                      {medalla(item.posicion)} {puesto(item.posicion)}
                    </div>
                    <div className="dash-podium-name">{item.nombreCompleto}</div>
                    <div className="dash-podium-amt">S/ {formatMoney(item.totalCobrado)}</div>
                  </article>
                ))}
              </div>
            )}
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
    </CredixPage>
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

function rankingConUsuario(rows: DashboardRankingRow[]): DashboardRankingRow[] {
  const top = rows.slice(0, 8)
  const yo = rows.find((r) => r.esUsuarioActual)
  if (yo && !top.some((r) => r.usuarioId === yo.usuarioId)) {
    return [...top, yo]
  }
  return top
}

function ordenarPodio<T extends { posicion: number }>(items: T[]): T[] {
  const byPos = new Map(items.map((i) => [i.posicion, i]))
  return [byPos.get(2), byPos.get(1), byPos.get(3)].filter((x): x is T => x != null)
}

function medalla(posicion: number): string {
  if (posicion === 1) return '1°'
  if (posicion === 2) return '2°'
  if (posicion === 3) return '3°'
  return String(posicion)
}

function puesto(posicion: number): string {
  if (posicion === 1) return 'Primer puesto'
  if (posicion === 2) return 'Segundo puesto'
  if (posicion === 3) return 'Tercer puesto'
  return `Puesto ${posicion}`
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

function saludo(): string {
  const h = new Date().getHours()
  if (h < 12) return 'Buenos días'
  if (h < 19) return 'Buenas tardes'
  return 'Buenas noches'
}

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'No se pudieron obtener los indicadores.'
}
