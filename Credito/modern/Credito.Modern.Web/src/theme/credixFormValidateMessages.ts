/**
 * Mensajes de validación Ant Form en español (ConfigProvider).
 */
export const credixFormValidateMessages = {
  required: 'Complete ${label}',
  whitespace: '${label} no puede estar vacío',
  types: {
    email: '${label} no es un correo válido',
    number: '${label} no es un número válido',
    url: '${label} no es una URL válida',
  },
  number: {
    min: '${label} debe ser ≥ ${min}',
    max: '${label} debe ser ≤ ${max}',
    range: '${label} debe estar entre ${min} y ${max}',
  },
  string: {
    min: '${label} debe tener al menos ${min} caracteres',
    max: '${label} debe tener como máximo ${max} caracteres',
    range: '${label} debe tener entre ${min} y ${max} caracteres',
  },
  pattern: {
    mismatch: '${label} no tiene el formato correcto',
  },
} as const
