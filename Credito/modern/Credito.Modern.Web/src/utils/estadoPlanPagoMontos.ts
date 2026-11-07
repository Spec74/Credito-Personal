import { roundMoney, sumMoney } from './money'

type PlanCuotaMontos = {
  estado: string | null | undefined
  pagoCuota: number | null | undefined
  pagoLibre: number | null | undefined
  cuota?: number | null
  importeMora?: number | null
  cargo?: number | null
  capital?: number | null
}

/**
 * Importe realmente cobrado en la cuota.
 * En `usp_EstadoPlanPago`, `pagoCuota` en PEN es saldo a pagar (CuotasPendientes),
 * no lo pagado; lo abonado parcial vive en `pagoLibre`.
 */
export function importePagadoPlanCuota(row: PlanCuotaMontos): number {
  const est = row.estado?.trim().toUpperCase()
  if (est === 'PAG') return roundMoney(row.pagoCuota ?? 0)
  return roundMoney(row.pagoLibre ?? 0)
}

export function totalPagadoPlanCuotas(rows: PlanCuotaMontos[]): number {
  return sumMoney(rows.map(importePagadoPlanCuota))
}

/** Saldo pendiente (PEN: pagoCuota del USP ya incluye mora/cargos). */
export function saldoPorPagarPlanCuota(row: PlanCuotaMontos): number {
  const est = row.estado?.trim().toUpperCase()
  if (est === 'PAG') return 0
  if (row.pagoCuota != null) return roundMoney(Math.max(0, row.pagoCuota))
  const estimado =
    (row.cuota ?? 0) - (row.pagoLibre ?? 0) + (row.importeMora ?? 0) + (row.cargo ?? 0)
  return roundMoney(Math.max(0, estimado))
}

export function totalSaldoPorPagarPlanCuotas(rows: PlanCuotaMontos[]): number {
  return sumMoney(rows.map(saldoPorPagarPlanCuota))
}
