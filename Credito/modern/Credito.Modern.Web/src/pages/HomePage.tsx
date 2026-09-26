import { Link, useSearchParams } from 'react-router-dom'
import { useMemo } from 'react'
import { branding } from '../config/branding'
import { CredixHubGrid, CredixPage } from '../components/credix'
import { useAuth } from '../auth/useAuth'
import { useAclHubSections } from '../hooks/useAclHubSections'
import { filterCreditoHomeLinks } from '../utils/creditoHubFilter'
import {
  debeMostrarDashboardAdmin,
  debeMostrarDashboardAnalista,
} from '../utils/creditoOperacionPermisos'
import { AdminDashboardPage } from './dashboard/AdminDashboardPage'
import { AnalystDashboardPage } from './dashboard/AnalystDashboardPage'
import '../styles/dashboard-analista.css'

const SECTIONS = [
  {
    title: 'Caja y cobranza',
    links: [
      { to: '/caja/diario', label: 'Caja diario' },
      { to: '/caja/chica', label: 'Caja chica' },
      { to: '/caja/saldos', label: 'Saldos y cierres' },
      { to: '/informes/cobro-diario', label: 'Cobro diario' },
    ],
  },
  {
    title: 'Crédito y clientes',
    links: [
      { to: '/credito/consulta', label: 'Consulta de crédito' },
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

export function HomePage() {
  const { session } = useAuth()
  const [params] = useSearchParams()
  const roles = useMemo(() => session?.roles ?? [], [session?.roles])

  const vista = params.get('vista')
  if (debeMostrarDashboardAnalista(roles, vista)) {
    return <AnalystDashboardPage />
  }
  if (debeMostrarDashboardAdmin(roles, vista)) {
    return <AdminDashboardPage />
  }

  return <HomeHubPage roles={roles} />
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
