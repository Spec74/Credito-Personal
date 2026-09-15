import type { ReactNode } from 'react'
import type { BreadcrumbProps } from 'antd'
import { CredixFilterBar } from './CredixFilterBar'
import { CredixPage } from './CredixPage'
import type { CredixStatItem } from './CredixStatsBar'
import { CredixPanel } from './CredixPanel'

/** Layout estándar para maestros y administración (toolbar + tabla + modales fuera). */
export function CredixCrudPage({
  title,
  subtitle,
  breadcrumb,
  actions,
  stats,
  toolbar,
  children,
  panelTitle = 'Listado',
  extra,
  className,
}: {
  title: string
  subtitle?: ReactNode
  breadcrumb: NonNullable<BreadcrumbProps['items']>
  actions?: ReactNode
  stats?: CredixStatItem[]
  toolbar?: ReactNode
  children: ReactNode
  panelTitle?: string
  /** Modales u otro contenido debajo del panel */
  extra?: ReactNode
  className?: string
}) {
  return (
    <CredixPage
      className={className}
      title={title}
      subtitle={subtitle}
      breadcrumb={breadcrumb}
      actions={actions}
      stats={stats}
    >
      {toolbar != null ? <CredixFilterBar>{toolbar}</CredixFilterBar> : null}
      <CredixPanel title={panelTitle}>{children}</CredixPanel>
      {extra}
    </CredixPage>
  )
}
