import { Link } from 'react-router-dom'
import type { ReactNode } from 'react'
import { CredixModuleCard, CredixModuleSectionTitle } from './CredixModuleCard'

export interface CredixHubLink {
  to: string
  label: string
  description?: string
  icon?: ReactNode
}

export interface CredixHubSection {
  title: string
  links: CredixHubLink[]
}

export function CredixHubGrid({
  sections,
  variant = 'links',
}: {
  sections: CredixHubSection[]
  /** `module` = tarjetas tipo caja MVC; `links` = lista compacta (inicio rápido) */
  variant?: 'module' | 'links'
}) {
  if (variant === 'links') {
    return (
      <div className="credix-hub-grid credix-hub-grid--links">
        {sections.map((section) => (
          <section key={section.title} className="credix-hub-section">
            <h2 className="credix-hub-section-title">{section.title}</h2>
            <ul className="credix-hub-links">
              {section.links.map((link) => (
                <li key={link.to}>
                  <Link to={link.to} className="credix-hub-link">
                    <span className="credix-hub-link-label">{link.label}</span>
                    {link.description ? (
                      <span className="credix-hub-link-desc">{link.description}</span>
                    ) : null}
                  </Link>
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>
    )
  }

  return (
    <div className="credix-hub-module">
      {sections.map((section) => (
        <section key={section.title} className="credix-hub-module-block">
          <CredixModuleSectionTitle>{section.title}</CredixModuleSectionTitle>
          <div className="credix-module-card-grid">
            {section.links.map((link, index) => (
              <CredixModuleCard key={link.to} link={link} emphasis={index === 0} />
            ))}
          </div>
        </section>
      ))}
    </div>
  )
}

export function CredixHubIntro({ children }: { children: ReactNode }) {
  return <p className="credix-hub-intro">{children}</p>
}
