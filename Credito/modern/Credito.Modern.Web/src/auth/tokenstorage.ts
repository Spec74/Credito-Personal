const ACCESS_KEY = 'credito.access'
const REFRESH_KEY = 'credito.refresh'
const EXPIRES_AT_KEY = 'credito.accessExpiresAt'

/**
 * Access token en localStorage: permite abrir informes en pestañas nuevas.
 * Refresh token en sessionStorage: reduce el impacto de XSS persistente y no se hereda al visor.
 */
const accessStore = localStorage
const refreshStore = sessionStorage

let accessTokenMemory: string | null = null

/** Migra access tokens guardados en sessionStorage (versión anterior). */
function migrateAccessFromSessionStorage(key: string): string | null {
  const legacy = refreshStore.getItem(key)
  if (!legacy) return null
  accessStore.setItem(key, legacy)
  refreshStore.removeItem(key)
  return legacy
}

/** Migra refresh tokens persistidos en localStorage y elimina la copia persistente. */
function migrateRefreshFromLocalStorage(): string | null {
  const legacy = accessStore.getItem(REFRESH_KEY)
  if (!legacy) return null
  refreshStore.setItem(REFRESH_KEY, legacy)
  accessStore.removeItem(REFRESH_KEY)
  return legacy
}

function readAccessKey(key: string): string | null {
  return accessStore.getItem(key) ?? migrateAccessFromSessionStorage(key)
}

export function getAccessToken(): string | null {
  return accessTokenMemory ?? readAccessKey(ACCESS_KEY)
}

export function getRefreshToken(): string | null {
  return refreshStore.getItem(REFRESH_KEY) ?? migrateRefreshFromLocalStorage()
}

export function getAccessExpiresAt(): number | null {
  const raw = readAccessKey(EXPIRES_AT_KEY)
  return raw ? Number(raw) : null
}

export function saveTokens(
  accessToken: string,
  refreshToken: string,
  expiresInSeconds: number,
): void {
  accessTokenMemory = accessToken
  accessStore.setItem(ACCESS_KEY, accessToken)
  refreshStore.setItem(REFRESH_KEY, refreshToken)
  const expiresAt = Date.now() + expiresInSeconds * 1000
  accessStore.setItem(EXPIRES_AT_KEY, String(expiresAt))
  refreshStore.removeItem(ACCESS_KEY)
  accessStore.removeItem(REFRESH_KEY)
  refreshStore.removeItem(EXPIRES_AT_KEY)
}

export function clearTokens(): void {
  accessTokenMemory = null
  accessStore.removeItem(ACCESS_KEY)
  accessStore.removeItem(REFRESH_KEY)
  accessStore.removeItem(EXPIRES_AT_KEY)
  refreshStore.removeItem(ACCESS_KEY)
  refreshStore.removeItem(REFRESH_KEY)
  refreshStore.removeItem(EXPIRES_AT_KEY)
}

export function hasStoredSession(): boolean {
  return Boolean(getAccessToken())
}
