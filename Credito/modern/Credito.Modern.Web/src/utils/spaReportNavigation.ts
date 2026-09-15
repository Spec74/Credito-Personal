import { getSpaBasePath, toSpaAbsolutePath } from './spaBasePath'

/** Origen actual de la SPA (en dev :5173; en nginx /app/ el mismo host). */
export function getSpaAppOrigin(): string {
  return window.location.origin
}

export function buildSpaInformeUrl(
  spaPath: string,
  params?: Record<string, string | number | undefined | null>,
): string {
  const search = new URLSearchParams()
  if (params) {
    for (const [key, value] of Object.entries(params)) {
      if (value != null && value !== '') {
        search.set(key, String(value))
      }
    }
  }
  const qs = search.toString()
  const path = toSpaAbsolutePath(spaPath)
  return `${getSpaAppOrigin()}${path}${qs ? `?${qs}` : ''}`
}

/** Abre pantalla de informe en la SPA (mismo origen que Vite/nginx). */
export function openSpaInformeInNewTab(
  spaPath: string,
  params?: Record<string, string | number | undefined | null>,
): void {
  const url = buildSpaInformeUrl(spaPath, params)
  const opened = window.open(url, '_blank', 'noopener,noreferrer')
  if (!opened) {
    throw new Error('Permita ventanas emergentes para abrir el informe.')
  }
}

export { getSpaBasePath }
