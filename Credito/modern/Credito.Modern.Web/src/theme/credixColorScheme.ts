export type CredixColorSchemePreference = 'light' | 'dark' | 'system'
export type CredixResolvedColorScheme = 'light' | 'dark'

export const CREDIX_COLOR_SCHEME_KEY = 'credix-color-scheme'

export function isColorSchemePreference(
  value: string | null | undefined,
): value is CredixColorSchemePreference {
  return value === 'light' || value === 'dark' || value === 'system'
}

export function readColorSchemePreference(
  storage?: Pick<Storage, 'getItem'> | null,
): CredixColorSchemePreference {
  try {
    const raw = storage?.getItem(CREDIX_COLOR_SCHEME_KEY)
    return isColorSchemePreference(raw) ? raw : 'system'
  } catch {
    return 'system'
  }
}

export function resolveCredixColorScheme(input: {
  preference: CredixColorSchemePreference
  prefersDark?: boolean
}): CredixResolvedColorScheme {
  if (input.preference === 'dark') return 'dark'
  if (input.preference === 'light') return 'light'
  return input.prefersDark ? 'dark' : 'light'
}

export function prefersDarkColorScheme(
  media: Pick<MediaQueryList, 'matches'> | null | undefined = typeof window === 'undefined'
    ? null
    : window.matchMedia('(prefers-color-scheme: dark)'),
): boolean {
  return media?.matches === true
}

export function applyCredixColorScheme(resolved: CredixResolvedColorScheme): void {
  if (typeof document === 'undefined') return
  const root = document.documentElement
  root.setAttribute('data-theme', resolved)
  root.style.colorScheme = resolved
  const schemeMeta = document.querySelector('meta[name="color-scheme"]')
  schemeMeta?.setAttribute('content', resolved)
  const themeMeta = document.querySelector('meta[name="theme-color"]')
  themeMeta?.setAttribute('content', resolved === 'dark' ? '#12151a' : '#114885')
}

export function persistColorSchemePreference(
  preference: CredixColorSchemePreference,
  storage?: Pick<Storage, 'setItem'> | null,
): void {
  try {
    storage?.setItem(CREDIX_COLOR_SCHEME_KEY, preference)
  } catch {
    /* private mode */
  }
}
