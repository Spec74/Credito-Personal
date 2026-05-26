const ACCESS_KEY = 'credito.access'
const REFRESH_KEY = 'credito.refresh'
const EXPIRES_AT_KEY = 'credito.accessExpiresAt'

/** localStorage: misma sesión en pestañas nuevas (informes PDF en /reportes/visor). */
const store = localStorage

let accessTokenMemory: string | null = null

/** Migra tokens guardados en sessionStorage (versión anterior). */
function migrateFromSessionStorage(key: string): string | null {
  const legacy = sessionStorage.getItem(key)
  if (!legacy) return null
  store.setItem(key, legacy)
  sessionStorage.removeItem(key)
  return legacy
}

function readKey(key: string): string | null {
  return store.getItem(key) ?? migrateFromSessionStorage(key)
}

export function getAccessToken(): string | null {
  return accessTokenMemory ?? readKey(ACCESS_KEY)
}

export function getRefreshToken(): string | null {
  return readKey(REFRESH_KEY)
}

export function getAccessExpiresAt(): number | null {
  const raw = readKey(EXPIRES_AT_KEY)
  return raw ? Number(raw) : null
}

export function saveTokens(
  accessToken: string,
  refreshToken: string,
  expiresInSeconds: number,
): void {
  accessTokenMemory = accessToken
  store.setItem(ACCESS_KEY, accessToken)
  store.setItem(REFRESH_KEY, refreshToken)
  const expiresAt = Date.now() + expiresInSeconds * 1000
  store.setItem(EXPIRES_AT_KEY, String(expiresAt))
  sessionStorage.removeItem(ACCESS_KEY)
  sessionStorage.removeItem(REFRESH_KEY)
  sessionStorage.removeItem(EXPIRES_AT_KEY)
}

export function clearTokens(): void {
  accessTokenMemory = null
  store.removeItem(ACCESS_KEY)
  store.removeItem(REFRESH_KEY)
  store.removeItem(EXPIRES_AT_KEY)
  sessionStorage.removeItem(ACCESS_KEY)
  sessionStorage.removeItem(REFRESH_KEY)
  sessionStorage.removeItem(EXPIRES_AT_KEY)
}

export function hasStoredSession(): boolean {
  return Boolean(getAccessToken() && getRefreshToken())
}
