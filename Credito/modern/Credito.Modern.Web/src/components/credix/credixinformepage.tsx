import { useMemo, useState, type ReactNode } from 'react'
import { Input, Space } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import type { BreadcrumbProps } from 'antd'
import { InformeTableContext } from '../informes/InformeTableContext'
import { CredixFilterBar } from './CredixFilterBar'
import { CredixPage } from './CredixPage'
import { CredixPanel } from './CredixPanel'
import type { CredixStatItem } from './CredixStatsBar'

/** Layout estándar para pantallas «Ver pantalla» (filtros, export, búsqueda, tabla). */
export function CredixInformePage({
  title,
  subtitle,
  breadcrumb,
  stats,
  filters,
  exportBar,
  error,
  children,
  panelTitle = 'Resultados',
  enableTableSearch = true,
  searchPlaceholder = 'Buscar en la tabla…',
  tableSearchVariant = 'compact',
  tableResultCount,
  panelActions,
  panelSearchFirst = true,
  className,
}: {
  title: string
  subtitle?: ReactNode
  breadcrumb: NonNullable<BreadcrumbProps['items']>
  className?: string
  stats?: CredixStatItem[]
  filters?: ReactNode
  exportBar?: ReactNode
  error?: ReactNode
  children: ReactNode
  panelTitle?: string
  enableTableSearch?: boolean
  searchPlaceholder?: string
  /** `prominent` = barra ancha sobre la tabla; `compact` = input en cabecera del panel. */
  tableSearchVariant?: 'compact' | 'prominent'
  /** Total de filas cargadas (para texto «N clientes»). */
  tableResultCount?: number
  /** Botones junto al buscador del panel (p. ej. Excel en cabecera de tabla). */
  panelActions?: ReactNode
  /** En cabecera: buscador antes que Excel (franja «Clientes del gestor»). */
  panelSearchFirst?: boolean
}) {
  const [tableSearch, setTableSearch] = useState('')
  const prominent = tableSearchVariant === 'prominent'

  const searchInput = enableTableSearch ? (
    <Input
      allowClear
      size={prominent ? 'middle' : 'small'}
      prefix={<SearchOutlined />}
      placeholder={searchPlaceholder}
      value={tableSearch}
      onChange={(e) => setTableSearch(e.target.value)}
      className={
        prominent
          ? 'credix-cobranza-table-search'
          : 'credix-informe-panel-search credix-informe-panel-search--wide'
      }
      aria-label="Buscar en resultados"
    />
  ) : null

  const panelExtra = useMemo(() => {
    if (prominent) {
      return panelActions ?? null
    }
    if (!searchInput && !panelActions) return null
    const countBadge =
      tableResultCount != null && tableResultCount > 0 ? (
        <span className="credix-informe-panel-count" aria-live="polite">
          {tableResultCount} cliente{tableResultCount === 1 ? '' : 's'}
        </span>
      ) : null
    return (
      <Space wrap size="small" className="credix-informe-panel-actions credix-informe-panel-actions--header">
        {panelSearchFirst ? (
          <>
            {searchInput}
            {countBadge}
            {panelActions}
          </>
        ) : (
          <>
            {panelActions}
            {searchInput}
            {countBadge}
          </>
        )}
      </Space>
    )
  }, [panelActions, panelSearchFirst, prominent, searchInput, tableResultCount])

  const prominentToolbar =
    prominent && enableTableSearch ? (
      <div className="credix-cobranza-table-toolbar">
        <div className="credix-cobranza-table-search-wrap">
          <span className="credix-cobranza-table-search-label">
            <SearchOutlined aria-hidden />
            Buscar en la lista
          </span>
          {searchInput}
        </div>
        {tableResultCount != null ? (
          <span className="credix-cobranza-table-search-meta">
            {tableResultCount} cliente{tableResultCount === 1 ? '' : 's'} cargados
            {tableSearch.trim() ? ' · filtro activo' : ''}
          </span>
        ) : null}
      </div>
    ) : null

  const body = enableTableSearch ? (
    <InformeTableContext.Provider value={tableSearch}>{children}</InformeTableContext.Provider>
  ) : (
    children
  )

  return (
    <CredixPage
      title={title}
      subtitle={subtitle}
      breadcrumb={breadcrumb}
      stats={stats}
      className={['credix-informe-page', className].filter(Boolean).join(' ')}
    >
      {(filters != null || exportBar != null) && (
        <CredixFilterBar className="credix-informe-filter-bar">
          <div className="credix-informe-filter-row">{filters}</div>
          {exportBar != null ? (
            <div className="credix-informe-export-row">{exportBar}</div>
          ) : null}
        </CredixFilterBar>
      )}
      {error}
      <CredixPanel
        title={panelTitle}
        extra={panelExtra}
        className="credix-informe-results-panel"
      >
        {prominentToolbar}
        {body}
      </CredixPanel>
    </CredixPage>
  )
}
