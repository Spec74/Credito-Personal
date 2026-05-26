import type { ReactNode } from 'react'
import type { BreadcrumbProps } from 'antd'
import { useModuleHubStats, type ModuleHubId } from '../../hooks/useModuleHubStats'
import {
  CredixHubGrid,
  CredixHubIntro,
  type CredixHubLink,
  type CredixHubSection,
} from './CredixHubGrid'
import { CredixQuickAccessStrip } from './CredixQuickAccessStrip'
import { CredixPage } from './CredixPage'
import type { CredixStatItem } from './CredixStatsBar'

/** Página índice del módulo — paridad con MVC (indicadores + cajas de acceso). */
export function CredixModuleHubPage({
  moduleId,
  title,
  breadcrumb,
  intro,
  sections,
  quickAccess,
  extraStats,
  statsTone = 'module',
}: {
  moduleId: ModuleHubId
  title: string
  breadcrumb: NonNullable<BreadcrumbProps['items']>
  intro: ReactNode
  sections: CredixHubSection[]
  quickAccess?: CredixHubLink[]
  extraStats?: CredixStatItem[]
  statsTone?: 'module' | 'default'
}) {
  const hubStats = useModuleHubStats(moduleId, title, sections)
  const stats = extraStats?.length ? [...hubStats, ...extraStats] : hubStats

  return (
    <CredixPage
      title={title}
      breadcrumb={breadcrumb}
      stats={stats}
      statsVariant={statsTone}
    >
      <CredixHubIntro>{intro}</CredixHubIntro>
      {quickAccess?.length ? <CredixQuickAccessStrip links={quickAccess} /> : null}
      <CredixHubGrid sections={sections} variant="module" />
    </CredixPage>
  )
}
