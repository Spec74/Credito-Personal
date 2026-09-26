import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'
import { useAclHubSections } from '../../hooks/useAclHubSections'

const SECTIONS = [
  {
    title: 'Operaciones',
    links: [
      { to: '/tesoreria/boveda', label: 'Bóveda' },
      { to: '/tesoreria/movimiento-boveda', label: 'Informe movimiento bóveda' },
    ],
  },
]

export function TesoreriaHubPage() {
  const sections = useAclHubSections(SECTIONS)

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
