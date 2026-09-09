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
