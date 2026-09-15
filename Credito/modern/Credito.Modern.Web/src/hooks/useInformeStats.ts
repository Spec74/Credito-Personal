import { useMemo } from 'react'
import type { CredixStatItem } from '../components/credix'
import { buildInformeStats } from '../utils/credixVisualStats'

type ConsultaLike<T> = {
  data?: T[] | null
  isSuccess?: boolean
  isError?: boolean
}

/** Franja KPI estándar para pantallas CredixInformePage. */
export function useInformeStats<T>(
  consulta: ConsultaLike<T>,
  oficinaId?: number | null,
  extras?: CredixStatItem[],
) {
  const extrasKey = extras?.map((e) => `${e.label}:${e.value}`).join('|') ?? ''
  return useMemo(
    () =>
      buildInformeStats({
        rowCount: consulta.data?.length ?? (consulta.isSuccess ? 0 : null),
        queried: !!(consulta.isSuccess || consulta.isError),
        oficinaId,
        extras,
      }),
    // extras estable vía extrasKey (evita recrear array en cada render del padre)
    // eslint-disable-next-line react-hooks/exhaustive-deps -- extrasKey
    [consulta.data, consulta.isSuccess, consulta.isError, oficinaId, extrasKey],
  )
}
