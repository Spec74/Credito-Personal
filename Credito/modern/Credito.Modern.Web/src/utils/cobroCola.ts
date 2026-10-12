/**
 * Cola del día para Cobro campo (/caja/cobro).
 *
 * - Tipar montos ≠ cobrado (viven en borrador / edits).
 * - «Por enviar» se deriva de tipados con monto &gt; 0.
 * - «Enviados» se persiste local tras cobro exitoso (hoy + caja).
 * - Errores de sync quedan visibles sin borrar el tipado.
 */

import { fechaOperacionLocal } from './cobroBloqueDraft'

export type CobroEnviadoItem = {
  creditoId: number
  personaNombre: string
  montoPagar: number
  tipoPagoId: number
  enviadoAt: string
  movimientoCajaId?: number | null
  origen: 'cobrar_ya' | 'lote' | 'sync'
}

export type CobroColaDia = {
  version: 1
  usuarioId: number
  oficinaId: number
  cajaDiarioId: number
  fechaOperacion: string
  enviados: CobroEnviadoItem[]
  /** Mensaje de error por creditoId (último intento fallido). */
  errores: Record<string, string>
}

const STORAGE_PREFIX = 'credix.cobroCola.v1'

function storageKey(usuarioId: number, cajaDiarioId: number, fecha: string) {
  return `${STORAGE_PREFIX}:${usuarioId}:${cajaDiarioId}:${fecha}`
}

function parseJson<T>(raw: string | null): T | null {
  if (!raw) return null
  try {
    return JSON.parse(raw) as T
  } catch {
    return null
  }
}

function purgeOtherDays(keepKey: string): void {
  try {
    const remove: string[] = []
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i)
      if (!k?.startsWith(STORAGE_PREFIX)) continue
      if (k === keepKey) continue
      remove.push(k)
    }
    for (const k of remove) localStorage.removeItem(k)
  } catch {
    /* ignore */
  }
}

function emptyCola(args: {
  usuarioId: number
  oficinaId: number
  cajaDiarioId: number
  fechaOperacion: string
}): CobroColaDia {
  return {
    version: 1,
    usuarioId: args.usuarioId,
    oficinaId: args.oficinaId,
    cajaDiarioId: args.cajaDiarioId,
    fechaOperacion: args.fechaOperacion,
    enviados: [],
    errores: {},
  }
}

export function loadCobroCola(args: {
  usuarioId: number
  oficinaId: number
  cajaDiarioId: number
}): CobroColaDia {
  if (args.usuarioId < 1 || args.cajaDiarioId < 1) {
    return emptyCola({
      ...args,
      fechaOperacion: fechaOperacionLocal(),
    })
  }
  const fecha = fechaOperacionLocal()
  const key = storageKey(args.usuarioId, args.cajaDiarioId, fecha)
  try {
    purgeOtherDays(key)
    const data = parseJson<CobroColaDia>(localStorage.getItem(key))
    if (
      !data ||
      data.version !== 1 ||
      data.usuarioId !== args.usuarioId ||
      data.cajaDiarioId !== args.cajaDiarioId ||
      data.fechaOperacion !== fecha ||
      !Array.isArray(data.enviados)
    ) {
      return emptyCola({ ...args, fechaOperacion: fecha })
    }
    return {
      ...data,
      oficinaId: args.oficinaId || data.oficinaId,
      errores: data.errores && typeof data.errores === 'object' ? data.errores : {},
    }
  } catch {
    return emptyCola({ ...args, fechaOperacion: fecha })
  }
}

export function saveCobroCola(cola: CobroColaDia): void {
  try {
    const key = storageKey(cola.usuarioId, cola.cajaDiarioId, cola.fechaOperacion)
    purgeOtherDays(key)
    localStorage.setItem(key, JSON.stringify(cola))
  } catch {
    /* quota */
  }
}

export function markCobroEnviados(
  args: {
    usuarioId: number
    oficinaId: number
    cajaDiarioId: number
  },
  items: Omit<CobroEnviadoItem, 'enviadoAt'>[],
): CobroColaDia {
  const cola = loadCobroCola(args)
  const now = new Date().toISOString()
  const sentIds = new Set(items.map((i) => i.creditoId))
  const keep = cola.enviados.filter((e) => !sentIds.has(e.creditoId))
  const nextErrores = { ...cola.errores }
  for (const id of sentIds) delete nextErrores[String(id)]
  const next: CobroColaDia = {
    ...cola,
    oficinaId: args.oficinaId || cola.oficinaId,
    enviados: [
      ...items.map((i) => ({ ...i, enviadoAt: now })),
      ...keep,
    ],
    errores: nextErrores,
  }
  saveCobroCola(next)
  return next
}

export function setCobroColaError(
  args: {
    usuarioId: number
    oficinaId: number
    cajaDiarioId: number
  },
  creditoId: number,
  mensaje: string,
): CobroColaDia {
  const cola = loadCobroCola(args)
  const next: CobroColaDia = {
    ...cola,
    oficinaId: args.oficinaId || cola.oficinaId,
    errores: { ...cola.errores, [String(creditoId)]: mensaje },
  }
  saveCobroCola(next)
  return next
}

export function clearCobroColaError(
  args: {
    usuarioId: number
    oficinaId: number
    cajaDiarioId: number
  },
  creditoId: number,
): CobroColaDia {
  const cola = loadCobroCola(args)
  if (!(String(creditoId) in cola.errores)) return cola
  const nextErrores = { ...cola.errores }
  delete nextErrores[String(creditoId)]
  const next: CobroColaDia = { ...cola, errores: nextErrores }
  saveCobroCola(next)
  return next
}

export function clearCobroColaDia(args: {
  usuarioId: number
  cajaDiarioId: number
}): void {
  try {
    const fecha = fechaOperacionLocal()
    localStorage.removeItem(storageKey(args.usuarioId, args.cajaDiarioId, fecha))
  } catch {
    /* ignore */
  }
}

export function isCreditoEnviadoHoy(
  cola: CobroColaDia,
  creditoId: number,
): boolean {
  return cola.enviados.some((e) => e.creditoId === creditoId)
}
