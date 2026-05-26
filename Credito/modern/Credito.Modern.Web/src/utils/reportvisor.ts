function appBasePath(): string {
  const base = import.meta.env.BASE_URL ?? '/'
  const trimmed = base.endsWith('/') ? base.slice(0, -1) : base
  return trimmed === '' ? '' : trimmed
}

/** Abre el visor SPA en pestaña nueva (mismo origen + JWT en localStorage). */
export function buildReportVisorUrl(apiPath: string): string {
  const normalized = apiPath.startsWith('/') ? apiPath : `/${apiPath}`
  const visorPath = `${appBasePath()}/reportes/visor`.replace(/\/+/g, '/')
  const url = new URL(visorPath, window.location.origin)
  url.searchParams.set('path', normalized)
  return url.toString()
}
export function openReportVisorInTab(apiPath: string): void {
  const opened = window.open(buildReportVisorUrl(apiPath), '_blank')
  if (!opened) {
    throw new Error('Permita ventanas emergentes para ver el informe.')
  }
}
