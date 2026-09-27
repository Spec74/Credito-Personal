/**
 * Reglas Ant Design Form reutilizables (paridad Application/Validation).
 * Usar en Form.Item rules={...} para validación inmediata + mensajes ES vía ConfigProvider.
 */
import type { Rule } from 'antd/es/form'

export const FIELD_MAX = {
  denominacion: 100,
  nombre: 100,
  glosa: 250,
  observacion: 500,
  usuario: 50,
  email: 120,
  direccion: 250,
  serie: 50,
  celular: 9,
  dni: 8,
  ruc: 11,
} as const

export const required = (message?: string): Rule => ({
  required: true,
  message: message ?? 'Campo obligatorio',
  whitespace: true,
})

export const maxLen = (max: number, message?: string): Rule => ({
  max,
  message: message ?? `Máximo ${max} caracteres`,
})

export const requiredText = (max: number, message?: string): Rule[] => [
  required(message),
  maxLen(max),
]

export const positiveNumber = (message = 'Debe ser mayor a cero'): Rule => ({
  type: 'number',
  min: 0.01,
  message,
})

export const nonNegativeNumber = (message = 'No puede ser negativo'): Rule => ({
  type: 'number',
  min: 0,
  message,
})

export const positiveInt = (message = 'Debe ser un entero ≥ 1'): Rule => ({
  type: 'number',
  min: 1,
  transform: (v) => (v === '' || v === null || v === undefined ? undefined : Number(v)),
  message,
})

export const moneyRequired = (message = 'Importe mayor a cero'): Rule[] => [
  { required: true, message },
  positiveNumber(message),
]

/** DNI peruano 8 dígitos. */
export const dniRule: Rule = {
  pattern: /^\d{8}$/,
  message: 'DNI de 8 dígitos',
}

/** RUC peruano 11 dígitos. */
export const rucRule: Rule = {
  pattern: /^\d{11}$/,
  message: 'RUC de 11 dígitos',
}

/** Celular móvil PE: 9 dígitos que empiezan con 9. */
export const celularPeRule: Rule = {
  pattern: /^9\d{8}$/,
  message: 'Celular: 9 dígitos que empiezan con 9',
}

export const celularPeRequired: Rule[] = [
  { required: true, message: 'Celular obligatorio' },
  celularPeRule,
]

export const emailRule: Rule = {
  type: 'email',
  message: 'Correo no válido',
}

export const denominacionRules = requiredText(FIELD_MAX.denominacion)
export const glosaRules = requiredText(FIELD_MAX.glosa)
export const observacionOptionalRules: Rule[] = [maxLen(FIELD_MAX.observacion)]
