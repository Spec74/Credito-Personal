const STORAGE_PREFIX = 'credix-report-visor:'
/** Tiempo máximo para abrir/refrescar el visor tras generar el enlace. */
const STASH_TTL_MS = 5 * 60 * 1000

type StashPayload = {
  path: string
  exp: number
}

function appBasePath(): string {
  const base = import.meta.env.BASE_URL ?? '/'
  const trimmed = base.endsWith('/') ? base.slice(0, -1) : base
  return trimmed === '' ? '' : trimmed
}

/**
 * Solo rutas relativas de export de informes / tickets PDF-CSV-TXT en la API moderna.
 * Cubre crédito, caja, almacén, ventas, prendario y maestros.
 */
export function isAllowedReportApiPath(apiPath: string): boolean {
  if (!apiPath.startsWith('/')) return false
  if (apiPath.includes('://') || apiPath.includes('..') || apiPath.includes('\\')) {
    return false
  }

  const pathOnly = apiPath.split('?', 1)[0] ?? apiPath
  return /^\/(credito|almacen|ventas|prendario|caja|maestros)\/[a-z0-9-]+-(pdf|csv|txt)$/i.test(
    pathOnly,
  )
}

export function isPdfReportApiPath(apiPath: string): boolean {
  const pathOnly = (apiPath.split('?', 1)[0] ?? apiPath).toLowerCase()
  return pathOnly.endsWith('-pdf')
}

function newReportId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`
}

/**
 * Guarda la ruta API en localStorage (mismo origen).
 * No usamos sessionStorage: un tab abierto con window.open no hereda sessionStorage.
 */
export function stashReportApiPath(apiPath: string): string {
  const normalized = apiPath.startsWith('/') ? apiPath : `/${apiPath}`
  if (!isAllowedReportApiPath(normalized)) {
    throw new Error('Ruta de informe no permitida.')
  }
  const id = newReportId()
  const payload: StashPayload = {
    path: normalized,
    exp: Date.now() + STASH_TTL_MS,
  }
  localStorage.setItem(`${STORAGE_PREFIX}${id}`, JSON.stringify(payload))
  pruneExpiredStashes()
  return id
}

export function takeReportApiPath(reportId: string): string | null {
  if (!reportId || reportId.length > 80) return null
  const key = `${STORAGE_PREFIX}${reportId}`
  const raw = localStorage.getItem(key)
  if (!raw) return null

  try {
    const parsed = JSON.parse(raw) as StashPayload
    if (
      !parsed ||
      typeof parsed.path !== 'string' ||
      typeof parsed.exp !== 'number' ||
      parsed.exp < Date.now()
    ) {
      localStorage.removeItem(key)
      return null
    }
    if (!isAllowedReportApiPath(parsed.path)) {
      localStorage.removeItem(key)
      return null
    }
    return parsed.path
  } catch {
    localStorage.removeItem(key)
    return null
  }
}

function pruneExpiredStashes(): void {
  try {
    const now = Date.now()
    for (let i = localStorage.length - 1; i >= 0; i--) {
      const key = localStorage.key(i)
      if (!key?.startsWith(STORAGE_PREFIX)) continue
      const raw = localStorage.getItem(key)
      if (!raw) continue
      try {
        const parsed = JSON.parse(raw) as StashPayload
        if (!parsed?.exp || parsed.exp < now) {
          localStorage.removeItem(key)
        }
      } catch {
        localStorage.removeItem(key)
      }
    }
  } catch {
    // Quota / modo privado: no bloquear la apertura del informe.
  }
}

/**
 * URL del visor sin filtros ni path API visibles.
 * Ejemplo: /app/reportes/visor?rid=3f2a…
 */
export function buildReportVisorUrl(apiPath: string): string {
  const rid = stashReportApiPath(apiPath)
  const visorPath = `${appBasePath()}/reportes/visor`.replace(/\/+/g, '/')
  const url = new URL(visorPath, window.location.origin)
  url.searchParams.set('rid', rid)
  return url.toString()
}

export function openReportVisorInTab(apiPath: string): void {
  if (!isPdfReportApiPath(apiPath)) {
    throw new Error('El visor solo admite informes PDF.')
  }
  const opened = window.open(buildReportVisorUrl(apiPath), '_blank')
  if (!opened) {
    throw new Error('Permita ventanas emergentes para ver el informe.')
  }
}

/** Rutas PDF conocidas (módulos + reporteador admin) — regresión de allowlist. */
export const KNOWN_REPORT_PDF_PATHS = [
  '/credito/rpt-clientes-inactivos-pdf',
  '/credito/rpt-cobro-diario-pdf',
  '/credito/rpt-morosidad-gestor-pdf',
  '/credito/rpt-credito-observado-pdf',
  '/credito/rpt-credito-vencido-pdf',
  '/credito/rpt-credito-morosidad-pdf',
  '/credito/rpt-plan-pagos-pdf',
  '/credito/rpt-estado-credito-pdf',
  '/credito/rpt-cliente-pdf',
  '/credito/rpt-movimiento-credito-pdf',
  '/credito/rpt-simulador-plan-pagos-pdf',
  '/credito/rpt-aval-pdf',
  '/credito/rpt-credito-pdf',
  '/credito/rpt-credito-aprobacion-pdf',
  '/credito/rpt-credito-rentabilidad-pdf',
  '/credito/rpt-credito-condonado-pdf',
  '/credito/rpt-creditos-activos-pdf',
  '/credito/rpt-creditos-cierres-pdf',
  '/credito/rpt-creditos-morosos-pagados-pdf',
  '/credito/rpt-clientes-bloqueados-pdf',
  '/credito/rpt-clientes-tope-credito-pdf',
  '/credito/rpt-clientes-nuevos-mes-pdf',
  '/credito/rpt-caja-diario-pdf',
  '/credito/rpt-cajas-asignadas-pdf',
  '/credito/rpt-comprobantes-caja-chica-pdf',
  '/credito/rpt-movimiento-caja-anulado-pdf',
  '/credito/rpt-saldo-cartera-caja-diario-pdf',
  '/credito/rpt-cobro-diario-detalle-pdf',
  '/credito/rpt-saldos-caja-pdf',
  '/credito/rpt-movimiento-boveda-pdf',
  '/credito/rpt-credito-tarea-pdf',
  '/credito/listar-saldo-cartera-pdf',
  '/credito/pagos-no-verificados-pdf',
  '/credito/movimiento-caja-ticket-pdf',
  '/credito/movimiento-caja-chica-ticket-pdf',
  '/credito/movimiento-boveda-ticket-pdf',
  '/credito/central-riesgo-generar-pdf',
  '/almacen/reporte-stock-pdf',
  '/almacen/rpt-stock-anulados-pdf',
  '/almacen/generar-kardex-pdf',
  '/almacen/rpt-constancia-almacen-pdf',
  '/ventas/rpt-rentabilidad-venta-pdf',
  '/ventas/rpt-lista-precio-pdf',
  '/ventas/codigo-barras-lst-pdf',
  '/prendario/contrato-pdf',
  '/prendario/acta-entrega-pdf',
] as const
