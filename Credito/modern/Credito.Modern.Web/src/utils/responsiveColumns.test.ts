import { describe, expect, it } from 'vitest'
import type { ColumnsType } from 'antd/es/table'
import { filterResponsiveColumns } from './responsiveColumns'

type Fila = { id: number; caja: string; resumen: string }

const columnas: ColumnsType<Fila> = [
  { title: 'N°', dataIndex: 'id' },
  { title: 'Caja', dataIndex: 'caja' },
  { title: 'Inicio', dataIndex: 'inicio', responsive: ['md'] },
  { title: 'Resumen', dataIndex: 'resumen', responsive: ['xl'] },
]

function titulos(cols: ColumnsType<Fila>): unknown[] {
  return cols.map((c) => c.title)
}

describe('filterResponsiveColumns', () => {
  it('en móvil deja solo las columnas sin restricción', () => {
    const visibles = filterResponsiveColumns(columnas, { xs: true })
    expect(titulos(visibles)).toEqual(['N°', 'Caja'])
  })

  it('incluye una columna cuando alguno de sus breakpoints está activo', () => {
    const visibles = filterResponsiveColumns(columnas, { xs: true, sm: true, md: true })
    expect(titulos(visibles)).toEqual(['N°', 'Caja', 'Inicio'])
  })

  it('en escritorio ancho muestra todas', () => {
    const visibles = filterResponsiveColumns(columnas, { md: true, lg: true, xl: true })
    expect(titulos(visibles)).toHaveLength(4)
  })

  it('sin breakpoints resueltos no oculta nada (evita parpadeo inicial)', () => {
    expect(filterResponsiveColumns(columnas, {})).toHaveLength(4)
  })
})
