import { Link, useSearchParams } from 'react-router-dom'
import { useMemo } from 'react'
import { Alert, Button } from 'antd'
import { branding } from '../config/branding'
import { CredixHubGrid, CredixHubIntro, CredixPage } from '../components/credix'
import { useModuleHubStats } from '../hooks/useModuleHubStats'
import { useAuth } from '../auth/useAuth'
import { filterCreditoHomeLinks } from '../utils/creditoHubFilter'
import {
  debeMostrarDashboardAnalista,
  esCreditoAdministrador,
  esCreditoAnalista,
} from '../utils/creditoOperacionPermisos'
import { AnalystDashboardPage } from './dashboard/AnalystDashboardPage'
import '../styles/dashboard-analista.css'

const SECTIONS = [
  {
    title: 'Caja y cobranza',
    links: [
      { to: '/caja/diario', label: 'Caja diario', description: 'Cobros, arqueo y cierre del día' },
      { to: '/caja/chica', label: 'Caja chica', description: 'Gastos, rendiciones y comprobantes' },
      { to: '/caja/saldos', label: 'Saldos y cierres', description: 'Cajas asignadas y cierre masivo' },
      { to: '/informes/cobro-diario', label: 'Cobro diario', description: 'Ruta del gestor (GPS)' },
    ],
  },
  {
    title: 'Crédito y clientes',
    links: [
      { to: '/credito/consulta', label: 'Consulta de crédito', description: 'Estado, cuotas y gestión' },
      { to: '/credito/simulador', label: 'Simulador', description: 'Plan de pagos y desembolso' },
      { to: '/credito/aprobar', label: 'Aprobar créditos', description: 'Bandeja de aprobación' },
      { to: '/clientes', label: 'Clientes', description: 'Búsqueda y ficha de persona' },
    ],
  },
  {
    title: 'Ventas, informes y administración',
    links: [
      { to: '/ventas/venta-rapida', label: 'Venta rápida', description: 'Pedido en mostrador' },
      { to: '/informes', label: 'Informes', description: 'Cartera, caja, almacén y crédito' },
      { to: '/maestros', label: 'Maestros', description: 'Marcas, artículos y almacenes' },
      { to: '/admin', label: 'Administración', description: 'Usuarios, roles y oficinas' },
    ],
  },
]

export function HomePage() {
  const { session } = useAuth()
  const [params] = useSearchParams()
  const roles = useMemo(() => session?.roles ?? [], [session?.roles])

  if (debeMostrarDashboardAnalista(roles, params.get('vista'))) {
    return <AnalystDashboardPage />
  }

  return <HomeHubPage roles={roles} />
}

function HomeHubPage({ roles }: { roles: string[] }) {
  const sections = useMemo(
    () =>
      SECTIONS.map((section) =>
        section.title === 'Crédito y clientes'
          ? {
              ...section,
              links: filterCreditoHomeLinks(section.links, roles),
            }
          : section,
      ),
    [roles],
  )

  const stats = useModuleHubStats('inicio', branding.appShortName, sections)
  const mostrarAtajoAnalista = esCreditoAdministrador(roles) && esCreditoAnalista(roles)

  return (
    <CredixPage
      title={`Inicio — ${branding.appShortName}`}
      subtitle={`${branding.systemDescription} — ${branding.companyLine1} ${branding.companyName}`}
      stats={stats}
      statsVariant="module"
      breadcrumb={[{ title: <Link to="/inicio">Inicio</Link> }]}
    >
      {mostrarAtajoAnalista ? (
        <Alert
          className="dash-admin-banner"
          type="info"
          showIcon
          message="También tienes rol de analista"
          description="El hub de accesos queda para administración. Tus indicadores personales están en el tablero operativo."
          action={
            <Link to="/inicio?vista=analista">
              <Button type="primary">Ver mi tablero</Button>
            </Link>
          }
        />
      ) : null}
      <CredixHubIntro>
        Panel principal con los mismos accesos que el menú lateral. El analista ve sus indicadores al
        entrar; aquí se mantiene el mapa de módulos para administración y el resto de perfiles.
      </CredixHubIntro>
      <CredixHubGrid sections={sections} variant="module" />
    </CredixPage>
  )
}
