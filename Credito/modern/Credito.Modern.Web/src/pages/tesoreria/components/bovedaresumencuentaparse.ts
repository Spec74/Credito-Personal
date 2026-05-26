/** Resultado de parsear el texto de CREDITO.usp_ResumenCuentaBoveda */
export type ResumenCuentaItem = {
  clave: string
  etiqueta: string
  monto: number
  variant: ResumenCuentaVariant
}

export type ResumenCuentaParsed = {
  titulo?: string
  items: ResumenCuentaItem[]
  textoPlano?: string
}

export type ResumenCuentaVariant =
  | 'efectivo'
  | 'yape'
  | 'plin'
  | 'interbank'
  | 'bcp'
  | 'banco-nacion'
  | 'bbva'
  | 'scotiabank'
  | 'transferencia'
  | 'otro'

const RE_LINEA = /([A-ZÁÉÍÓÚÑ][A-ZÁÉÍÓÚÑ0-9\s./-]*?)\s*=\s*(-?\d+(?:\.\d+)?)/gi

function normalizarClave(raw: string): string {
  return raw.trim().replace(/\s+/g, ' ').toUpperCase()
}

function resolverVariant(clave: string): { etiqueta: string; variant: ResumenCuentaVariant } {
  const k = normalizarClave(clave)

  if (k === 'EFECTIVO' || k.includes('EFECTIVO') || k === 'CASH') {
    return { etiqueta: 'Efectivo', variant: 'efectivo' }
  }
  if (k.includes('YAPE')) {
    return { etiqueta: 'Yape', variant: 'yape' }
  }
  if (k.includes('PLIN')) {
    return { etiqueta: 'Plin', variant: 'plin' }
  }
  if (k.includes('INTERBANK')) {
    return { etiqueta: 'Interbank', variant: 'interbank' }
  }
  if (
    k.includes('BCO CREDITO') ||
    k.includes('BANCO CREDITO') ||
    k === 'BCP' ||
    k.includes('BCP')
  ) {
    return { etiqueta: 'BCP', variant: 'bcp' }
  }
  if (k.includes('NACION') || k.includes('BN')) {
    return { etiqueta: 'Banco de la Nación', variant: 'banco-nacion' }
  }
  if (k.includes('BBVA')) {
    return { etiqueta: 'BBVA', variant: 'bbva' }
  }
  if (k.includes('SCOTIA')) {
    return { etiqueta: 'Scotiabank', variant: 'scotiabank' }
  }
  if (k.includes('TRANSFER') || k.includes('TRANSF')) {
    return { etiqueta: clave.trim(), variant: 'transferencia' }
  }

  const etiqueta = clave
    .trim()
    .toLowerCase()
    .replace(/\b\w/g, (c) => c.toUpperCase())
  return { etiqueta, variant: 'otro' }
}

/**
 * Parsea texto tipo:
 * `RESUMEN BOVEDA: EFECTIVO = 1166477.21  YAPE = 405491.64 ...`
 */
export function parseResumenBovedaTexto(texto: string): ResumenCuentaParsed {
  const trimmed = texto.trim()
  if (!trimmed) {
    return { items: [] }
  }

  const items: ResumenCuentaItem[] = []
  let match: RegExpExecArray | null
  const re = new RegExp(RE_LINEA.source, RE_LINEA.flags)

  while ((match = re.exec(trimmed)) !== null) {
    const clave = match[1].trim()
    const monto = Number.parseFloat(match[2])
    if (!Number.isFinite(monto)) continue
    const { etiqueta, variant } = resolverVariant(clave)
    items.push({ clave: normalizarClave(clave), etiqueta, monto, variant })
  }

  if (items.length > 0) {
    const tituloMatch = trimmed.match(/^([^=]+?):/i)
    const titulo = tituloMatch?.[1]?.trim()
    return { titulo, items }
  }

  return { items: [], textoPlano: trimmed }
}
