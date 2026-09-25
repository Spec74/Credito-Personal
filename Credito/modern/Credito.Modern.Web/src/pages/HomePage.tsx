import { Link, useSearchParams } from 'react-router-dom'
import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Alert, Button } from 'antd'
import { branding } from '../config/branding'
import { CredixHubGrid, CredixHubIntro, CredixPage } from '../components/credix'
import { useModuleHubStats } from '../hooks/useModuleHubStats'
import { useAuth } from '../auth/useAuth'
import { fetchMenu } from '../api/menu'
import { filterCreditoHomeLinks } from '../utils/creditoHubFilter'
import { hasMenuRouteAccess } from '../utils/menuRouteAccess'
import {
  debeMostrarDashboardAdmin,
  debeMostrarDashboardAnalista,
  esCreditoAdministrador,
  esCreditoAnalista,
} from '../utils/creditoOperacionPermisos'
import { AdminDashboardPage } from './dashboard/AdminDashboardPage'
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
      {
        to: '/admin',
        label: 'Administración',
        description: 'Oficinas, cajas, usuarios, roles y comisiones',
      },
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
  const { session } = useAuth()
  const menuQuery = useQuery({
    queryKey: ['menu', session?.oficinaId, session?.usuarioId],
    queryFn: fetchMenu,
    enabled: (session?.oficinaId ?? 0) > 0 && (session?.usuarioId ?? 0) > 0,
    staleTime: 5 * 60_000,
  })
  const menu = menuQuery.data ?? []

  const sections = useMemo(
    () =>
      SECTIONS.map((section) => {
        let links = section.links
        if (section.title === 'Crédito y clientes') {
          links = filterCreditoHomeLinks(links, roles)
        }
        // Esperar menú cargado para no ocultar tarjetas por ACL vacío.
        if (menuQuery.isSuccess) {
          links = links.filter((link) => hasMenuRouteAccess(link.to, menu))
        }
        return { ...section, links }
      }).filter((section) => section.links.length > 0),
    [roles, menu, menuQuery.isSuccess],
  )

  const stats = useModuleHubStats('inicio', branding.appShortName, sections)
  const esAdmin = esCreditoAdministrador(roles)
  const esAnalista = esCreditoAnalista(roles)

  return (
    <CredixPage
      title={`Inicio — ${branding.appShortName}`}
      subtitle={`${branding.systemDescription} — ${branding.companyLine1} ${branding.companyName}`}
      stats={stats}
      statsVariant="default"
      breadcrumb={[{ title: <Link to="/inicio">Inicio</Link> }]}
    >
      {esAdmin ? (
        <Alert
          className="dash-admin-banner"
          type="info"
          showIcon
          message="Mapa de módulos"
          description="El tablero gerencial de la oficina es la pantalla de inicio. Aquí quedan los accesos al resto de la operación."
          action={
            <>
              <Link to="/inicio">
                <Button type="primary">Tablero gerencial</Button>
              </Link>
              {esAnalista ? (
                <Link to="/inicio?vista=analista">
                  <Button>Mi tablero</Button>
                </Link>
              ) : null}
            </>
          }
        />
      ) : null}
      <CredixHubIntro>
        El administrador ve el tablero de la oficina al entrar; el analista, el suyo. Este mapa
        queda para el resto de perfiles y para quien lo abra con «Mapa de módulos». Cada tarjeta
        abre solo módulos habilitados en su menú.
      </CredixHubIntro>
      <CredixHubGrid sections={sections} variant="module" />
    </CredixPage>
  )
}
