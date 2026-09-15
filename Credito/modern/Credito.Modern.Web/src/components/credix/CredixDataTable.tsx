import { useMemo } from 'react'
import { Table, type TableProps } from 'antd'
import { useInformeTableSearch } from '../informes/InformeTableContext'
import { enhanceInformeColumns, informeTableScrollX } from '../../utils/informeColumns'
import { filterInformeTableRows } from '../../utils/informeTableFilter'

export type CredixDataTableMode = 'informe' | 'operacion'

export type CredixDataTableProps<T extends object> = TableProps<T> & {
  /**
   * `informe`: búsqueda en contexto + scroll según columnas.
   * `operacion`: sin filtro de informe, columnas autoajustables (`tableLayout="auto"`).
   */
  mode?: CredixDataTableMode
}

/** Tabla Ant Design con defaults Credix; modo operación para crédito/caja. */
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
    ...rest
  } = props

  const tableSearch = useInformeTableSearch()
  const enhancedColumns = useMemo(
    () => enhanceInformeColumns(columns),
    [columns],
  )

  const filteredData = useMemo(() => {
    if (mode === 'operacion') {
      return dataSource ?? []
    }
    return filterInformeTableRows(dataSource ?? [], tableSearch, enhancedColumns)
  }, [dataSource, enhancedColumns, tableSearch, mode])

  const resolvedScroll = useMemo(() => {
    if (mode === 'operacion') {
      if (scroll == null) {
        return { x: 'max-content' as const }
      }
      if (typeof scroll === 'object') {
        return scroll
      }
      return { x: 'max-content' as const }
    }
    const defaultX = informeTableScrollX(enhancedColumns?.length ?? 8)
    if (scroll == null) {
      return { x: defaultX }
    }
    if (typeof scroll === 'object') {
      return { ...scroll, x: scroll.x ?? defaultX }
    }
    return { x: defaultX }
  }, [enhancedColumns?.length, scroll, mode])

  const defaultPagination =
    pagination === false
      ? false
      : {
          showSizeChanger: true,
          pageSizeOptions: ['10', '20', '50', '100'],
          defaultPageSize: 20,
          showTotal: (total: number) => `${total} registro(s)`,
          ...(typeof pagination === 'object' ? pagination : {}),
        }

  const tableClass =
    mode === 'operacion'
      ? ['credix-table', 'credix-table--operacion', className]
      : ['credix-table', 'credix-informe-table', className]

  const table = (
    <Table<T>
      size={size}
      bordered={bordered}
      className={tableClass.filter(Boolean).join(' ')}
      tableLayout={tableLayout ?? (mode === 'operacion' ? 'auto' : undefined)}
      pagination={defaultPagination}
      scroll={resolvedScroll}
      columns={enhancedColumns}
      dataSource={filteredData}
      {...rest}
    />
  )

  if (mode === 'operacion') {
    return <div className="credix-table-scroll">{table}</div>
  }

  return table
}
