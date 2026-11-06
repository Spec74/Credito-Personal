/**
 * Medios de pago (MAESTRO.ValorTabla TablaId = 13).
 *
 * 1 Efectivo · 2 Yape (ROLLUP) · 3 Interbank · 4 BCP · 5 Nación · 9 Plin
 * 10 Yape Gerente · 11 Yape René
 * 6–8 sede obsoleta.
 *
 * Postable = cobro/fondeo. Digital (&gt;1) exige voucher.
 */

export const TIPO_PAGO_EFECTIVO = 1
export const TIPO_PAGO_YAPE = 2
export const TIPO_PAGO_PLIN = 9
export const TIPO_PAGO_YAPE_GERENTE = 10
export const TIPO_PAGO_YAPE_RENE = 11

export const YAPE_FAMILY_IDS: readonly number[] = [
  TIPO_PAGO_YAPE,
  TIPO_PAGO_YAPE_GERENTE,
  TIPO_PAGO_YAPE_RENE,
]

export function esTipoPagoDigital(tipoPagoId: number): boolean {
  return Number(tipoPagoId) > TIPO_PAGO_EFECTIVO
}

export function esTipoPagoSedeObsoleto(tipoPagoId: number): boolean {
  const id = Number(tipoPagoId)
  return id >= 6 && id <= 8
}

export function esTipoPagoYapeFamilia(tipoPagoId: number): boolean {
  return YAPE_FAMILY_IDS.includes(Number(tipoPagoId))
}

/** Padre rollup (ItemId 2): no se tipa en cobro/fondeo. */
export function esTipoPagoRollup(tipoPagoId: number): boolean {
  return Number(tipoPagoId) === TIPO_PAGO_YAPE
}

export function esTipoPagoCobroPermitido(tipoPagoId: number): boolean {
  const id = Number(tipoPagoId)
  if (!Number.isFinite(id) || id < 1) return false
  if (esTipoPagoSedeObsoleto(id)) return false
  if (esTipoPagoRollup(id)) return false
  return true
}
