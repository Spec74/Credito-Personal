import { Link } from 'react-router-dom'

import { useMemo } from 'react'

import { CREDITO_HUB_QUICK_ACCESS } from '../../config/creditoHubSections'

import { CREDITO_OPERACIONES_SECTIONS } from '../../config/creditoOperacionesSections'

import { CredixModuleHubPage } from '../../components/credix'

import { useAuth } from '../../auth/useAuth'

import { filterCreditoHubSections } from '../../utils/creditoHubFilter'

import { esCreditoPerfilSoloBandeja } from '../../utils/creditoOperacionPermisos'



/** Operaciones diarias del módulo Crédito (no confundir con Reportes → Crédito). */

export function CreditoHubPage() {

  const { session } = useAuth()

  const roles = useMemo(() => session?.roles ?? [], [session?.roles])

  const soloBandeja = esCreditoPerfilSoloBandeja(roles)



  const sections = useMemo(

    () => filterCreditoHubSections(CREDITO_OPERACIONES_SECTIONS, roles),

    [roles],

  )



  const quickAccess = useMemo(

    () =>

      soloBandeja

        ? CREDITO_HUB_QUICK_ACCESS.filter((l) => l.to === '/credito/aprobar')

        : CREDITO_HUB_QUICK_ACCESS,

    [soloBandeja],

  )



  return (

    <CredixModuleHubPage

      moduleId="credito"

      title="Crédito — operaciones"

      breadcrumb={[

        { title: <Link to="/inicio">Inicio</Link> },

        { title: 'Crédito' },

      ]}

      intro={

        soloBandeja ? (

          <>

            Su rol <strong>APROBADOR 1</strong> tiene acceso a la{' '}

            <Link to="/credito/aprobar">bandeja de aprobación</Link>. Las demás operaciones de

            crédito requieren un rol adicional (gestor, encargado o administrador).

          </>

        ) : (

          <>

            Punto de entrada: <Link to="/credito/consulta">Consulta de crédito</Link> (cliente →

            crédito → plan). También simulador, aprobación y tareas. Los{' '}

            <strong>informes con cajas y filtros</strong>{' '}

            del menú <strong>Reportes → Crédito</strong> y <strong>Reportes → Cobranza</strong> están en{' '}

            <Link to="/reportes/credito">Reportes de crédito</Link> y{' '}

            <Link to="/reportes/cobranza">Cobranza pagos</Link>.

          </>

        )

      }

      quickAccess={quickAccess}

      sections={sections}

    />

  )

}


