/** Etiqueta de mes en español (es-PE), independiente del idioma de SQL Server. */
export function formatMesEs(fechaIso: string, style: 'short' | 'long' = 'short'): string {
  const raw = fechaIso.trim()
  const m = /^(\d{4})-(\d{2})(?:-(\d{2}))?/.exec(raw)
  const d = m
    ? new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3] ?? '1'))
    : new Date(raw)
  if (Number.isNaN(d.getTime())) {
    return raw
  }
  const label = d.toLocaleDateString('es-PE', {
    month: style,
    year: 'numeric',
  })
  return label.replace(/\.$/, '').replace(/^\w/, (c) => c.toUpperCase())
}
