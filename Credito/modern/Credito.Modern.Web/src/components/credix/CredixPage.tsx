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
  className,
  children,
}: {
  title: string
  subtitle?: ReactNode
  breadcrumb?: BreadcrumbProps['items']
  actions?: ReactNode
  stats?: CredixStatItem[]
  /** `module` = franja oscura en índice de módulo. */
  statsVariant?: 'default' | 'module'
  className?: string
  children: ReactNode
}) {
  return (
    <div className={['credix-page', className].filter(Boolean).join(' ')}>
      {breadcrumb && breadcrumb.length > 0 ? (
        <Breadcrumb items={breadcrumb} className="credix-page-breadcrumb" />
      ) : null}

      <div className="credix-page-head">
        <div className="credix-page-head-text">
          <h1 className="credix-page-title">{title}</h1>
          {/* Nativo: evita que el LCP espere Typography de antd (elementRenderDelay). */}
          {subtitle ? <p className="credix-page-subtitle">{subtitle}</p> : null}
        </div>
        {actions ? <Space wrap className="credix-page-actions">{actions}</Space> : null}
      </div>

      {stats && stats.length > 0 ? (
        <CredixStatsBar items={stats} variant={statsVariant} />
      ) : null}

      <div className="credix-page-body">{children}</div>
    </div>
  )
}
