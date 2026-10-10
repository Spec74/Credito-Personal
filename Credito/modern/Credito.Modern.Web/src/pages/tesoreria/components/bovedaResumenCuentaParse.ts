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

/** Sufijo de sede/cuenta cuando el SP distingue Central vs Huanta. */
function sufijoSede(clave: string): string {
  const k = normalizarClave(clave)
  if (k.includes('HUANTA')) return ' Huanta'
  if (k.includes('CENTRAL')) return ' Central'
  return ''
}

/**
 * Marca de medio sin sede (p. ej. YAPE, INTERBANK, BCO CREDITO).
 * El SP usa el nombre corto para Central y «… HUANTA» para Huanta.
 */
function marcaBase(clave: string): string | null {
  const k = normalizarClave(clave)
  if (k.includes('YAPE')) return 'YAPE'
  if (k.includes('PLIN')) return 'PLIN'
  if (k.includes('INTERBANK')) return 'INTERBANK'
  if (
    k.includes('BCO CREDITO') ||
    k.includes('BANCO CREDITO') ||
    k === 'BCP' ||
    (k.includes('BCP') && !k.includes('NACION'))
  ) {
    return 'BCP'
  }
  if (k.includes('BBVA')) return 'BBVA'
  if (k.includes('SCOTIA')) return 'SCOTIABANK'
  return null
}

/** Resuelve marca visual + etiqueta legible desde denominación ValorTabla o clave del SP. */
export function resolveResumenCuentaVariant(
  clave: string,
): { etiqueta: string; variant: ResumenCuentaVariant } {
  const k = normalizarClave(clave)
  const sede = sufijoSede(k)

  if (k === 'EFECTIVO' || k.includes('EFECTIVO') || k === 'CASH') {
    return { etiqueta: 'Efectivo', variant: 'efectivo' }
  }
  if (k.includes('YAPE')) {
    return { etiqueta: `Yape${sede}`, variant: 'yape' }
  }
  if (k.includes('PLIN')) {
    return { etiqueta: `Plin${sede}`, variant: 'plin' }
  }
  if (k.includes('INTERBANK')) {
    return { etiqueta: `Interbank${sede}`, variant: 'interbank' }
  }
  if (
    k.includes('BCO CREDITO') ||
    k.includes('BANCO CREDITO') ||
    k === 'BCP' ||
    (k.includes('BCP') && !k.includes('NACION'))
  ) {
    return { etiqueta: `BCP${sede}`, variant: 'bcp' }
  }
  if (k.includes('NACION') || /\bBN\b/.test(k) || k === 'BCO NACION' || k.startsWith('BCO NACION')) {
    return { etiqueta: 'Banco de la Nación', variant: 'banco-nacion' }
  }
  if (k.includes('BBVA')) {
    return { etiqueta: `BBVA${sede}`, variant: 'bbva' }
  }
  if (k.includes('SCOTIA')) {
    return { etiqueta: `Scotiabank${sede}`, variant: 'scotiabank' }
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
 * Si el SP trae «YAPE» + «YAPE HUANTA» (sin «CENTRAL» en el primero),
 * aclara el corto como Central — paridad visual con el legacy.
 */
function aclararParesCentralHuanta(items: ResumenCuentaItem[]): ResumenCuentaItem[] {
  const marcasConHuanta = new Set<string>()
  for (const item of items) {
    if (sufijoSede(item.clave) === ' Huanta') {
      const marca = marcaBase(item.clave)
      if (marca) marcasConHuanta.add(marca)
    }
  }
  if (marcasConHuanta.size === 0) return items

  return items.map((item) => {
    if (sufijoSede(item.clave) !== '') return item
    const marca = marcaBase(item.clave)
    if (!marca || !marcasConHuanta.has(marca)) return item
    return { ...item, etiqueta: `${item.etiqueta} Central` }
  })
}

/**
 * Parsea texto tipo:
 * `RESUMEN BOVEDA: EFECTIVO = 1166477.21  YAPE = 405491.64 ... YAPE HUANTA = -13.10`
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
    const { etiqueta, variant } = resolveResumenCuentaVariant(clave)
    items.push({ clave: normalizarClave(clave), etiqueta, monto, variant })
  }

  if (items.length > 0) {
    const tituloMatch = trimmed.match(/^([^=]+?):/i)
    const titulo = tituloMatch?.[1]?.trim()
    return { titulo, items: aclararParesCentralHuanta(items) }
  }

  return { items: [], textoPlano: trimmed }
}
