/** Paridad localStorage MVC `cobroBloqueEjecutado` — evita reentrar el mismo día. */

const STORAGE_KEY = 'cobroBloqueEjecutado'

export function cobroBloqueHoyKey(): string {
  return new Date().toLocaleDateString()
}

export function isCobroBloqueEjecutadoHoy(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === cobroBloqueHoyKey()
  } catch {
    return false
  }
}

export function markCobroBloqueEjecutadoHoy(): void {
  try {
    localStorage.setItem(STORAGE_KEY, cobroBloqueHoyKey())
  } catch {
    /* private mode / quota */
  }
}
