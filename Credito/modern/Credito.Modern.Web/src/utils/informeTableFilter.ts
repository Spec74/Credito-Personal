import type { ColumnsType } from 'antd/es/table'

function cellText(value: unknown): string {
  if (value == null) return ''
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return String(value)
}

/** Búsqueda rápida en filas visibles (paridad filtro jqGrid). */
export function filterInformeTableRows<T extends object>(
  rows: readonly T[] | undefined,
  query: string,
  columns?: ColumnsType<T>,
): T[] {
  const list = rows ?? []
  const q = query.trim().toLowerCase()
  if (!q) return [...list]

  const keys =
    columns
      ?.map((c) => {
        if ('dataIndex' in c && c.dataIndex != null) {
          return Array.isArray(c.dataIndex) ? c.dataIndex.join('.') : String(c.dataIndex)
        }
        return null
      })
      .filter((k): k is string => Boolean(k)) ?? []

  return list.filter((row) => {
    const record = row as Record<string, unknown>
    const parts: string[] = []
    for (const key of keys) {
      parts.push(cellText(record[key]))
    }
    for (const v of Object.values(record)) {
      parts.push(cellText(v))
    }
    return parts.join(' ').toLowerCase().includes(q)
  })
}
