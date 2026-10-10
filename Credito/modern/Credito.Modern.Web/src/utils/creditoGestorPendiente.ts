import type { CreditoGestorPendienteRow } from '../api/cajaDiario'

function pickNonEmpty(...vals: unknown[]): string | null {
  for (const v of vals) {
    if (typeof v === 'string' && v.trim()) return v.trim()
  }
  return null
}

function numOr(raw: unknown, fallback = 0): number {
  const n = Number(raw)
  return Number.isFinite(n) ? n : fallback
}

/** Acepta camelCase o PascalCase (por si el host serializa distinto). */
export function normalizeCreditoGestorPendienteRow(
  raw: Record<string, unknown>,
): CreditoGestorPendienteRow {
  return {
    creditoId: numOr(raw.creditoId ?? raw.CreditoId),
    personaCodigo: String(raw.personaCodigo ?? raw.PersonaCodigo ?? ''),
    personaNombre: String(raw.personaNombre ?? raw.PersonaNombre ?? ''),
    montoCredito: numOr(raw.montoCredito ?? raw.MontoCredito),
    personaId: numOr(raw.personaId ?? raw.PersonaId),
    fechaVencimiento: String(raw.fechaVencimiento ?? raw.FechaVencimiento ?? ''),
    importeMora: numOr(raw.importeMora ?? raw.ImporteMora),
    deudaPendiente: numOr(raw.deudaPendiente ?? raw.DeudaPendiente),
    orden: (() => {
      const o = raw.orden ?? raw.Orden
      if (o == null || o === '') return null
      const n = Number(o)
      return Number.isFinite(n) ? n : null
    })(),
    celular: pickNonEmpty(raw.celular, raw.Celular),
    direccion: pickNonEmpty(raw.direccion, raw.Direccion),
    cuotaSugerida: numOr(raw.cuotaSugerida ?? raw.CuotaSugerida),
    diasAtrazo: numOr(raw.diasAtrazo ?? raw.DiasAtrazo),
  }
}

export function mergeContactoFromInforme(
  rows: CreditoGestorPendienteRow[],
  informe: Array<{ creditoId: number; celular?: string | null; direccion?: string | null }>,
): CreditoGestorPendienteRow[] {
  if (!informe.length) return rows
  const byCredito = new Map(
    informe.map((r) => [
      r.creditoId,
      {
        celular: pickNonEmpty(r.celular),
        direccion: pickNonEmpty(r.direccion),
      },
    ]),
  )
  return rows.map((r) => {
    const extra = byCredito.get(r.creditoId)
    if (!extra) return r
    return {
      ...r,
      celular: r.celular ?? extra.celular,
      direccion: r.direccion ?? extra.direccion,
    }
  })
}
