import { useCallback, useEffect, useState } from 'react'
import {
  applyCredixDensity,
  persistCredixDensity,
  readCredixDensity,
  type CredixDensity,
} from '../theme/credixDensity'

export function useCredixDensity(): [CredixDensity, (next: CredixDensity) => void] {
  const [density, setDensity] = useState<CredixDensity>(() =>
    typeof document === 'undefined' ? 'standard' : readCredixDensity(),
  )

  useEffect(() => {
    applyCredixDensity(density)
  }, [density])

  const update = useCallback((next: CredixDensity) => {
    persistCredixDensity(next)
    setDensity(next)
  }, [])

  return [density, update]
}
