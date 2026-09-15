/** Búsqueda insensible a mayúsculas y acentos (tablas de informes). */
export function normalizeSearchText(value: string): string {
  return value
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .toLowerCase()
    .trim()
}

export function matchesSearchText(haystack: string, query: string): boolean {
  const q = normalizeSearchText(query)
  if (!q) return true
  const h = normalizeSearchText(haystack)
  const tokens = q.split(/\s+/).filter(Boolean)
  return tokens.every((t) => h.includes(t))
}
