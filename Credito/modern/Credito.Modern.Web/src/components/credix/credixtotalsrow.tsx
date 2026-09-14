import type { ReactNode } from 'react'
import { Table } from 'antd'
import type { ColumnsType, ColumnType } from 'antd/es/table'

/** Contenido de la fila de totales indexado por `dataIndex`/`key` de columna. */
export type CredixTotals = Record<string, ReactNode>

type Props<T> = {
  /** Columnas realmente visibles (ver `useResponsiveColumns`). */
  columns: ColumnsType<T>
  totals: CredixTotals
}

function columnKey<T>(col: ColumnsType<T>[number], index: number): string {
  const dataIndex = (col as ColumnType<T>).dataIndex
  if (dataIndex != null) {
    return Array.isArray(dataIndex) ? dataIndex.join('.') : String(dataIndex)
  }
  return String(col.key ?? index)
}

/**
 * Fila de totales alineada a las columnas visibles: emite una celda por columna en vez de
 * `colSpan` fijos, que se descuadran cuando un breakpoint oculta columnas.
 */
export function CredixTotalsRow<T>({ columns, totals }: Props<T>) {
  return (
    <Table.Summary fixed>
      <Table.Summary.Row>
        {columns.map((col, index) => {
          const key = columnKey(col, index)
          return (
            <Table.Summary.Cell
              key={key}
              index={index}
              align={(col as ColumnType<T>).align}
            >
              {totals[key] ?? null}
            </Table.Summary.Cell>
          )
        })}
      </Table.Summary.Row>
    </Table.Summary>
  )
}
