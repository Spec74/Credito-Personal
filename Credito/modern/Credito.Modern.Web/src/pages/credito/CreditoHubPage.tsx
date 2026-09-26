import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { CREDITO_OPERACIONES_SECTIONS } from '../../config/creditoOperacionesSections'
import { CredixModuleHubPage } from '../../components/credix'
import { useAuth } from '../../auth/useAuth'
import { useAclHubSections } from '../../hooks/useAclHubSections'
import { filterCreditoHubSections } from '../../utils/creditoHubFilter'
import { esCreditoPerfilSoloBandeja } from '../../utils/creditoOperacionPermisos'

/** Operaciones diarias del módulo Crédito (no confundir con Reportes → Crédito). */
export function CreditoHubPage() {
  const { session } = useAuth()
  const roles = useMemo(() => session?.roles ?? [], [session?.roles])
  const soloBandeja = esCreditoPerfilSoloBandeja(roles)

  const roleSections = useMemo(
    () => filterCreditoHubSections(CREDITO_OPERACIONES_SECTIONS, roles),
    [roles],
  )
  const sections = useAclHubSections(roleSections)

  return (
    <CredixModuleHubPage
      title="Crédito"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Crédito' },
      ]}
      intro={
        soloBandeja ? (
          <>
            Su rol tiene acceso a la <Link to="/credito/aprobar">bandeja de aprobación</Link>.
          </>
        ) : undefined
      }
      sections={sections}
    />
  )
}
