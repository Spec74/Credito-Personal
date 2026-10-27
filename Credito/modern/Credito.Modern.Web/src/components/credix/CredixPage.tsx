import type { ReactNode } from 'react'
import { Breadcrumb, Space } from 'antd'
import type { BreadcrumbProps } from 'antd'
import { CredixStatsBar, type CredixStatItem } from './CredixStatsBar'

export function CredixPage({
  title,
  subtitle,
  breadcrumb,
  actions,
  stats,
  statsVariant = 'default',
  toolbar,
  className,
  translate = 'no',
  children,
}: {
  title: string
  subtitle?: ReactNode
  breadcrumb?: BreadcrumbProps['items']
  actions?: ReactNode
  stats?: CredixStatItem[]
  /** `module` = franja oscura en índice de módulo. */
  statsVariant?: 'default' | 'module'
  /** Filtros / búsqueda de la bandeja. Hereda el gap del body. */
  toolbar?: ReactNode
  className?: string
  /** Evita que el traductor del navegador doble letras en UI operativa. */
  translate?: 'yes' | 'no'
  children: ReactNode
}) {
  return (
    <article
      className={['credix-page', className].filter(Boolean).join(' ')}
      translate={translate}
    >
      {breadcrumb && breadcrumb.length > 0 ? (
        <Breadcrumb items={breadcrumb} className="credix-page-breadcrumb" />
      ) : null}

      <header className="credix-page-head">
        <div className="credix-page-head-text">
          <h1 className="credix-page-title">{title}</h1>
          {/* Nativo: evita que el LCP espere Typography de antd (elementRenderDelay). */}
          {subtitle ? <p className="credix-page-subtitle">{subtitle}</p> : null}
        </div>
        {actions ? <Space wrap className="credix-page-actions">{actions}</Space> : null}
      </header>

      {stats && stats.length > 0 ? (
        <CredixStatsBar items={stats} variant={statsVariant} />
      ) : null}

      <div className="credix-page-body">
        {toolbar ? <div className="credix-page-toolbar">{toolbar}</div> : null}
        {children}
      </div>
    </article>
  )
}
