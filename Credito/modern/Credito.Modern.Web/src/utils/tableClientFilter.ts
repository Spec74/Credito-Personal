/** Filtro rápido en cliente (varias palabras, todas deben coincidir). */
export function filterTableRows<T>(
  rows: T[],
  term: string,
  pickText: (row: T) => string,
): T[] {
  const q = term.trim().toLowerCase()
  if (!q) {
    return rows
  }
  const tokens = q.split(/\s+/).filter(Boolean)
  return rows.filter((row) => {
    const hay = pickText(row).toLowerCase()
    return tokens.every((t) => hay.includes(t))
  })
}
