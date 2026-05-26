/** Ruta pública de la SPA (VITE_BASE_URL en build, p. ej. /app/). */
export function getSpaBasePath(): string {
  const base = (import.meta.env.BASE_URL as string) || '/'
  const trimmed = base.replace(/\/$/, '')
  return trimmed || ''
}

export function toSpaAbsolutePath(spaRelativePath: string): string {
  const rel = spaRelativePath.startsWith('/') ? spaRelativePath : `/${spaRelativePath}`
  const base = getSpaBasePath()
  return base ? `${base}${rel}` : rel
}
