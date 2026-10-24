import { useEffect, useState } from 'react'
import {
  resolveCredixDensity,
  syncCredixDensityFromViewport,
  type CredixDensity,
} from '../theme/credixDensity'

/** Aplica densidad según viewport / puntero. No hay control en la UI. */
export function useCredixDensity(): CredixDensity {
  const [density, setDensity] = useState<CredixDensity>(() =>
    typeof window === 'undefined' ? 'compact' : resolveCredixDensity(),
  )

  useEffect(() => {
    const sync = () => setDensity(syncCredixDensityFromViewport())
    sync()
    const mqCoarse = window.matchMedia('(pointer: coarse)')
    const mqNarrow = window.matchMedia('(max-width: 767px)')
    mqCoarse.addEventListener('change', sync)
    mqNarrow.addEventListener('change', sync)
    return () => {
      mqCoarse.removeEventListener('change', sync)
      mqNarrow.removeEventListener('change', sync)
    }
  }, [])

  return density
}
