import type { ResumenCuentaVariant } from './bovedaResumenCuentaParse'

const base = (import.meta.env.BASE_URL ?? '/').replace(/\/?$/, '/')

/** Logos en public/assets/resumen-cuenta (marcas Perú). Sustituir SVG por oficiales si el cliente los provee. */
export function resumenCuentaLogoSrc(variant: ResumenCuentaVariant): string {
  return `${base}assets/resumen-cuenta/${variant}.svg`
}
