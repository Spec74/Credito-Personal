const ACCESS_KEY = 'credito.access'
const REFRESH_KEY = 'credito.refresh'
const EXPIRES_AT_KEY = 'credito.accessExpiresAt'
const PERSIST_REFRESH_KEY = 'credito.refreshPersist'

/**
 * Access token en localStorage: permite abrir informes en pestañas nuevas.
 * Refresh token:
 * - sessionStorage por defecto (cierra pestaña → fin de sesión)
 * - localStorage si el usuario marcó «Recordar sesión»
 */
const accessStore = localStorage

let accessTokenMemory: string | null = null

function refreshStore(persist: boolean): Storage {
  return persist ? localStorage : sessionStorage
}

export function isRefreshPersisted(): boolean {
  return accessStore.getItem(PERSIST_REFRESH_KEY) === '1'
}

/** Migra access tokens guardados en sessionStorage (versión anterior). */
function migrateAccessFromSessionStorage(key: string): string | null {
  const legacy = sessionStorage.getItem(key)
  if (!legacy) return null
  accessStore.setItem(key, legacy)
  sessionStorage.removeItem(key)
  return legacy
}

function readAccessKey(key: string): string | null {
  return accessStore.getItem(key) ?? migrateAccessFromSessionStorage(key)
}

export function getAccessToken(): string | null {
  return accessTokenMemory ?? readAccessKey(ACCESS_KEY)
}

export function getRefreshToken(): string | null {
  return (
    sessionStorage.getItem(REFRESH_KEY) ??
    localStorage.getItem(REFRESH_KEY) ??
    null
  )
}

export function getAccessExpiresAt(): number | null {
  const raw = readAccessKey(EXPIRES_AT_KEY)
  return raw ? Number(raw) : null
}

/**
 * @param persistRefresh Si se omite, conserva la preferencia actual (p. ej. al rotar refresh).
 */
export function saveTokens(
  accessToken: string,
  refreshToken: string,
  expiresInSeconds: number,
  persistRefresh?: boolean,
): void {
  const persist =
    persistRefresh === undefined ? isRefreshPersisted() : persistRefresh

  accessTokenMemory = accessToken
  accessStore.setItem(ACCESS_KEY, accessToken)
  const expiresAt = Date.now() + expiresInSeconds * 1000
  accessStore.setItem(EXPIRES_AT_KEY, String(expiresAt))

  sessionStorage.removeItem(REFRESH_KEY)
  localStorage.removeItem(REFRESH_KEY)
  refreshStore(persist).setItem(REFRESH_KEY, refreshToken)

  if (persist) {
    accessStore.setItem(PERSIST_REFRESH_KEY, '1')
  } else {
    accessStore.removeItem(PERSIST_REFRESH_KEY)
  }

  sessionStorage.removeItem(ACCESS_KEY)
  sessionStorage.removeItem(EXPIRES_AT_KEY)
}

export function clearTokens(): void {
  accessTokenMemory = null
  accessStore.removeItem(ACCESS_KEY)
  accessStore.removeItem(REFRESH_KEY)
  accessStore.removeItem(EXPIRES_AT_KEY)
  accessStore.removeItem(PERSIST_REFRESH_KEY)
  sessionStorage.removeItem(ACCESS_KEY)
  sessionStorage.removeItem(REFRESH_KEY)
  sessionStorage.removeItem(EXPIRES_AT_KEY)
}

export function hasStoredSession(): boolean {
  return Boolean(getAccessToken())
}
