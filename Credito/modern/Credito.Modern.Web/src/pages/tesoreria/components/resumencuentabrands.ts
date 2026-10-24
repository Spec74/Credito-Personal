import type { ResumenCuentaVariant } from './bovedaResumenCuentaParse'

const base = (import.meta.env.BASE_URL ?? '/').replace(/\/?$/, '/')

export type MedioMarcaTone = 'logo' | 'mark'

/**
 * logo = wordmark oficial (PNG que pasó el usuario), altura fija.
 * mark = símbolo recortado del mismo archivo oficial, 1:1 para el select.
 */
const LOGO_OFICIAL: ReadonlySet<ResumenCuentaVariant> = new Set([
  'efectivo',
  'yape',
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
  return `${base}assets/resumen-cuenta/${folder}${variant}.${ext}?v=6`
}

export function marcaDesdeTipoPagoId(tipoPagoId: number): ResumenCuentaVariant | null {
  switch (tipoPagoId) {
    case 1:
      return 'efectivo'
    case 2:
      return 'yape'
    case 3:
      return 'interbank'
    case 4:
      return 'bcp'
    case 5:
      return 'banco-nacion'
    default:
      return null
  }
}
