import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'
import { useAclHubSections } from '../../hooks/useAclHubSections'
import { useEsCentral } from '../../hooks/useEsCentral'

export function TesoreriaHubPage() {
  const { esCentral } = useEsCentral()
  const sectionsDef = useMemo(
    () => [
      {
        title: 'Operaciones',
        links: [
          { to: '/tesoreria/boveda', label: 'Bóveda' },
          esCentral
            ? { to: '/tesoreria/fondear-sucursales', label: 'Fondear sucursales' }
            : { to: '/tesoreria/boveda', label: 'Enviar a Central' },
          { to: '/tesoreria/red', label: 'Red de sucursales' },
          { to: '/tesoreria/movimiento-boveda', label: 'Informe movimiento bóveda' },
        ],
      },
    ],
    [esCentral],
  )
  const sections = useAclHubSections(sectionsDef)

  return (
    <CredixModuleHubPage
      title="Tesorería"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Tesorería' },
      ]}
      sections={sections}
    />
  )
}
