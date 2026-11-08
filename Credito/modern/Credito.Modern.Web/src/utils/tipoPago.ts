/**
 * Dominio medios (ValorTabla TablaId=13) — fuente única FE.
 * Paridad con Credito.Modern.Application.CreditoPlanes.TipoPagoRules.
 *
 * Sucursal tipa: Yape(2), Interbank(3), BCP, Nación, Plin(9), Efectivo.
 * Central tipa: billeteras Gerente/René (10–13); no genéricos 2/3.
 * Vista red Central: padre Yape/Interbank = rollup familia; 10–13 = detalle billetera.
 * Fondeo billetera→sucursal: 10/11→2 y 12/13→3.
 */

export const TIPO_PAGO_EFECTIVO = 1
export const TIPO_PAGO_YAPE = 2
export const TIPO_PAGO_INTERBANK = 3
export const TIPO_PAGO_BCP = 4
export const TIPO_PAGO_NACION = 5
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

/** Variante visual (logo/mark). Misma semántica que ResumenCuentaVariant. */
export type TipoPagoMarcaVariant =
  | 'efectivo'
  | 'yape'
  | 'plin'
  | 'interbank'
  | 'bcp'
  | 'banco-nacion'
  | 'bbva'
  | 'scotiabank'
  | 'transferencia'
  | 'otro'

type TipoPagoCatalogEntry = {
  nombre: string
  variant: TipoPagoMarcaVariant
  /** Orden estable en combos/chips. */
  orden: number
}

/** Catálogo tipable conocido — nombres amigables independientes del texto SQL. */
export const TIPO_PAGO_CATALOGO: Readonly<Record<number, TipoPagoCatalogEntry>> = {
  [TIPO_PAGO_EFECTIVO]: { nombre: 'Efectivo', variant: 'efectivo', orden: 10 },
  [TIPO_PAGO_YAPE_GERENTE]: { nombre: 'Yape Gerente', variant: 'yape', orden: 20 },
  [TIPO_PAGO_YAPE_RENE]: { nombre: 'Yape René', variant: 'yape', orden: 21 },
  [TIPO_PAGO_YAPE]: { nombre: 'Yape', variant: 'yape', orden: 22 },
  [TIPO_PAGO_INTERBANK_GERENTE]: {
    nombre: 'Interbank Gerente',
    variant: 'interbank',
    orden: 30,
  },
  [TIPO_PAGO_INTERBANK_RENE]: { nombre: 'Interbank René', variant: 'interbank', orden: 31 },
  [TIPO_PAGO_INTERBANK]: { nombre: 'Interbank', variant: 'interbank', orden: 32 },
  [TIPO_PAGO_BCP]: { nombre: 'BCP', variant: 'bcp', orden: 40 },
  [TIPO_PAGO_NACION]: { nombre: 'Banco de la Nación', variant: 'banco-nacion', orden: 50 },
  [TIPO_PAGO_PLIN]: { nombre: 'Plin', variant: 'plin', orden: 60 },
}

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

/** Regla de tipado (cobro / ingreso / transferencia) según sede. */
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

/** Nombre UI estable por ItemId (chips, combos, aria). */
export function medioNombrePorItemId(itemId: number): string | null {
  return TIPO_PAGO_CATALOGO[itemId]?.nombre ?? null
}

export function marcaDesdeTipoPagoId(tipoPagoId: number): TipoPagoMarcaVariant | null {
  return TIPO_PAGO_CATALOGO[tipoPagoId]?.variant ?? null
}

export function ordenTipoPago(tipoPagoId: number): number {
  return TIPO_PAGO_CATALOGO[tipoPagoId]?.orden ?? 100 + tipoPagoId
}

/**
 * ¿Aparece en combo/chips?
 * Combina tipado de sede + metadatos de catálogo (OBSOLETO / sufijo sede legado).
 */
export function esMarcaTipoPagoCatalogo(
  item: {
    itemId: number
    denominacion: string
    desCorta?: string | null
  },
  esCentral = false,
): boolean {
  if (item.itemId < 1) return false
  const dc = (item.desCorta ?? '').trim().toUpperCase()
  if (dc === 'OBSOLETO') return false
  // Sufijo sede en denominación (legado), no la bandera DesCorta=CENTRAL.
  if (/\s+(HUANTA|PICHARI|ANDAHUAYLAS|HUAMANGA|CENTRAL)\s*$/i.test(item.denominacion.trim())) {
    return false
  }
  if (!esCentral && dc === 'CENTRAL') return false
  return esTipoPagoCobroPermitido(item.itemId, esCentral)
}
