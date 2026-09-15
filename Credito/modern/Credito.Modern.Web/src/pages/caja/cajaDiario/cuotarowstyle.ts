import type { CuotasPendientesRow } from '../../../types/api'

/** Variantes de fila (paridad CSS legacy: field-pagado, field-pendiente, field-error). */
export type CuotaRowVariant =
  | 'pagada'
  | 'pendiente'
  | 'mora'
  | 'creada'
  | 'resumen'

export function getCuotaRowVariant(row: CuotasPendientesRow): CuotaRowVariant {
  const planId = row.planPagoId
  const glosa = (row.glosa ?? '').trim().toUpperCase()

  if (planId != null && planId <= 0) {
    if (planId === -1 || glosa.includes('PAGADO')) {
      return 'pagada'
    }
    return 'resumen'
  }

  if (glosa.includes('PAGADO')) {
    return 'pagada'
  }
  if (glosa.includes('TOTAL') || glosa.includes('PENDIENTE:')) {
    return 'resumen'
  }
  if (glosa.includes('CREADO') || glosa.startsWith('CRE ')) {
    return 'creada'
  }

  const mora = (row.importeMora ?? 0) > 0
  const atraso = (row.diasAtrazo ?? 0) > 0
  if (mora || atraso) {
    return 'mora'
  }

  return 'pendiente'
}

export function cuotaRowClassName(row: CuotasPendientesRow): string {
  return `caja-cuota-row caja-cuota-row--${getCuotaRowVariant(row)}`
}

export function isCuotaSelectable(row: CuotasPendientesRow): boolean {
  const planId = row.planPagoId
  if (planId == null || planId <= 0) {
    return false
  }
  return getCuotaRowVariant(row) !== 'resumen'
}

export function cuotaRowKey(row: CuotasPendientesRow, index: number): string {
  if (row.planPagoId != null && row.planPagoId > 0) {
    return `plan-${row.planPagoId}`
  }
  return `summary-${index}-${row.glosa ?? ''}`
}
