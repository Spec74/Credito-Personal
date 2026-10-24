export type CredixDensity = 'compact' | 'standard' | 'comfortable'

const CLASSES = [
  'credix-density-compact',
  'credix-density-standard',
  'credix-density-comfortable',
] as const

/** Mostrador (ratón, ≥768): compacto. Teléfono / táctil: holgado. Sin selector. */
export function resolveCredixDensity(input?: {
  coarse?: boolean
  width?: number
}): CredixDensity {
  const coarse =
    input?.coarse ??
    (typeof window !== 'undefined' ? window.matchMedia('(pointer: coarse)').matches : false)
  const width = input?.width ?? (typeof window !== 'undefined' ? window.innerWidth : 1280)
  if (coarse || width < 768) return 'comfortable'
  return 'compact'
}

export function applyCredixDensity(density: CredixDensity): void {
  if (typeof document === 'undefined') return
  const body = document.body
  body.classList.remove(...CLASSES)
  body.classList.add(`credix-density-${density}`)
}

export function syncCredixDensityFromViewport(): CredixDensity {
  const density = resolveCredixDensity()
  applyCredixDensity(density)
  return density
}
