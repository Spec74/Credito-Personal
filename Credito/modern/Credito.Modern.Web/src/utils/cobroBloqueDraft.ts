/**
 * Borrador local de cobro en bloque (PWA / navegación entre módulos).
 *
 * - Persiste montos tipados mientras el gestor cambia de pantalla.
 * - Nunca se envía solo: solo vive en el dispositivo.
 * - Expira al cambiar el día operativo (fecha local) o si la caja diario cambia.
 * - Si el gestor no vuelve: el borrador caduca solo; no altera la BD.
 */

export type CobroBloqueRowDraft = {
  montoPagar: number
  tipoPagoId: number
  fechaHoraTrans: string
  cuotasSeleccionadas: number[]
}

export type CobroBloqueDraft = {
  version: 1
  usuarioId: number
  oficinaId: number
  cajaDiarioId: number
  /** yyyy-mm-dd local del día operativo */
  fechaOperacion: string
  updatedAt: string
  filtro: string
  edits: Record<string, CobroBloqueRowDraft>
}

const PREFIX = 'credix.cobroBloqueDraft.v1'

export function fechaOperacionLocal(d = new Date()): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

function storageKey(usuarioId: number, cajaDiarioId: number, fecha: string): string {
  return `${PREFIX}:${usuarioId}:${cajaDiarioId}:${fecha}`
}

function safeParse(raw: string | null): CobroBloqueDraft | null {
  if (!raw) return null
  try {
    const data = JSON.parse(raw) as CobroBloqueDraft
    if (data?.version !== 1 || typeof data.edits !== 'object') return null
    return data
  } catch {
    return null
  }
}

/** Elimina borradores de días / cajas anteriores (higiene del dispositivo). */
export function purgeStaleCobroBloqueDrafts(keep?: {
  usuarioId: number
  cajaDiarioId: number
  fechaOperacion: string
}): number {
  let removed = 0
  try {
    const keepKey = keep
      ? storageKey(keep.usuarioId, keep.cajaDiarioId, keep.fechaOperacion)
      : null
    const toRemove: string[] = []
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i)
      if (!k?.startsWith(`${PREFIX}:`)) continue
      if (keepKey && k === keepKey) continue
      toRemove.push(k)
    }
    for (const k of toRemove) {
      localStorage.removeItem(k)
      removed++
    }
  } catch {
    /* private mode */
  }
  return removed
}

export function loadCobroBloqueDraft(args: {
  usuarioId: number
  oficinaId: number
  cajaDiarioId: number
}): { draft: CobroBloqueDraft | null; discardedStale: boolean } {
  const fecha = fechaOperacionLocal()
  let discardedStale = false
  try {
    // ¿Había borrador de otro día/caja?
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i)
      if (!k?.startsWith(`${PREFIX}:`)) continue
      if (k !== storageKey(args.usuarioId, args.cajaDiarioId, fecha)) {
        discardedStale = true
        break
      }
    }
    purgeStaleCobroBloqueDrafts({
      usuarioId: args.usuarioId,
      cajaDiarioId: args.cajaDiarioId,
      fechaOperacion: fecha,
    })
    const draft = safeParse(
      localStorage.getItem(storageKey(args.usuarioId, args.cajaDiarioId, fecha)),
    )
    if (
      !draft ||
      draft.usuarioId !== args.usuarioId ||
      draft.cajaDiarioId !== args.cajaDiarioId ||
      draft.fechaOperacion !== fecha
    ) {
      return { draft: null, discardedStale }
    }
    const hasEdits = Object.keys(draft.edits).length > 0
    return { draft: hasEdits ? draft : null, discardedStale }
  } catch {
    return { draft: null, discardedStale: false }
  }
}

export function saveCobroBloqueDraft(draft: CobroBloqueDraft): void {
  try {
    const key = storageKey(draft.usuarioId, draft.cajaDiarioId, draft.fechaOperacion)
    localStorage.setItem(key, JSON.stringify(draft))
  } catch {
    /* quota / private */
  }
}

export function clearCobroBloqueDraft(args: {
  usuarioId: number
  cajaDiarioId: number
}): void {
  try {
    const fecha = fechaOperacionLocal()
    localStorage.removeItem(storageKey(args.usuarioId, args.cajaDiarioId, fecha))
    purgeStaleCobroBloqueDrafts()
  } catch {
    /* ignore */
  }
}

export function draftHasMontos(draft: CobroBloqueDraft | null | undefined): boolean {
  if (!draft) return false
  return Object.values(draft.edits).some((e) => (e?.montoPagar ?? 0) > 0)
}

export function formatDraftAge(updatedAt: string): string {
  const t = Date.parse(updatedAt)
  if (!Number.isFinite(t)) return ''
  const mins = Math.max(0, Math.round((Date.now() - t) / 60_000))
  if (mins < 1) return 'hace un momento'
  if (mins < 60) return `hace ${mins} min`
  const h = Math.floor(mins / 60)
  return h === 1 ? 'hace 1 h' : `hace ${h} h`
}
