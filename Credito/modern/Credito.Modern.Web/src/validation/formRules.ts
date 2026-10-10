/**
 * Reglas Ant Design Form reutilizables.
 * Usar en Form.Item rules={...} para validación inmediata + mensajes ES vía ConfigProvider.
 */
import type { Rule } from 'antd/es/form'
import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'

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

/** Clave nueva: mín. 8, al menos una letra y un número (paridad UsuarioValidacion). */
export function mensajeClaveNueva(
  value: string | null | undefined,
  fieldLabel = 'clave',
): string | null {
  const v = (value ?? '').trim()
  if (!v) return `${fieldLabel} es obligatoria`
  if (v.length < 8) return `${fieldLabel} debe tener al menos 8 caracteres`
  if (v.length > 100) return `${fieldLabel}: máximo 100 caracteres`
  if (!/[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]/.test(v) || !/\d/.test(v)) {
    return `${fieldLabel} debe incluir al menos una letra y un número`
  }
  return null
}

export const claveNuevaRule: Rule = {
  validator: async (_, value: string | null | undefined) => {
    const err = mensajeClaveNueva(value)
    if (err) throw new Error(err)
  },
}

/** Obligatoria en alta; en edición vacío = no cambiar. */
export function claveUsuarioRules(requiredOnCreate: boolean): Rule[] {
  if (requiredOnCreate) {
    return [claveNuevaRule]
  }
  return [
    {
      validator: async (_, value: string | null | undefined) => {
        const v = (value ?? '').trim()
        if (!v) return
        const err = mensajeClaveNueva(v)
        if (err) throw new Error(err)
      },
    },
  ]
}

export const denominacionRules = requiredText(FIELD_MAX.denominacion)
export const glosaRules = requiredText(FIELD_MAX.glosa)
export const observacionOptionalRules: Rule[] = [maxLen(FIELD_MAX.observacion)]

/** Edad en años cumplidos (paridad DateRules.EdadEnAnios). */
export function edadEnAnios(birth: Dayjs, onDate: Dayjs = dayjs()): number {
  let age = onDate.year() - birth.year()
  if (birth.isAfter(onDate.subtract(age, 'year'), 'day')) {
    age -= 1
  }
  return age
}

/** Fecha nacimiento: no futura, edad 18–120. */
export function fechaNacimientoRule(minAgeYears = 18): Rule {
  return {
    validator: async (_, value: Dayjs | null | undefined) => {
      if (value == null || !dayjs.isDayjs(value) || !value.isValid()) return
      const hoy = dayjs().startOf('day')
      const birth = value.startOf('day')
      if (birth.isAfter(hoy, 'day')) {
        throw new Error('La fecha de nacimiento no puede ser futura')
      }
      const edad = edadEnAnios(birth, hoy)
      if (edad > 120) {
        throw new Error('Fecha de nacimiento no válida (edad mayor a 120 años)')
      }
      if (edad < minAgeYears) {
        throw new Error(`Debe tener al menos ${minAgeYears} años`)
      }
    },
  }
}

/** Deshabilita en DatePicker fechas futuras y menores de minAge / mayores de 120. */
export function disabledFechaNacimiento(current: Dayjs | null, minAgeYears = 18): boolean {
  if (!current) return false
  const hoy = dayjs().endOf('day')
  if (current.isAfter(hoy)) return true
  const maxBirth = dayjs().subtract(minAgeYears, 'year').endOf('day')
  const minBirth = dayjs().subtract(120, 'year').startOf('day')
  return current.isAfter(maxBirth) || current.isBefore(minBirth)
}

/**
 * Dirección opcional pero realista (paridad StringRules.OptionalDireccionRealista).
 * Vacío = OK; "123" / solo números / demasiado corta = error.
 */
export function mensajeDireccionRealista(
  value: string | null | undefined,
  fieldLabel = 'dirección',
): string | null {
  const v = (value ?? '').trim()
  if (!v) return null
  if (v.length > FIELD_MAX.direccion) {
    return `${fieldLabel}: máximo ${FIELD_MAX.direccion} caracteres`
  }
  if (v.length < 8) {
    return `${fieldLabel} demasiado corta (mínimo 8 caracteres)`
  }
  const letras = (v.match(/[A-Za-zÁÉÍÓÚÜÑáéíóúüñ]/g) ?? []).length
  if (letras < 3) {
    return `${fieldLabel}: incluya texto (calle, jirón, avenida…), no solo números`
  }
  const compact = v.replace(/\s+/g, '')
  if (/^[\d\W_]+$/.test(compact)) {
    return `${fieldLabel} no puede ser solo números o símbolos`
  }
  if (/^(.)\1{4,}$/i.test(compact)) {
    return `${fieldLabel} no parece válida`
  }
  return null
}

export function direccionRealistaRule(fieldLabel = 'dirección'): Rule {
  return {
    validator: async (_, value: string | null | undefined) => {
      const err = mensajeDireccionRealista(value, fieldLabel)
      if (err) throw new Error(err)
    },
  }
}
