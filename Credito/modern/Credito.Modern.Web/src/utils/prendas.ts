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

/** Solo cuentan los bienes con descripción: los renglones vacíos del formulario se descartan. */
export function prendasValidas(prendas: PrendaItem[]): PrendaItem[] {
  return prendas.filter((p) => p.descripcion.trim().length > 0 && p.valorTasacion > 0)
}

export function totalTasacion(prendas: PrendaItem[]): number {
  return prendasValidas(prendas).reduce((acc, p) => acc + p.valorTasacion, 0)
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
