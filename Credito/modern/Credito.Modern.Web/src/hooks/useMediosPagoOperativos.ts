import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchOficinaMediosPago, type OficinaMedioPago } from '../api/mediosPago'
import {
  buildMediosOficinaOptions,
  type TipoPagoOption,
} from '../pages/tesoreria/components/TipoPagoBrand'

function flattenMedios(rows: OficinaMedioPago[]): OficinaMedioPago[] {
  const out: OficinaMedioPago[] = []
  for (const m of rows) {
    out.push(m)
    for (const v of m.variantes ?? []) out.push(v)
  }
  return out
}

/**
 * Catálogo operativo por sucursal: solo activos de OficinaMedioPago.
 * El API siembra medios base al listar; no se cae al catálogo global VT13.
 */
export function useMediosPagoOperativos(
  oficinaId: number,
  opts?: {
    esCentral?: boolean
    enabled?: boolean
    /** Si true, solo raíces en la query (variantes vienen anidadas). Default true. */
    soloRaices?: boolean
  },
): {
  options: TipoPagoOption[]
  medios: OficinaMedioPago[]
  flat: OficinaMedioPago[]
  isLoading: boolean
  isFetching: boolean
} {
  const enabled = (opts?.enabled ?? true) && oficinaId > 0
  const esCentral = opts?.esCentral === true
  const soloRaices = opts?.soloRaices !== false

  const mediosQuery = useQuery({
    queryKey: ['oficina-medios-pago', oficinaId, { soloRaices }],
    queryFn: () => fetchOficinaMediosPago(oficinaId, { soloRaices }),
    enabled,
    staleTime: 30_000,
  })

  const flat = useMemo(
    () => flattenMedios(mediosQuery.data ?? []),
    [mediosQuery.data],
  )

  const options = useMemo(
    () =>
      buildMediosOficinaOptions(flat, {
        esCentral,
        // Respetar Activo=0 de efectivo: no reinyectar id 1.
        ensureEfectivo: false,
      }),
    [flat, esCentral],
  )

  return {
    options,
    medios: mediosQuery.data ?? [],
    flat,
    isLoading: mediosQuery.isLoading,
    isFetching: mediosQuery.isFetching,
  }
}
