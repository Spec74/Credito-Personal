import { useMemo } from 'react'

/** Franja KPI para listados CredixCrudPage — desactivada (ruido vs legacy). */
export function useCrudListStats(
  _rows: unknown[] | undefined,
  _entityLabel?: string,
  _activeCount?: number,
) {
  return useMemo(() => [], [])
}
