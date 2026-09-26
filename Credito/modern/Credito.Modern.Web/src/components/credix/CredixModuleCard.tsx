import { Link } from 'react-router-dom'
import { ArrowRightOutlined } from '@ant-design/icons'
import { Button } from 'antd'
import type { ReactNode } from 'react'
import type { CredixHubLink } from './CredixHubGrid'
import { hubLinkIcon } from '../../utils/hubLinkIcons'

/** Tarjeta de acceso al módulo: solo etiqueta funcional (sin texto de relleno). */
export function CredixModuleCard({ link }: { link: CredixHubLink }) {
  const icon = link.icon ?? hubLinkIcon(link.to, link.label)
  return (
    <article className="credix-module-card">
      <header className="credix-module-card-header">
        <span className="credix-module-card-icon" aria-hidden>
          {icon}
        </span>
        <h3 className="credix-module-card-title">{link.label}</h3>
      </header>
      <footer className="credix-module-card-actions">
        <Link to={link.to}>
          <Button type="default" icon={<ArrowRightOutlined />}>
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
