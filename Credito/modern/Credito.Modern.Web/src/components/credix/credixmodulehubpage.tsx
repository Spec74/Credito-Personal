import { useMemo, useState, type ReactNode } from 'react'
import type { BreadcrumbProps } from 'antd'
import { Input } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
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

function filterHubSections(sections: CredixHubSection[], query: string): CredixHubSection[] {
  const q = query.trim().toLowerCase()
  if (!q) {
    return sections
  }
  return sections
    .map((section) => ({
      ...section,
      links: section.links.filter((link) => {
        const haystack = `${link.label} ${link.description ?? ''} ${section.title}`.toLowerCase()
        return haystack.includes(q)
      }),
    }))
    .filter((section) => section.links.length > 0)
}

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
  searchable = false,
  searchPlaceholder = 'Buscar en el módulo…',
}: {
  moduleId: ModuleHubId
  title: string
  breadcrumb: NonNullable<BreadcrumbProps['items']>
  intro: ReactNode
  sections: CredixHubSection[]
  quickAccess?: CredixHubLink[]
  extraStats?: CredixStatItem[]
  statsTone?: 'module' | 'default'
  /** Filtro local sobre tarjetas (útil en Informes). */
  searchable?: boolean
  searchPlaceholder?: string
}) {
  const [hubSearch, setHubSearch] = useState('')
  const filteredSections = useMemo(
    () => (searchable ? filterHubSections(sections, hubSearch) : sections),
    [sections, searchable, hubSearch],
  )
  const hubStats = useModuleHubStats(moduleId, title, filteredSections)
  const stats = extraStats?.length ? [...hubStats, ...extraStats] : hubStats

  return (
    <CredixPage
      title={title}
      breadcrumb={breadcrumb}
      stats={stats}
      statsVariant={statsTone}
    >
      <CredixHubIntro>{intro}</CredixHubIntro>
      {searchable ? (
        <Input
          allowClear
          size="large"
          prefix={<SearchOutlined />}
          placeholder={searchPlaceholder}
          value={hubSearch}
          onChange={(e) => setHubSearch(e.target.value)}
          aria-label="Buscar en el hub"
          style={{ maxWidth: 480, marginBottom: 16 }}
        />
      ) : null}
      {quickAccess?.length ? <CredixQuickAccessStrip links={quickAccess} /> : null}
      {filteredSections.length === 0 ? (
        <CredixHubIntro>No hay informes que coincidan con «{hubSearch.trim()}».</CredixHubIntro>
      ) : (
        <CredixHubGrid sections={filteredSections} variant="module" />
      )}
    </CredixPage>
  )
}
