import { Link, useSearchParams } from 'react-router-dom'
import { Suspense, lazy, useMemo } from 'react'
import { Skeleton } from 'antd'
import { branding } from '../config/branding'
import { CredixHubGrid, CredixPage } from '../components/credix'
import { RoleHomePage, RoleHomeStrip } from './RoleHomePage'
import { useAuth } from '../auth/useAuth'
import { useAclHubSections } from '../hooks/useAclHubSections'
import { filterCreditoHomeLinks } from '../utils/creditoHubFilter'
import {
  debeMostrarDashboardAdmin,
  debeMostrarDashboardAnalista,
} from '../utils/creditoOperacionPermisos'
import '../styles/dashboard-analista.css'

/** Tableros en chunks propios: no bloquean el parse del hub ni entre sí. */
const AdminDashboardPage = lazy(() =>
  import('./dashboard/AdminDashboardPage').then((m) => ({
    default: m.AdminDashboardPage,
  })),
)
const AnalystDashboardPage = lazy(() =>
  import('./dashboard/AnalystDashboardPage').then((m) => ({
    default: m.AnalystDashboardPage,
  })),
)
const RedSucursalesPage = lazy(() =>
  import('./tesoreria/RedSucursalesPage').then((m) => ({
    default: m.RedSucursalesPage,
  })),
)

const SECTIONS = [
  {
    title: 'Caja y cobranza',
    links: [
      { to: '/caja/diario', label: 'Caja diario' },
      { to: '/caja/saldos', label: 'Saldos y cierres' },
      { to: '/informes/cobro-diario', label: 'Cobro diario' },
    ],
  },
  {
    title: 'Crédito y clientes',
    links: [
      { to: '/credito/consulta', label: 'Consulta de crédito' },
      { to: '/credito/nuevo', label: 'Nueva solicitud' },
      { to: '/credito/simulador', label: 'Simulador' },
      { to: '/credito/aprobar', label: 'Aprobar créditos' },
      { to: '/clientes', label: 'Clientes' },
    ],
  },
  {
    title: 'Ventas, informes y administración',
    links: [
      { to: '/ventas/venta-rapida', label: 'Venta rápida' },
      { to: '/informes', label: 'Informes' },
      { to: '/maestros', label: 'Maestros' },
      { to: '/admin', label: 'Administración' },
    ],
  },
]

/** Cabecera estática del LCP — no depende del API (el subtítulo es copy fijo). */
function DashboardBootFallback({
  kicker,
  title,
  subtitle,
}: {
  kicker: string
  title: string
  subtitle: string
}) {
  return (
    <CredixPage title="Inicio" subtitle="Cargando indicadores…">
      <div className="dash-analista">
        <header className="dash-head">
          <div>
            <p className="dash-kicker">{kicker}</p>
            <h2 className="dash-hello">{title}</h2>
            {subtitle ? <p className="dash-sub">{subtitle}</p> : null}
          </div>
        </header>
        <Skeleton active paragraph={{ rows: 8 }} />
      </div>
    </CredixPage>
  )
}

export function HomePage() {
  const { session } = useAuth()
  const [params] = useSearchParams()
  const roles = useMemo(() => session?.roles ?? [], [session?.roles])

  const vista = params.get('vista')
  if (vista === 'modulos') {
    return <HomeHubPage roles={roles} />
  }
  if (vista === 'red') {
    return (
      <Suspense
        fallback={
          <DashboardBootFallback
            kicker="Red"
            title="Cargando…"
            subtitle="Sucursales de la red"
          />
        }
      >
        <RedSucursalesPage />
      </Suspense>
    )
  }
  if (debeMostrarDashboardAnalista(roles, vista)) {
    return (
      <>
        <RoleHomeStrip roles={roles} />
        <Suspense
          fallback={
            <DashboardBootFallback
              kicker="Tablero del gestor"
              title="Cargando…"
              subtitle=""
            />
          }
        >
          <AnalystDashboardPage />
        </Suspense>
      </>
    )
  }
  if (debeMostrarDashboardAdmin(roles, vista)) {
    return (
      <>
        <RoleHomeStrip roles={roles} />
        <Suspense
          fallback={
            <DashboardBootFallback
              kicker="Tablero gerencial"
              title="Cargando…"
              subtitle=""
            />
          }
        >
          <AdminDashboardPage />
        </Suspense>
      </>
    )
  }

  return <RoleHomePage roles={roles} />
}

function HomeHubPage({ roles }: { roles: string[] }) {
  const roleSections = useMemo(
    () =>
      SECTIONS.map((section) => {
        let links = section.links
        if (section.title === 'Crédito y clientes') {
          links = filterCreditoHomeLinks(links, roles)
        }
        return { ...section, links }
      }).filter((section) => section.links.length > 0),
    [roles],
  )

  const sections = useAclHubSections(roleSections)

  return (
    <CredixPage
      title={branding.appShortName}
      breadcrumb={[{ title: <Link to="/inicio">Inicio</Link> }]}
    >
      <CredixHubGrid sections={sections} variant="module" />
    </CredixPage>
  )
}
