import { Link } from 'react-router-dom'
import { ArrowRightOutlined } from '@ant-design/icons'
import { Button } from 'antd'
import type { ReactNode } from 'react'
import type { CredixHubLink } from './CredixHubGrid'
import { hubLinkIcon } from '../../utils/hubLinkIcons'

/** Tarjeta de acceso al módulo (paridad con `.box` del MVC: cabecera gris + acción). */
export function CredixModuleCard({
  link,
  emphasis,
}: {
  link: CredixHubLink
  /** Primera operación de la sección — botón principal más visible */
  emphasis?: boolean
}) {
  const icon = link.icon ?? hubLinkIcon(link.to, link.label)
  return (
    <article className="credix-module-card">
      <header className="credix-module-card-header">
        <span className="credix-module-card-icon" aria-hidden>
          {icon}
        </span>
        <h3 className="credix-module-card-title">{link.label}</h3>
      </header>
      <div className="credix-module-card-body">
        {link.description ? (
          <p className="credix-module-card-desc">{link.description}</p>
        ) : (
          <p className="credix-module-card-desc">Misma operación que en el sistema anterior.</p>
        )}
      </div>
      <footer className="credix-module-card-actions">
        <Link to={link.to}>
          <Button type={emphasis ? 'primary' : 'default'} icon={<ArrowRightOutlined />}>
            Abrir
          </Button>
        </Link>
      </footer>
    </article>
  )
}

export function CredixModuleSectionTitle({ children }: { children: ReactNode }) {
  return <h2 className="credix-module-section-title">{children}</h2>
}
