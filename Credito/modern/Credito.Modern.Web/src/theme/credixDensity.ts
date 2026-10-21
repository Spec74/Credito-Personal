export const CREDIX_DENSITY_KEY = 'credix.density'

export type CredixDensity = 'compact' | 'standard' | 'comfortable'

const VALUES: CredixDensity[] = ['compact', 'standard', 'comfortable']

export function isCredixDensity(value: string | null | undefined): value is CredixDensity {
  return VALUES.includes(value as CredixDensity)
}

export function readCredixDensity(): CredixDensity {
  try {
    const raw = localStorage.getItem(CREDIX_DENSITY_KEY)
    if (isCredixDensity(raw)) return raw
  } catch {
    /* private mode */
  }
  return 'standard'
}

export function applyCredixDensity(density: CredixDensity): void {
  if (typeof document === 'undefined') return
  const body = document.body
  body.classList.remove(
    'credix-density-compact',
    'credix-density-standard',
    'credix-density-comfortable',
  )
  body.classList.add(`credix-density-${density}`)
}

export function persistCredixDensity(density: CredixDensity): void {
  try {
    localStorage.setItem(CREDIX_DENSITY_KEY, density)
  } catch {
    /* ignore */
  }
  applyCredixDensity(density)
}
