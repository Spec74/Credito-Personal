/** Tablas MAESTRO.ValorTabla — mismos IDs que legacy Mantener.cshtml. */
export const TABLA_ESTADO_CIVIL = 11
export const TABLA_TIPO_VIVIENDA = 12
export const TABLA_RIESGO_SBS = 14

/** Casado / conviviente — obligatorio cónyuge (legacy p_EstadoCivilId 2 y 3). */
export const ESTADO_CIVIL_CONYUGE = new Set([2, 3])

export const CALIFICACIONES = [
  { value: 'A', label: 'A' },
  { value: 'B', label: 'B' },
  { value: 'C', label: 'C' },
] as const

/** @deprecated Usar celularPeRule / celularPeRequired de validation/formRules. */
export { celularPeRule as REGLA_CELULAR, celularPeRequired as REGLA_CELULAR_OBLIGATORIO } from '../../validation/formRules'
