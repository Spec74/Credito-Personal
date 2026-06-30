import { Link } from 'react-router-dom'

import { useMemo } from 'react'

import { branding } from '../config/branding'

import { CredixHubGrid, CredixHubIntro, CredixPage } from '../components/credix'

import { useModuleHubStats } from '../hooks/useModuleHubStats'

import { useAuth } from '../auth/useAuth'

import { filterCreditoHomeLinks } from '../utils/creditoHubFilter'



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

  const roles = useMemo(() => session?.roles ?? [], [session?.roles])



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



  return (

    <CredixPage

      title={`Inicio — ${branding.appShortName}`}

      subtitle={`${branding.systemDescription} — ${branding.companyLine1} ${branding.companyName}`}

      stats={stats}

      statsVariant="module"

      breadcrumb={[{ title: <Link to="/inicio">Inicio</Link> }]}

    >

      <CredixHubIntro>

        Panel principal con los mismos accesos que el menú lateral. Elija el módulo o la

        operación; la pantalla de cada área muestra indicadores y accesos como en el sistema

        anterior.

      </CredixHubIntro>

      <CredixHubGrid sections={sections} variant="module" />

    </CredixPage>

  )

}


