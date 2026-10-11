import type { CreditoGestorPendienteRow } from '../api/cajaDiario'
import { hasValidCoordinates } from '../config/googleMaps'

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

function optionalCoord(raw: unknown): number | null {
  if (raw == null || raw === '') return null
  const n = Number(raw)
  return Number.isFinite(n) ? n : null
}

/** Monto sugerido desde informe cobro diario (cuota del día + mora). */
export function sugeridaFromInforme(row: {
  cuotaTotal?: number | null
  cuotaPlan?: number | null
  mora?: number | null
}): number {
  const cuota = numOr(row.cuotaTotal, 0) || numOr(row.cuotaPlan, 0)
  const mora = Math.max(0, numOr(row.mora, 0))
  return Math.max(0, cuota + mora)
}

/** Acepta camelCase o PascalCase (por si el host serializa distinto). */
export function normalizeCreditoGestorPendienteRow(
  raw: Record<string, unknown>,
): CreditoGestorPendienteRow {
  const latitud = optionalCoord(raw.latitud ?? raw.Latitud)
  const longitud = optionalCoord(raw.longitud ?? raw.Longitud)
  const gpsOk = hasValidCoordinates(latitud, longitud)

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
    latitud: gpsOk ? latitud : null,
    longitud: gpsOk ? longitud : null,
    cuotaSugerida: numOr(raw.cuotaSugerida ?? raw.CuotaSugerida),
    diasAtrazo: numOr(raw.diasAtrazo ?? raw.DiasAtrazo),
  }
}

export function mergeContactoFromInforme(
  rows: CreditoGestorPendienteRow[],
  informe: Array<{
    creditoId: number
    celular?: string | null
    direccion?: string | null
    cuotaTotal?: number | null
    cuotaPlan?: number | null
    mora?: number | null
  }>,
): CreditoGestorPendienteRow[] {
  if (!informe.length) return rows
  const byCredito = new Map(
    informe.map((r) => [
      r.creditoId,
      {
        celular: pickNonEmpty(r.celular),
        direccion: pickNonEmpty(r.direccion),
        sugerida: sugeridaFromInforme(r),
      },
    ]),
  )
  return rows.map((r) => {
    const extra = byCredito.get(r.creditoId)
    if (!extra) return r
    const cuotaSugerida =
      r.cuotaSugerida > 0
        ? r.cuotaSugerida
        : extra.sugerida > 0
          ? Math.min(extra.sugerida, r.deudaPendiente > 0 ? r.deudaPendiente : extra.sugerida)
          : r.cuotaSugerida
    return {
      ...r,
      celular: r.celular ?? extra.celular,
      direccion: r.direccion ?? extra.direccion,
      cuotaSugerida,
    }
  })
}
