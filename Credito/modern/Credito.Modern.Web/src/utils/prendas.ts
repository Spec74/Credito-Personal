import type { Prenda, PrendaItem } from '../api/creditoGestion'

export function prendaVacia(): PrendaItem {
  return {
    descripcion: '',
    marca: '',
    modelo: '',
    serie: '',
    color: '',
    valorTasacion: 0,
    observaciones: '',
    codigoInterno: '',
  }
}

export const PRENDA_MAX = {
  descripcion: 200,
  marca: 50,
  modelo: 50,
  serie: 50,
  color: 50,
  codigoInterno: 50,
  observaciones: 500,
  valorTasacion: 9_999_999.99,
} as const

/** Solo cuentan los bienes con descripción: los renglones vacíos del formulario se descartan. */
export function prendasValidas(prendas: PrendaItem[]): PrendaItem[] {
  return prendas.filter((p) => p.descripcion.trim().length > 0 && p.valorTasacion > 0)
}

export function totalTasacion(prendas: PrendaItem[]): number {
  return prendasValidas(prendas).reduce((acc, p) => acc + p.valorTasacion, 0)
}

export type PrendaCampoError = {
  indice: number
  campo: keyof PrendaItem | 'general'
  mensaje: string
}

export type ValidacionPrendasResult = {
  ok: boolean
  bienes: PrendaItem[]
  mensaje: string | null
  errores: PrendaCampoError[]
}

function tieneMasDeDosDecimales(valor: number): boolean {
  return Math.round(valor * 100) / 100 !== valor
}

/**
 * Validación de formulario de bienes (paridad backend PrendarioValidacion).
 * Los renglones sin descripción se ignoran; al menos uno debe ser válido.
 */
export function validarPrendasForm(prendas: PrendaItem[]): ValidacionPrendasResult {
  const errores: PrendaCampoError[] = []
  const conDescripcion = prendas
    .map((p, indice) => ({ p, indice }))
    .filter(({ p }) => p.descripcion.trim().length > 0)

  if (conDescripcion.length === 0) {
    const mensaje = 'Registre al menos un bien con descripción y tasación mayor a cero.'
    return {
      ok: false,
      bienes: [],
      mensaje,
      errores: [{ indice: 0, campo: 'descripcion', mensaje: 'Descripción obligatoria' }],
    }
  }

  for (const { p, indice } of conDescripcion) {
    const n = indice + 1
    const desc = p.descripcion.trim()
    if (desc.length > PRENDA_MAX.descripcion) {
      errores.push({
        indice,
        campo: 'descripcion',
        mensaje: `Máximo ${PRENDA_MAX.descripcion} caracteres`,
      })
    }
    if (!(p.valorTasacion > 0)) {
      errores.push({
        indice,
        campo: 'valorTasacion',
        mensaje: 'Tasación mayor a cero',
      })
    } else if (p.valorTasacion > PRENDA_MAX.valorTasacion) {
      errores.push({
        indice,
        campo: 'valorTasacion',
        mensaje: 'Tasación supera el máximo',
      })
    } else if (tieneMasDeDosDecimales(p.valorTasacion)) {
      errores.push({
        indice,
        campo: 'valorTasacion',
        mensaje: 'Máximo 2 decimales',
      })
    }

    const opcionales: Array<[keyof PrendaItem, number]> = [
      ['marca', PRENDA_MAX.marca],
      ['modelo', PRENDA_MAX.modelo],
      ['serie', PRENDA_MAX.serie],
      ['color', PRENDA_MAX.color],
      ['codigoInterno', PRENDA_MAX.codigoInterno],
      ['observaciones', PRENDA_MAX.observaciones],
    ]
    for (const [campo, max] of opcionales) {
      const raw = p[campo]
      if (typeof raw === 'string' && raw.trim().length > max) {
        errores.push({ indice, campo, mensaje: `Máximo ${max} caracteres` })
      }
    }

    if (errores.some((e) => e.indice === indice)) {
      // mensaje agregado por campo; el resumen usa el primero
      void n
    }
  }

  if (errores.length > 0) {
    const primero = errores[0]
    return {
      ok: false,
      bienes: [],
      mensaje: `Bien #${primero.indice + 1}: ${primero.mensaje}`,
      errores,
    }
  }

  const bienes = conDescripcion.map(({ p }) => ({
    ...p,
    descripcion: p.descripcion.trim(),
  }))
  return { ok: true, bienes, mensaje: null, errores: [] }
}

export function errorCampoPrenda(
  errores: PrendaCampoError[],
  indice: number,
  campo: keyof PrendaItem,
): string | undefined {
  return errores.find((e) => e.indice === indice && e.campo === campo)?.mensaje
}

/** Celular móvil peruano: 9 dígitos que empiezan con 9. */
export function esCelularPeruano(celular: string | null | undefined): boolean {
  const digits = (celular ?? '').replace(/\D/g, '')
  return digits.length === 9 && digits.startsWith('9')
}

export function validarMontoVsTasacion(montoCredito: number, montoTasacion: number): string | null {
  if (!(montoCredito > 0)) {
    return 'El monto del crédito debe ser mayor a cero'
  }
  if (!(montoTasacion > 0)) {
    return 'Debe registrar bienes con tasación antes de continuar'
  }
  if (montoCredito > montoTasacion) {
    return `El monto del crédito no puede superar la tasación (S/ ${montoTasacion.toFixed(2)})`
  }
  return null
}

export function prendaAItem(prenda: Prenda): PrendaItem {
  return {
    descripcion: prenda.descripcion,
    marca: prenda.marca,
    modelo: prenda.modelo,
    serie: prenda.serie,
    color: prenda.color,
    valorTasacion: prenda.valorTasacion,
    observaciones: prenda.observaciones,
    codigoInterno: prenda.codigoInterno,
  }
}

export type PrendaSimuladorPrecarga = {
  descripcion: string
  montoTasacion: number
  fechaRemate: string
  observacion: string | null
}

/** Une los bienes del formulario para el simulador (descripción, tasación, remate). */
export function prendaSimuladorDesdeBienes(
  prendas: PrendaItem[],
  fechaRemate?: string | null,
): PrendaSimuladorPrecarga | null {
  const bienes = prendasValidas(prendas)
  if (bienes.length === 0) {
    return null
  }

  const remate = (fechaRemate ?? '').trim().slice(0, 10) || fechaRematePorDefecto()
  const observacion =
    bienes
      .map((p) => p.observaciones?.trim())
      .filter((v): v is string => Boolean(v))
      .join(' / ') || null

  return {
    descripcion: bienes.map((p) => p.descripcion.trim()).join(' / '),
    montoTasacion: totalTasacion(prendas),
    fechaRemate: remate,
    observacion,
  }
}

export function buildSimuladorPrendarioPath(opts: {
  personaId: number
  solicitudCreditoId: number
  prendas: PrendaItem[]
  fechaRemate?: string | null
}): string {
  const q = new URLSearchParams({
    personaId: String(opts.personaId),
    solicitudCreditoId: String(opts.solicitudCreditoId),
    productoId: '2',
  })
  const pre = prendaSimuladorDesdeBienes(opts.prendas, opts.fechaRemate)
  if (pre) {
    q.set('prendaDescripcion', pre.descripcion)
    q.set('prendaMontoTasacion', String(pre.montoTasacion))
    q.set('prendaFechaRemate', pre.fechaRemate)
    if (pre.observacion) {
      q.set('prendaObservacion', pre.observacion)
    }
  }
  return `/credito/simulador?${q.toString()}`
}

function fechaRematePorDefecto(): string {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  d.setMonth(d.getMonth() + 1)
  d.setDate(d.getDate() + 30)
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}
