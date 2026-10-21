export const INFORMES_RECIENTES_KEY = 'credix.informes.recientes'
const MAX = 6

export type InformeReciente = { to: string; label: string; at: number }

export function labelFromInformePath(path: string): string {
  const clean = path.split('?')[0] ?? path
  const slug = clean.replace(/^\/informes\/?/, '').replace(/-/g, ' ')
  if (!slug) return 'Informes'
  return slug.replace(/\b\w/g, (c) => c.toUpperCase())
}

export function readInformesRecientes(): InformeReciente[] {
  try {
    const raw = localStorage.getItem(INFORMES_RECIENTES_KEY)
    if (!raw) return []
    const parsed = JSON.parse(raw) as InformeReciente[]
    return Array.isArray(parsed) ? parsed.slice(0, MAX) : []
  } catch {
    return []
  }
}

export function rememberInforme(path: string): InformeReciente[] {
  const clean = path.split('?')[0] ?? path
  if (!clean.startsWith('/informes/') || clean === '/informes/') {
    return readInformesRecientes()
  }
  const next: InformeReciente[] = [
    { to: clean, label: labelFromInformePath(clean), at: Date.now() },
    ...readInformesRecientes().filter((r) => r.to !== clean),
  ].slice(0, MAX)
  try {
    localStorage.setItem(INFORMES_RECIENTES_KEY, JSON.stringify(next))
  } catch {
    /* ignore */
  }
  return next
}
