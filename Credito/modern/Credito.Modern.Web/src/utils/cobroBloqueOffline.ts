/**
 * Caché de planilla + cola de procesamiento offline (PWA campo).
 *
 * - Mientras hay red: se actualiza la foto de la cartera del día.
 * - Sin red: el gestor sigue tipando sobre la última foto + borrador.
 * - Procesar sin red: encola el lote; al volver online se envía una sola vez.
 * - Cola de otro día / otra caja se descarta (nunca auto-cobra ayer).
 */

import type { CreditoGestorPendienteRow } from '../api/cajaDiario'
import { fechaOperacionLocal } from './cobroBloqueDraft'

export type CobroBloquePlanillaItem = {
  creditoId: number
  montoPagar: number
  tipoPagoId: number
  fechaHoraTrans?: string | null
}

export type CobroBloqueSessionSnap = {
  oficinaId: number
  cajaDiarioId: number
  cajaId: number
  cajaDenominacion: string
  fechaIniOperacion: string
  saldoInicial: number
  entradas: number
  salidas: number
  saldoFinal: number
  indCierre: boolean
  esCajaCentral: boolean
}

export type CobroBloqueCarteraCache = {
  version: 1
  usuarioId: number
  oficinaId: number
  cajaDiarioId: number
  fechaOperacion: string
  cachedAt: string
  rows: CreditoGestorPendienteRow[]
  /** Última sesión abierta vista online (para seguir tipando sin red). */
  session: CobroBloqueSessionSnap | null
}

export type CobroBloquePendingProcess = {
  version: 1
  usuarioId: number
  oficinaId: number
  cajaDiarioId: number
  fechaOperacion: string
  queuedAt: string
  conCobro: number
  total: number
  planilla: CobroBloquePlanillaItem[]
}

/** v3: planilla con lat/lng de cliente (GPS campo). */
const CACHE_PREFIX = 'credix.cobroBloqueCartera.v3'
/** Prefijos anteriores (sin lat/lng o schema viejo); se purgan al tocar v3. */
const LEGACY_CACHE_PREFIXES = [
  'credix.cobroBloqueCartera.v1',
  'credix.cobroBloqueCartera.v2',
] as const
const QUEUE_PREFIX = 'credix.cobroBloquePending.v1'

function cacheKey(usuarioId: number, cajaDiarioId: number, fecha: string) {
  return `${CACHE_PREFIX}:${usuarioId}:${cajaDiarioId}:${fecha}`
}

function queueKey(usuarioId: number, cajaDiarioId: number, fecha: string) {
  return `${QUEUE_PREFIX}:${usuarioId}:${cajaDiarioId}:${fecha}`
}

function parseJson<T>(raw: string | null): T | null {
  if (!raw) return null
  try {
    return JSON.parse(raw) as T
  } catch {
    return null
  }
}

export function isBrowserOnline(): boolean {
  return typeof navigator === 'undefined' ? true : navigator.onLine !== false
}

export function isLikelyNetworkError(e: unknown): boolean {
  if (!isBrowserOnline()) return true
  if (e instanceof TypeError) return true
  const msg = e instanceof Error ? e.message : String(e ?? '')
  return /failed to fetch|networkerror|load failed|offline|timeout/i.test(msg)
}

function purgePrefix(prefix: string, keepKey?: string | null): void {
  try {
    const remove: string[] = []
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i)
      if (!k?.startsWith(prefix)) continue
      if (keepKey && k === keepKey) continue
      remove.push(k)
    }
    for (const k of remove) localStorage.removeItem(k)
  } catch {
    /* ignore */
  }
}

function purgeLegacyCarteraCaches(): void {
  for (const prefix of LEGACY_CACHE_PREFIXES) {
    purgePrefix(prefix)
  }
}

/**
 * Recupera la última sesión de caja tipada en caché (sin conocer aún cajaDiarioId).
 * Escanea v3; si no hay, intenta v2/v1 del mismo día (migración) y purga legacy al hallar v3.
 */
export function findCobroBloqueOfflineSession(
  usuarioId: number,
): CobroBloqueSessionSnap | null {
  if (usuarioId < 1) return null
  const fecha = fechaOperacionLocal()
  const prefixes = [CACHE_PREFIX, ...LEGACY_CACHE_PREFIXES]
  try {
    for (const prefix of prefixes) {
      const needle = `${prefix}:${usuarioId}:`
      for (let i = 0; i < localStorage.length; i++) {
        const k = localStorage.key(i)
        if (!k?.startsWith(needle) || !k.endsWith(`:${fecha}`)) continue
        const parsed = parseJson<{ session?: CobroBloqueSessionSnap | null }>(
          localStorage.getItem(k),
        )
        if (parsed?.session && !parsed.session.indCierre) {
          return parsed.session
        }
      }
    }
  } catch {
    /* ignore */
  }
  return null
}

export function saveCobroBloqueCarteraCache(cache: CobroBloqueCarteraCache): void {
  try {
    const key = cacheKey(cache.usuarioId, cache.cajaDiarioId, cache.fechaOperacion)
    purgePrefix(CACHE_PREFIX, key)
    purgeLegacyCarteraCaches()
    localStorage.setItem(key, JSON.stringify(cache))
  } catch {
    /* quota */
  }
}

export function loadCobroBloqueCarteraCache(args: {
  usuarioId: number
  cajaDiarioId: number
}): CobroBloqueCarteraCache | null {
  if (args.usuarioId < 1 || args.cajaDiarioId < 1) return null
  const fecha = fechaOperacionLocal()
  try {
    purgePrefix(CACHE_PREFIX, cacheKey(args.usuarioId, args.cajaDiarioId, fecha))
    purgeLegacyCarteraCaches()
    const data = parseJson<CobroBloqueCarteraCache>(
      localStorage.getItem(cacheKey(args.usuarioId, args.cajaDiarioId, fecha)),
    )
    if (
      !data ||
      data.version !== 1 ||
      data.usuarioId !== args.usuarioId ||
      data.cajaDiarioId !== args.cajaDiarioId ||
      data.fechaOperacion !== fecha ||
      !Array.isArray(data.rows)
    ) {
      return null
    }
    return data
  } catch {
    return null
  }
}

export function enqueueCobroBloqueProcess(
  pending: CobroBloquePendingProcess,
): void {
  try {
    const key = queueKey(pending.usuarioId, pending.cajaDiarioId, pending.fechaOperacion)
    purgePrefix(QUEUE_PREFIX, key)
    localStorage.setItem(key, JSON.stringify(pending))
  } catch {
    /* quota */
  }
}

export function loadCobroBloquePendingProcess(args: {
  usuarioId: number
  cajaDiarioId: number
}): CobroBloquePendingProcess | null {
  if (args.usuarioId < 1 || args.cajaDiarioId < 1) return null
  const fecha = fechaOperacionLocal()
  try {
    purgePrefix(QUEUE_PREFIX, queueKey(args.usuarioId, args.cajaDiarioId, fecha))
    const data = parseJson<CobroBloquePendingProcess>(
      localStorage.getItem(queueKey(args.usuarioId, args.cajaDiarioId, fecha)),
    )
    if (
      !data ||
      data.version !== 1 ||
      data.usuarioId !== args.usuarioId ||
      data.cajaDiarioId !== args.cajaDiarioId ||
      data.fechaOperacion !== fecha ||
      !Array.isArray(data.planilla)
    ) {
      return null
    }
    return data
  } catch {
    return null
  }
}

export function clearCobroBloquePendingProcess(args: {
  usuarioId: number
  cajaDiarioId: number
}): void {
  try {
    const fecha = fechaOperacionLocal()
    localStorage.removeItem(queueKey(args.usuarioId, args.cajaDiarioId, fecha))
    purgePrefix(QUEUE_PREFIX)
  } catch {
    /* ignore */
  }
}

export function clearCobroBloqueCarteraCache(args: {
  usuarioId: number
  cajaDiarioId: number
}): void {
  try {
    const fecha = fechaOperacionLocal()
    localStorage.removeItem(cacheKey(args.usuarioId, args.cajaDiarioId, fecha))
    purgePrefix(CACHE_PREFIX)
  } catch {
    /* ignore */
  }
}
