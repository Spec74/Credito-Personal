import { Link } from 'react-router-dom'
import { CredixPage } from '../components/credix'
import { buildRoleHomeActions } from '../utils/roleHomeActions'
import { RoleHomeKpis } from './RoleHomeKpis'

export function RoleHomeStrip({ roles }: { roles: string[] }) {
  const actions = buildRoleHomeActions(roles)
  return (
    <nav className="credix-role-home-strip" aria-label="Hoy">
      {actions.map((a) => (
        <Link key={a.to} to={a.to} className={a.primary ? 'is-primary' : undefined}>
          {a.label}
        </Link>
      ))}
      <Link to="/comunicados">Avisos</Link>
      <Link to="/agenda">Agenda</Link>
      <Link to="/inicio?vista=modulos">Todas las pantallas</Link>
    </nav>
  )
}

export function RoleHomePage({ roles }: { roles: string[] }) {
  const actions = buildRoleHomeActions(roles)
  return (
    <CredixPage
      title="Hoy"
      actions={
        <Link to="/inicio?vista=modulos">Todas las pantallas</Link>
      }
    >
      <RoleHomeKpis />
      <nav className="credix-role-home" aria-label="Acciones de hoy">
        {actions.map((a) => (
          <Link
            key={a.to}
            to={a.to}
            className={['credix-role-home__action', a.primary ? 'is-primary' : '']
              .filter(Boolean)
              .join(' ')}
          >
            {a.label}
          </Link>
        ))}
        <Link to="/comunicados" className="credix-role-home__action">
          Avisos
        </Link>
        <Link to="/agenda" className="credix-role-home__action">
          Agenda
        </Link>
      </nav>
    </CredixPage>
  )
}
