/** Limpieza de marca local antigua; el cobro moderno no bloquea el día. */

const STORAGE_KEY = 'cobroBloqueEjecutado'

export function cobroBloqueHoyKey(): string {
  return new Date().toLocaleDateString()
}

/** @deprecated Soft signal only — no bloquear UI. */
export function isCobroBloqueEjecutadoHoy(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === cobroBloqueHoyKey()
  } catch {
    return false
  }
}

/** @deprecated Preferir cola «Enviados»; no usar para day-lock duro. */
export function markCobroBloqueEjecutadoHoy(): void {
  try {
    localStorage.setItem(STORAGE_KEY, cobroBloqueHoyKey())
  } catch {
    /* private mode / quota */
  }
}

export function clearCobroBloqueEjecutadoHoy(): void {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch {
    /* ignore */
  }
}
