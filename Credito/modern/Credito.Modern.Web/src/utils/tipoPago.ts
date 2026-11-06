/**
 * Medios (ValorTabla 13) — multisucursal:
 * - Sucursal: Yape(2), Interbank(3), BCP, Nación, Plin(9), Efectivo
 * - Central: Yape Gerente/René (10/11), Interbank Gerente/René (12/13) en vez de 2/3
 * Familias (totales): Yape = 2+10+11 · Interbank = 3+12+13
 */

export const TIPO_PAGO_EFECTIVO = 1
export const TIPO_PAGO_YAPE = 2
export const TIPO_PAGO_INTERBANK = 3
export const TIPO_PAGO_PLIN = 9
export const TIPO_PAGO_YAPE_GERENTE = 10
export const TIPO_PAGO_YAPE_RENE = 11
export const TIPO_PAGO_INTERBANK_GERENTE = 12
export const TIPO_PAGO_INTERBANK_RENE = 13

export const YAPE_FAMILY_IDS: readonly number[] = [
  TIPO_PAGO_YAPE,
  TIPO_PAGO_YAPE_GERENTE,
  TIPO_PAGO_YAPE_RENE,
]

export const INTERBANK_FAMILY_IDS: readonly number[] = [
  TIPO_PAGO_INTERBANK,
  TIPO_PAGO_INTERBANK_GERENTE,
  TIPO_PAGO_INTERBANK_RENE,
]

export const CENTRAL_WALLET_IDS: readonly number[] = [
  TIPO_PAGO_YAPE_GERENTE,
  TIPO_PAGO_YAPE_RENE,
  TIPO_PAGO_INTERBANK_GERENTE,
  TIPO_PAGO_INTERBANK_RENE,
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

export function esTipoPagoInterbankFamilia(tipoPagoId: number): boolean {
  return INTERBANK_FAMILY_IDS.includes(Number(tipoPagoId))
}

export function esTipoPagoBilleteraCentral(tipoPagoId: number): boolean {
  return CENTRAL_WALLET_IDS.includes(Number(tipoPagoId))
}

/** Genéricos 2/3: en Central se tipan las billeteras, no estos. */
export function esTipoPagoGenericoCentral(tipoPagoId: number): boolean {
  const id = Number(tipoPagoId)
  return id === TIPO_PAGO_YAPE || id === TIPO_PAGO_INTERBANK
}

export function esTipoPagoCobroPermitido(
  tipoPagoId: number,
  esCentral: boolean,
): boolean {
  const id = Number(tipoPagoId)
  if (!Number.isFinite(id) || id < 1) return false
  if (esTipoPagoSedeObsoleto(id)) return false
  if (esTipoPagoBilleteraCentral(id) && !esCentral) return false
  if (esCentral && esTipoPagoGenericoCentral(id)) return false
  return true
}
