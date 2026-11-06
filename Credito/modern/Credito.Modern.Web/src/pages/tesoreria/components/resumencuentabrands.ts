import {
  marcaDesdeTipoPagoId as marcaDesdeCatalogo,
  type TipoPagoMarcaVariant,
} from '../../../utils/tipoPago'
import type { ResumenCuentaVariant } from './bovedaResumenCuentaParse'

const base = (import.meta.env.BASE_URL ?? '/').replace(/\/?$/, '/')

export type MedioMarcaTone = 'logo' | 'mark'

/**
 * logo = wordmark oficial (PNG), altura fija.
 * mark = símbolo recortado del mismo archivo, 1:1 para el select.
 */
const LOGO_OFICIAL: ReadonlySet<ResumenCuentaVariant> = new Set([
  'efectivo',
  'yape',
  'plin',
  'interbank',
  'bcp',
  'banco-nacion',
])

export function resumenCuentaLogoSrc(
  variant: ResumenCuentaVariant,
  tone: MedioMarcaTone = 'mark',
): string {
  const oficial = LOGO_OFICIAL.has(variant)
  const folder = tone === 'logo' && oficial ? '' : 'mark/'
  const ext = oficial ? 'png' : 'svg'
  return `${base}assets/resumen-cuenta/${folder}${variant}.${ext}?v=9`
}

/** Marca visual por ItemId — delega al catálogo de `tipoPago.ts`. */
export function marcaDesdeTipoPagoId(tipoPagoId: number): ResumenCuentaVariant | null {
  const v: TipoPagoMarcaVariant | null = marcaDesdeCatalogo(tipoPagoId)
  return v
}
