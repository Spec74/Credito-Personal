import { useMemo } from 'react'
import { buildCrudListStats } from '../utils/credixVisualStats'

/** Franja KPI para listados CredixCrudPage. */
export function useCrudListStats(
  rows: unknown[] | undefined,
  entityLabel?: string,
  activeCount?: number,
) {
  return useMemo(
    () => buildCrudListStats(rows, { entityLabel, activeCount }),
    [rows, entityLabel, activeCount],
  )
}
