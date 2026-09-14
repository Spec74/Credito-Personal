import type { ColumnsType, ColumnType } from 'antd/es/table'
import type { Breakpoint } from 'antd/es/_util/responsiveObserver'

export type ScreenMap = Partial<Record<Breakpoint, boolean>>

/**
 * Columnas visibles para los breakpoints activos, con la misma regla que Ant Design: una columna
 * con `responsive` se muestra si alguno de sus breakpoints está activo.
 *
 * Se resuelve aquí (y no solo dentro de la tabla) para que la fila de totales use exactamente
 * las mismas columnas y no se desalinee en móvil.
 */
export function filterResponsiveColumns<T>(
  columns: ColumnsType<T>,
  screens: ScreenMap,
): ColumnsType<T> {
  // Antes del primer cálculo de breakpoints no hay información: mostrar todo evita parpadeos.
  if (Object.keys(screens).length === 0) {
    return columns
  }
  return columns.filter((col) => {
    const responsive = (col as ColumnType<T>).responsive
    return responsive == null || responsive.some((bp) => screens[bp] === true)
  })
}
