import { Link } from 'react-router-dom'
import type { CredixHubLink } from './CredixHubGrid'

/** Franja de accesos rápidos (paridad pie del menú MVC). */
export function CredixQuickAccessStrip({ links }: { links: CredixHubLink[] }) {
  if (links.length === 0) return null
  return (
    <nav className="credix-quick-access" aria-label="Accesos rápidos">
      <span className="credix-quick-access-label">Accesos rápidos</span>
      <ul className="credix-quick-access-list">
        {links.map((link) => (
          <li key={link.to}>
            <Link to={link.to} className="credix-quick-access-link">
              {link.label}
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  )
}
