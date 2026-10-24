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

/** Prefijos de marca. Lo que sobra en ValorTabla.Denominacion es la sede. */
const PREFIJOS_MARCA = [
  /^BANCO DE LA NACI[OÓ]N\b/,
  /^BCO NACI[OÓ]N\b/,
  /^BCO CREDITO\b/,
  /^BANCO CREDITO\b/,
  /^SCOTIABANK\b/,
  /^INTERBANK\b/,
  /^EFECTIVO\b/,
  /^YAPE\b/,
  /^PLIN\b/,
  /^BBVA\b/,
  /^BCP\b/,
]

function tituloSede(raw: string): string {
  return raw.toLowerCase().replace(/\b\w/g, (c) => c.toUpperCase())
}

/** Sede en el nombre del catálogo: «YAPE ANDAHUAYLAS» → Andahuaylas. */
function sufijoSede(clave: string): string {
  let k = normalizarClave(clave)
  for (const re of PREFIJOS_MARCA) {
    if (re.test(k)) {
      k = k.replace(re, '').trim()
      break
    }
  }
  if (!k) return ''
  return ` ${tituloSede(k)}`
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
 * El wordmark ya dice el banco (y Yape/Plin también). Junto al logo
 * solo va lo que aporta: sede, o «Efectivo» porque el icono no nombra.
 */
const MARCA_EN_EL_LOGO: ReadonlySet<ResumenCuentaVariant> = new Set([
  'yape',
  'plin',
  'interbank',
  'bcp',
  'banco-nacion',
  'bbva',
  'scotiabank',
])

export function medioComplemento(
  etiqueta: string,
  variant: ResumenCuentaVariant,
): string {
  if (variant === 'efectivo') return 'Efectivo'
  if (!MARCA_EN_EL_LOGO.has(variant)) return etiqueta
  return sufijoSede(etiqueta).trim()
}

/** Catálogo 1–5 es marca. 6–8 y «YAPE HUANTA» son sede disfrazada: no van al combo. */
export function esMarcaTipoPago(item: { itemId: number; denominacion: string }): boolean {
  if (item.itemId >= 6 && item.itemId <= 8) return false
  return !/\s+(HUANTA|PICHARI|ANDAHUAYLAS|HUAMANGA|CENTRAL)\s*$/i.test(
    item.denominacion.trim(),
  )
}

/** Nombre corto de sede para la lista (Oficina.Denominacion). */
export function etiquetaSede(oficina: string, indPrincipal: boolean): string {
  if (indPrincipal) return 'Central'
  const raw = oficina.replace(/^(SUCURSAL|OFICINA)\s+/i, '').trim()
  if (!raw) return oficina
  return raw.replace(/\S+/g, (w) => w.charAt(0).toUpperCase() + w.slice(1).toLowerCase())
}

export function medioEtiquetaUi(raw: string): {
  variant: ResumenCuentaVariant
  aria: string
  visible: string
} {
  const { etiqueta, variant } = resolveResumenCuentaVariant(raw)
  return {
    variant,
    aria: etiqueta,
    visible: medioComplemento(etiqueta, variant),
  }
}

/** Nombre de marca para combos y chips. El icono no sustituye al texto. */
export function medioNombreMarca(raw: string): string {
  const { variant, etiqueta } = resolveResumenCuentaVariant(raw)
  switch (variant) {
    case 'efectivo':
      return 'Efectivo'
    case 'yape':
      return 'Yape'
    case 'plin':
      return 'Plin'
    case 'interbank':
      return 'Interbank'
    case 'bcp':
      return 'BCP'
    case 'banco-nacion':
      return 'Banco de la Nación'
    case 'bbva':
      return 'BBVA'
    case 'scotiabank':
      return 'Scotiabank'
    case 'transferencia':
      return 'Transferencia'
    default:
      return etiqueta
  }
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
