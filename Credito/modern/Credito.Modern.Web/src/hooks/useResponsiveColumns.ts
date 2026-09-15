import { useMemo } from 'react'
import { Grid } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { filterResponsiveColumns } from '../utils/responsiveColumns'

/** Columnas visibles según el breakpoint actual, compartidas por la tabla y sus totales. */
export function useResponsiveColumns<T>(columns: ColumnsType<T>): ColumnsType<T> {
  const screens = Grid.useBreakpoint()

  return useMemo(() => filterResponsiveColumns(columns, screens), [columns, screens])
}
