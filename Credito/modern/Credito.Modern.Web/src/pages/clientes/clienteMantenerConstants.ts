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

/** Celular móvil peruano (9 dígitos, empieza con 9). */
export const REGLA_CELULAR = {
  pattern: /^9\d{8}$/,
  message: 'Celular: 9 dígitos que empiezan con 9',
} as const

/** Obligatorio + formato peruano (p. ej. alta desde Prendario). */
export const REGLA_CELULAR_OBLIGATORIO = [
  { required: true, message: 'Celular obligatorio' },
  REGLA_CELULAR,
] as const
