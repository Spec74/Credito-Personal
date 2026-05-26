import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'

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
  return (
    <CredixModuleHubPage
      moduleId="maestros"
      title="Maestros"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Maestros' },
      ]}
      intro={
        <>
          Catálogos de mantenimiento: alta, edición y activación (marcas, modelos, artículos,
          almacenes).
        </>
      }
      sections={SECTIONS}
    />
  )
}
