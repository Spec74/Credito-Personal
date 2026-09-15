import { extractSearchTermsFromInput } from '../components/caja/clienteBuscarResolve'

export type AprobarBuscarEstado = {
  /** Texto enviado al API (vacío = listar todos los PEN). */
  aplicado: string
  /** Términos detectados para mostrar en la UI. */
  terminos: string[]
  /** Menos de 2 letras y no es solo número de crédito/DNI. */
  terminoCorto: boolean
}

/** Normaliza entrada de búsqueda de la bandeja aprobar (paridad potente vs solo nombre legacy). */
export function resolveAprobarBuscar(raw: string): AprobarBuscarEstado {
  const termino = raw.trim()
  if (!termino) {
    return { aplicado: '', terminos: [], terminoCorto: false }
  }

  const terminos = extractSearchTermsFromInput(termino)
  const soloDigitos = /^\d+$/.test(termino.replace(/\s+/g, ''))
  const terminoCorto = !soloDigitos && termino.length < 2

  return {
    aplicado: terminoCorto ? '' : termino,
    terminos,
    terminoCorto,
  }
}
