import type { CuotasPendientesRow } from '../../types/api'
import type { CreditoMoraRow } from '../../api/cajaDiario'
import { getCuotaRowVariant, isCuotaSelectable } from './cuotaRowStyle'

/** Mora calculada en cuotas aún no registrada en CreditoMora (hasta el pago con atraso). */
export type MoraVigenteCuotaRow = {
  key: string
  planPagoId: number
  glosa: string
  fechaVencimiento: string | null
  diasAtrazo: number
  importeMora: number
  aPagar: number
}

export function extraerMorasVigentesEnCuotas(
  cuotas: CuotasPendientesRow[],
): MoraVigenteCuotaRow[] {
  return cuotas
    .filter((c) => {
      if (!isCuotaSelectable(c)) {
        return false
      }
      const variant = getCuotaRowVariant(c)
      return (
        variant === 'mora' ||
        (c.importeMora ?? 0) > 0 ||
        (c.diasAtrazo ?? 0) > 0
      )
    })
    .map((c) => ({
      key: `vig-${c.planPagoId}`,
      planPagoId: c.planPagoId!,
      glosa: c.glosa ?? `Cuota ${c.planPagoId}`,
      fechaVencimiento: c.fechaVencimiento,
      diasAtrazo: c.diasAtrazo ?? 0,
      importeMora: c.importeMora ?? 0,
      aPagar: c.pagoCuota ?? c.cuota ?? 0,
    }))
}

export function sumarMoraVigente(cuotas: CuotasPendientesRow[]): number {
  return extraerMorasVigentesEnCuotas(cuotas).reduce(
    (s, r) => s + r.importeMora,
    0,
  )
}

export function sumarSaldoPostergadoHistorial(rows: CreditoMoraRow[]): number {
  return rows
    .filter((r) => r.movimientoCajaId == null || r.movimientoCajaId === 0)
    .reduce((s, r) => s + (r.saldoMora ?? 0), 0)
}
