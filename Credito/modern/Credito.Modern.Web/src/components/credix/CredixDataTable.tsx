import { useEffect, useMemo, useState, type Key, type ReactNode } from 'react'
import { Checkbox, Grid, Pagination, Table, type TableProps } from 'antd'
import type { ColumnType, ColumnsType } from 'antd/es/table'
import type { TableRowSelection } from 'antd/es/table/interface'
import { useInformeTableSearch } from '../informes/InformeTableContext'
import { enhanceInformeColumns, informeTableScrollX } from '../../utils/informeColumns'
import { filterInformeTableRows } from '../../utils/informeTableFilter'
import { CredixWideTable } from './CredixWideTable'

export type CredixDataTableMode = 'informe' | 'operacion'

export type CredixDataTableProps<T extends object> = TableProps<T> & {
  /**
   * `informe`: búsqueda en contexto + scroll según columnas.
   * `operacion`: sin filtro de informe, columnas autoajustables (`tableLayout="auto"`).
   */
  mode?: CredixDataTableMode
  /** Si es false, en móvil no se usan tarjetas (solo scroll horizontal). Default true. */
  mobileCards?: boolean
}

function flattenColumns<T extends object>(columns: ColumnsType<T> | undefined): ColumnType<T>[] {
  if (!columns?.length) {
    return []
  }
  const out: ColumnType<T>[] = []
  for (const col of columns) {
    if (!col || typeof col !== 'object') {
      continue
    }
    if ('children' in col && Array.isArray(col.children) && col.children.length > 0) {
      out.push(...flattenColumns(col.children as ColumnsType<T>))
      continue
    }
    out.push(col as ColumnType<T>)
  }
  return out
}

function columnLabel<T extends object>(col: ColumnType<T>): string {
  if (typeof col.title === 'string' || typeof col.title === 'number') {
    return String(col.title)
  }
  return String(col.key ?? col.dataIndex ?? '')
}

function isActionsColumn<T extends object>(col: ColumnType<T>): boolean {
  const key = String(col.key ?? '').toLowerCase()
  const dataIndex = Array.isArray(col.dataIndex)
    ? col.dataIndex.join('.').toLowerCase()
    : String(col.dataIndex ?? '').toLowerCase()
  const title = columnLabel(col).toLowerCase()
  return (
    key === 'acciones' ||
    key === 'accion' ||
    key === 'action' ||
    key === 'actions' ||
    dataIndex === 'acciones' ||
    dataIndex === 'accion' ||
    dataIndex === 'action' ||
    dataIndex === 'actions' ||
    /acci[oó]n(es)?/.test(title) ||
    title === 'actions'
  )
}

function stripFixedColumns<T extends object>(
  columns: ColumnsType<T> | undefined,
): ColumnsType<T> | undefined {
  if (!columns?.length) {
    return columns
  }
  return columns.map((col) => {
    if (!col || typeof col !== 'object') {
      return col
    }
    if ('children' in col && Array.isArray(col.children) && col.children.length > 0) {
      return {
        ...col,
        fixed: undefined,
        children: stripFixedColumns(col.children as ColumnsType<T>),
      }
    }
    if (!('fixed' in col) || col.fixed == null) {
      return col
    }
    const { fixed: _fixed, ...rest } = col as ColumnType<T> & { fixed?: unknown }
    return rest as ColumnType<T>
  })
}

function readCellValue<T extends object>(record: T, dataIndex: ColumnType<T>['dataIndex']): unknown {
  if (dataIndex == null) {
    return undefined
  }
  if (Array.isArray(dataIndex)) {
    let cur: unknown = record
    for (const part of dataIndex) {
      if (cur == null || typeof cur !== 'object') {
        return undefined
      }
      cur = (cur as Record<string, unknown>)[String(part)]
    }
    return cur
  }
  return (record as Record<string, unknown>)[String(dataIndex)]
}

function renderColumnCell<T extends object>(
  col: ColumnType<T>,
  record: T,
  index: number,
): ReactNode {
  const value = readCellValue(record, col.dataIndex)
  if (typeof col.render === 'function') {
    return col.render(value, record, index) as ReactNode
  }
  if (value == null || value === '') {
    return '—'
  }
  if (typeof value === 'object') {
    return null
  }
  return String(value)
}

function resolveRowKey<T extends object>(
  record: T,
  index: number,
  rowKey: TableProps<T>['rowKey'],
): Key {
  if (typeof rowKey === 'function') {
    return rowKey(record, index)
  }
  if (typeof rowKey === 'string') {
    return (record as Record<string, Key>)[rowKey] ?? index
  }
  return (record as { key?: Key }).key ?? index
}

function columnsHaveFixed<T extends object>(columns: ColumnsType<T> | undefined): boolean {
  return flattenColumns(columns).some((c) => c.fixed === 'left' || c.fixed === 'right' || c.fixed === true)
}

/** Tabla Ant Design con defaults Credix; en móvil ≤767px muestra tarjetas legibles. */
export function CredixDataTable<T extends object>(props: CredixDataTableProps<T>) {
  const {
    size = 'small',
    bordered = true,
    className,
    pagination,
    scroll,
    dataSource,
    columns,
    mode = 'informe',
    tableLayout,
    mobileCards = true,
    rowKey,
    rowSelection,
    ...rest
  } = props

  const screens = Grid.useBreakpoint()
  // md undefined antes del layout → tratar como móvil para no aplastar columnas fijas.
  const isMobile = screens.md !== true
  const useCards = mobileCards && isMobile

  const tableSearch = useInformeTableSearch()
  const enhancedColumns = useMemo(() => {
    const base = enhanceInformeColumns(columns)
    // En viewport estrecho, quitar fixed (rompe Ant Design con scroll.max-content).
    return isMobile ? stripFixedColumns(base) : base
  }, [columns, isMobile])

  const filteredData = useMemo(() => {
    if (mode === 'operacion') {
      return dataSource ?? []
    }
    return filterInformeTableRows(dataSource ?? [], tableSearch, enhancedColumns)
  }, [dataSource, enhancedColumns, tableSearch, mode])

  const flatColumns = useMemo(() => flattenColumns(enhancedColumns), [enhancedColumns])
  const hasFixed = columnsHaveFixed(enhancedColumns)

  const resolvedScroll = useMemo(() => {
    // Ant Design: columnas `fixed` exigen scroll.x numérico; 'max-content' las aplasta en móvil.
    const numericFallback = Math.max(880, flatColumns.length * 120)
    if (mode === 'operacion') {
      if (scroll == null) {
        return { x: hasFixed ? numericFallback : ('max-content' as const) }
      }
      if (typeof scroll === 'object') {
        if (hasFixed && (scroll.x == null || scroll.x === 'max-content')) {
          return { ...scroll, x: numericFallback }
        }
        return scroll
      }
      return { x: hasFixed ? numericFallback : ('max-content' as const) }
    }
    const defaultX = informeTableScrollX(enhancedColumns?.length ?? 8)
    if (scroll == null) {
      return { x: hasFixed ? Math.max(defaultX, numericFallback) : defaultX }
    }
    if (typeof scroll === 'object') {
      const x = scroll.x ?? defaultX
      if (hasFixed && (x === 'max-content' || x == null)) {
        return { ...scroll, x: Math.max(defaultX, numericFallback) }
      }
      return { ...scroll, x }
    }
    return { x: defaultX }
  }, [enhancedColumns?.length, flatColumns.length, hasFixed, scroll, mode])

  const defaultPagination =
    pagination === false
      ? false
      : {
          showSizeChanger: !isMobile,
          pageSizeOptions: ['10', '15', '20', '25', '50', '100'],
          defaultPageSize: isMobile ? 10 : 20,
          showTotal: (total: number) => `${total} registro(s)`,
          ...(typeof pagination === 'object' ? pagination : {}),
        }

  const tableClass =
    mode === 'operacion'
      ? ['credix-table', 'credix-table--operacion', className]
      : ['credix-table', 'credix-informe-table', className]

  if (useCards) {
    const dataCols = flatColumns.filter((c) => !isActionsColumn(c))
    const actionCols = flatColumns.filter((c) => isActionsColumn(c))
    const pageSize =
      typeof defaultPagination === 'object' && defaultPagination
        ? (defaultPagination.pageSize ?? defaultPagination.defaultPageSize ?? 10)
        : 10
    const total = filteredData.length

    return (
      <CredixMobileCardList
        className={className}
        records={filteredData}
        dataColumns={dataCols}
        actionColumns={actionCols}
        rowKey={rowKey}
        rowSelection={rowSelection}
        onRow={rest.onRow}
        loading={rest.loading}
        locale={rest.locale}
        pagination={
          defaultPagination === false
            ? false
            : {
                total,
                pageSize,
                showTotal:
                  typeof defaultPagination === 'object'
                    ? defaultPagination.showTotal
                    : (t: number) => `${t} registro(s)`,
              }
        }
      />
    )
  }

  const table = (
    <Table<T>
      size={size}
      bordered={bordered}
      className={tableClass.filter(Boolean).join(' ')}
      tableLayout={tableLayout ?? (mode === 'operacion' ? 'auto' : undefined)}
      pagination={defaultPagination}
      scroll={resolvedScroll}
      rowSelection={rowSelection}
      columns={enhancedColumns}
      dataSource={filteredData}
      rowKey={rowKey}
      {...rest}
    />
  )

  if (mode === 'operacion') {
    return (
      <CredixWideTable className="credix-table-scroll">
        {table}
      </CredixWideTable>
    )
  }

  return <CredixWideTable className="credix-table-scroll credix-table-scroll--informe">{table}</CredixWideTable>
}

function CredixMobileCardList<T extends object>({
  className,
  records,
  dataColumns,
  actionColumns,
  rowKey,
  rowSelection,
  pagination,
  onRow,
  loading,
  locale,
}: {
  className?: string
  records: readonly T[]
  dataColumns: ColumnType<T>[]
  actionColumns: ColumnType<T>[]
  rowKey: TableProps<T>['rowKey']
  rowSelection?: TableRowSelection<T>
  pagination:
    | false
    | {
        total: number
        pageSize: number
        showTotal?: (total: number, range: [number, number]) => ReactNode
      }
  onRow?: TableProps<T>['onRow']
  loading?: TableProps<T>['loading']
  locale?: TableProps<T>['locale']
}) {
  const [page, setPage] = useState(1)
  useEffect(() => {
    setPage(1)
  }, [records.length])

  const pageSize = pagination === false ? records.length || 1 : pagination.pageSize
  const start = pagination === false ? 0 : (page - 1) * pageSize
  const slice = records.slice(start, start + pageSize)
  const emptyText: ReactNode =
    locale?.emptyText == null
      ? 'Sin registros'
      : typeof locale.emptyText === 'function'
        ? locale.emptyText()
        : locale.emptyText
  const isLoading = Boolean(loading)
  const selectedKeys = new Set((rowSelection?.selectedRowKeys ?? []).map(String))
  const selectionEnabled = Boolean(rowSelection)

  const emitSelectionChange = (nextKeys: Key[], nextRows: T[]) => {
    rowSelection?.onChange?.(nextKeys, nextRows, { type: 'all' })
  }

  const toggleKey = (key: Key, checked: boolean) => {
    if (!rowSelection) return
    const currentKeys = [...(rowSelection.selectedRowKeys ?? [])]
    const keyStr = String(key)
    let nextKeys: Key[]
    if (checked) {
      nextKeys = currentKeys.some((k) => String(k) === keyStr) ? currentKeys : [...currentKeys, key]
    } else {
      nextKeys = currentKeys.filter((k) => String(k) !== keyStr)
    }
    const keySet = new Set(nextKeys.map(String))
    const nextRows = records.filter((r, i) => keySet.has(String(resolveRowKey(r, i, rowKey))))
    emitSelectionChange(nextKeys, nextRows)
  }

  return (
    <div
      className={['credix-mobile-cards', isLoading ? 'credix-mobile-cards--loading' : '', className]
        .filter(Boolean)
        .join(' ')}
    >
      {slice.length === 0 ? (
        <div className="credix-mobile-cards__empty">{isLoading ? 'Cargando…' : emptyText}</div>
      ) : (
        slice.map((record, index) => {
          const absoluteIndex = start + index
          const key = resolveRowKey(record, absoluteIndex, rowKey)
          const rowProps = onRow?.(record, absoluteIndex) ?? {}
          const checkboxProps = rowSelection?.getCheckboxProps?.(record) ?? {}
          const checked = selectedKeys.has(String(key))
          const selectedClass = checked ? 'credix-mobile-card--selected' : ''
          return (
            <article
              key={key}
              className={['credix-mobile-card', selectedClass, rowProps.className]
                .filter(Boolean)
                .join(' ')}
              onClick={(e) => {
                if (selectionEnabled && !(checkboxProps.disabled)) {
                  const target = e.target as HTMLElement
                  if (!target.closest('.credix-mobile-card__actions, a, button, .ant-btn')) {
                    toggleKey(key, !checked)
                  }
                }
                rowProps.onClick?.(e)
              }}
              onDoubleClick={rowProps.onDoubleClick}
              style={rowProps.style}
            >
              {selectionEnabled ? (
                <div className="credix-mobile-card__select">
                  <Checkbox
                    checked={checked}
                    disabled={Boolean(checkboxProps.disabled)}
                    onClick={(e) => e.stopPropagation()}
                    onChange={(e) => toggleKey(key, e.target.checked)}
                  />
                </div>
              ) : null}
              <div className="credix-mobile-card__body">
                {dataColumns.map((col, colIdx) => {
                  const label = columnLabel(col) || `Campo ${colIdx + 1}`
                  const content = renderColumnCell(col, record, absoluteIndex)
                  if (colIdx === 0) {
                    return (
                      <div key={`${String(key)}-title`} className="credix-mobile-card__title">
                        <span className="credix-mobile-card__title-label">{label}</span>
                        <div className="credix-mobile-card__title-value">{content}</div>
                      </div>
                    )
                  }
                  return (
                    <div key={`${String(key)}-${label}-${colIdx}`} className="credix-mobile-card__row">
                      <span className="credix-mobile-card__label">{label}</span>
                      <div className="credix-mobile-card__value">{content}</div>
                    </div>
                  )
                })}
                {actionColumns.length > 0 ? (
                  <div className="credix-mobile-card__actions">
                    {actionColumns.map((col, colIdx) => (
                      <div
                        key={`act-${String(key)}-${colIdx}`}
                        className="credix-mobile-card__actions-inner"
                      >
                        {renderColumnCell(col, record, absoluteIndex)}
                      </div>
                    ))}
                  </div>
                ) : null}
              </div>
            </article>
          )
        })
      )}
      {pagination !== false && pagination.total > pageSize ? (
        <div className="credix-mobile-cards__pager">
          <Pagination
            size="small"
            current={page}
            pageSize={pageSize}
            total={pagination.total}
            showSizeChanger={false}
            showTotal={pagination.showTotal}
            onChange={setPage}
          />
        </div>
      ) : null}
    </div>
  )
}
