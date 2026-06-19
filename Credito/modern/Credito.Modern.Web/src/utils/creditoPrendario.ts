export type CreditoPrendarioInput = {
  descripcion: string
  montoTasacion: number
  fechaRemate: string
  observacion?: string
}

const PRENDARIO_HEADER = '[CREDITO PRENDARIO]'
const PRENDARIO_BLOCK_RE = /\n?\[CREDITO PRENDARIO\][\s\S]*?(?=\n\[[A-Z0-9 _-]+\]|$)/i

export function buildCreditoPrendarioObservacion(input: CreditoPrendarioInput): string {
  const lines = [
    PRENDARIO_HEADER,
    `Descripcion prenda: ${input.descripcion.trim().toUpperCase()}`,
    `Monto tasacion: ${input.montoTasacion.toFixed(2)}`,
    `Fecha remate: ${input.fechaRemate}`,
  ]
  const obs = input.observacion?.trim()
  if (obs) {
    lines.push(`Observacion: ${obs.toUpperCase()}`)
  }
  return lines.join('\n')
}

export function upsertCreditoPrendarioObservacion(
  observacionActual: string | null | undefined,
  input: CreditoPrendarioInput,
): string {
  const current = observacionActual?.trim() ?? ''
  const block = buildCreditoPrendarioObservacion(input)
  if (!current) return block
  if (PRENDARIO_BLOCK_RE.test(current)) {
    return current.replace(PRENDARIO_BLOCK_RE, `\n${block}`).trim()
  }
  return `${current}\n\n${block}`.trim()
}

export function extractCreditoPrendarioObservacion(
  observacionActual: string | null | undefined,
): Partial<CreditoPrendarioInput> {
  const current = observacionActual ?? ''
  const match = current.match(PRENDARIO_BLOCK_RE)
  if (!match) return {}
  const block = match[0]
  const descripcion = block.match(/Descripcion prenda:\s*(.+)/i)?.[1]?.trim()
  const montoRaw = block.match(/Monto tasacion:\s*([0-9.,]+)/i)?.[1]?.trim()
  const fechaRemate = block.match(/Fecha remate:\s*([0-9-]+)/i)?.[1]?.trim()
  const observacion = block.match(/Observacion:\s*(.+)/i)?.[1]?.trim()
  const montoTasacion = montoRaw ? Number(montoRaw.replace(',', '.')) : undefined
  return {
    descripcion,
    montoTasacion: Number.isFinite(montoTasacion) ? montoTasacion : undefined,
    fechaRemate,
    observacion,
  }
}
