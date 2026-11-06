/**
 * Medios de pago (MAESTRO.ValorTabla TablaId = 13).
 *
 * Catálogo operativo vigente:
 * 1 Efectivo · 2 Yape · 3 Interbank · 4 BCP · 5 Banco de la Nación
 * (6–8 = variantes por sede obsoletas; no usar en cobro).
 *
 * Regla alineada a los USP: todo TipoPagoId &gt; 1 es digital/transferencia
 * y exige fecha/hora de voucher + MovimientoCajaExtension.
 */

export const TIPO_PAGO_EFECTIVO = 1

/** Efectivo = 1; Yape / bancos / cualquier marca digital = &gt; 1. */
export function esTipoPagoDigital(tipoPagoId: number): boolean {
  return Number(tipoPagoId) > TIPO_PAGO_EFECTIVO
}

/** Variantes legacy por sede (no van al combo ni a cobros nuevos). */
export function esTipoPagoSedeObsoleto(tipoPagoId: number): boolean {
  const id = Number(tipoPagoId)
  return id >= 6 && id <= 8
}

/** Medio aceptable en cobro moderno (efectivo o marca activa 2–5). */
export function esTipoPagoCobroPermitido(tipoPagoId: number): boolean {
  const id = Number(tipoPagoId)
  if (!Number.isFinite(id) || id < 1) return false
  if (esTipoPagoSedeObsoleto(id)) return false
  return true
}
