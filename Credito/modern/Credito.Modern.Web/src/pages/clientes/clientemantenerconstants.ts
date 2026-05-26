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

/** Paridad pattern celular legacy Mantener. */
export const REGLA_CELULAR = {
  pattern: /^\d{1,10}$/,
  message: 'Celular: solo números, máximo 10 dígitos',
} as const
