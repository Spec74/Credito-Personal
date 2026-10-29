export type TipoDocumentoOpcion = {
  tipoDocumentoId: number
  denominacion?: string | null
}

export type PreferenciaComprobante = 'nota' | 'boleta' | 'factura'

function normalizar(texto: string | null | undefined): string {
  return (texto ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toUpperCase()
}

function peso(denominacion: string | null | undefined, prefer: PreferenciaComprobante): number {
  const d = normalizar(denominacion)
  if (prefer === 'nota' && d.includes('NOTA')) return 0
  if (prefer === 'boleta' && d.includes('BOLETA')) return 0
  if (prefer === 'factura' && d.includes('FACTURA')) return 0
  if (d.includes('NOTA')) return 1
  if (d.includes('BOLETA')) return 2
  if (d.includes('FACTURA')) return 3
  return 9
}

/** Prefiere Nota de venta para ingreso interno; boleta/factura si el cliente lo pide. */
export function sugerirTipoDocumento(
  rows: TipoDocumentoOpcion[],
  prefer: PreferenciaComprobante = 'nota',
): TipoDocumentoOpcion | undefined {
  if (rows.length === 0) return undefined
  return [...rows].sort(
    (a, b) => peso(a.denominacion, prefer) - peso(b.denominacion, prefer),
  )[0]
}

export function seriePorTipoDocumento(denominacion: string | null | undefined): string {
  const d = normalizar(denominacion)
  if (d.includes('FACTURA')) return 'F001'
  if (d.includes('BOLETA')) return 'B001'
  if (d.includes('NOTA')) return 'NV01'
  return 'INT'
}

/** RUC / persona jurídica → factura; el resto boleta (POS). */
export function preferenciaComprobanteCliente(
  numeroDocumento: string | null | undefined,
  tipoPersona: string | null | undefined,
): PreferenciaComprobante {
  const doc = (numeroDocumento ?? '').replace(/\D/g, '')
  const tipo = (tipoPersona ?? '').trim().toUpperCase()
  if (tipo === 'J' || doc.length === 11) return 'factura'
  return 'boleta'
}
