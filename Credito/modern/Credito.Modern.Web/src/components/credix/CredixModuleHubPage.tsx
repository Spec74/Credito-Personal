import { useMemo, useState, type ReactNode } from 'react'
import type { BreadcrumbProps } from 'antd'
import { Input } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import {
  CredixHubGrid,
  CredixHubIntro,
  type CredixHubSection,
} from './CredixHubGrid'
import { CredixPage } from './CredixPage'
import type { CredixStatItem } from './CredixStatsBar'

function filterHubSectionsByQuery(sections: CredixHubSection[], query: string): CredixHubSection[] {
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

/**
 * Índice de módulo: título + tarjetas funcionales (sin KPIs decorativos ni Accesos rápidos).
 * Los accesos rápidos viven solo en el sidebar.
 */
export function CredixModuleHubPage({
  title,
  breadcrumb,
  intro,
  sections,
  extraStats,
  statsTone = 'default',
  searchable = false,
  searchPlaceholder = 'Buscar…',
}: {
  title: string
  breadcrumb: NonNullable<BreadcrumbProps['items']>
  intro?: ReactNode
  sections: CredixHubSection[]
  /** Solo KPIs operativos reales del módulo (p. ej. totales legacy). */
  extraStats?: CredixStatItem[]
  statsTone?: 'module' | 'default'
  searchable?: boolean
  searchPlaceholder?: string
}) {
  const [hubSearch, setHubSearch] = useState('')
  const filteredSections = useMemo(
    () => (searchable ? filterHubSectionsByQuery(sections, hubSearch) : sections),
    [sections, searchable, hubSearch],
  )

  return (
    <CredixPage
      title={title}
      breadcrumb={breadcrumb}
      stats={extraStats}
      statsVariant={statsTone}
    >
      {intro ? <CredixHubIntro>{intro}</CredixHubIntro> : null}
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
      {filteredSections.length === 0 ? (
        <CredixHubIntro>
          {hubSearch.trim()
            ? `No hay opciones que coincidan con «${hubSearch.trim()}».`
            : 'No hay pantallas habilitadas en su menú para este módulo.'}
        </CredixHubIntro>
      ) : (
        <CredixHubGrid sections={filteredSections} variant="module" />
      )}
    </CredixPage>
  )
}
