import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'
import { useAclHubSections } from '../../hooks/useAclHubSections'

const SECTIONS = [
  {
    title: 'Catálogos',
    links: [
      { to: '/maestros/marcas', label: 'Marcas' },
      { to: '/maestros/modelos', label: 'Modelos' },
      { to: '/maestros/tipos-articulo', label: 'Tipos de artículo' },
      { to: '/maestros/articulos', label: 'Artículos' },
      { to: '/maestros/almacenes', label: 'Almacenes' },
    ],
  },
]

export function MaestrosHubPage() {
  const sections = useAclHubSections(SECTIONS)

  return (
    <CredixModuleHubPage
      title="Maestros"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Maestros' },
      ]}
      searchable
      sections={sections}
    />
  )
}
