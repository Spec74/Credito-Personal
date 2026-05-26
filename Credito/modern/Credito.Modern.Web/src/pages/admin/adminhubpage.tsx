import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'

const SECTIONS = [
  {
    title: 'Oficinas',
    links: [{ to: '/admin/oficinas', label: 'Listado de oficinas' }],
  },
  {
    title: 'Seguridad',
    links: [
      { to: '/admin/usuarios', label: 'Usuarios' },
      { to: '/admin/roles', label: 'Roles y menús' },
    ],
  },
  {
    title: 'Comisiones',
    links: [{ to: '/admin/comisiones', label: 'Comisiones' }],
  },
]

export function AdminHubPage() {
  return (
    <CredixModuleHubPage
      moduleId="admin"
      title="Administración"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Administración' },
      ]}
      intro={
        <>
          Oficinas, usuarios, roles y comisiones — misma seguridad que el módulo Seguridad del
          sistema anterior.
        </>
      }
      sections={SECTIONS}
    />
  )
}
