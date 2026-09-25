import type { MenuItemDto } from '../types/api'
import { ADMIN_HUB_ENTRY_PATHS } from '../config/adminHubSections'
import { resolveSpaPathFromModulo } from './legacyRoutes'
import { resolveSpaPathFromMenuItem } from './resolveSpaPathFromMenuItem'

const ALWAYS_ALLOWED = [
  '/inicio',
  '/modulo',
  '/reportes/visor',
  // Accesos rápidos del layout MVC (siempre disponibles con sesión).
  '/admin/comisiones',
  '/credito/simulador',
  // Atajos de cartera del dashboard gestor/admin (paridad PDF/SPA del layout).
  '/informes/cobro-diario',
  '/informes/morosidad-gestor',
  '/informes/creditos-observados',
  '/informes/clientes-inactivos',
  '/informes/saldo-cartera',
]

const HUB_CHILDREN: Record<string, string[]> = {
  '/reportes/credito': ['/informes/'],
  '/reportes/almacen': ['/almacen/', '/informes/reporte-stock', '/informes/stock-anulados'],
  '/reportes/cobranza': ['/reportes/cobranza'],
  '/reportes/venta': ['/reportes/venta', '/informes/rentabilidad-venta'],
  '/credito': ['/credito/'],
  '/credito/consulta': ['/credito/persona/'],
  '/clientes': ['/clientes/'],
  '/caja': ['/caja/'],
  '/tesoreria': ['/tesoreria/'],
  '/tesoreria/boveda': ['/tesoreria/movimiento-boveda'],
  '/maestros': ['/maestros/', '/mantenimiento/'],
  '/almacen': ['/almacen/'],
  '/ventas': ['/ventas/'],
  '/admin': ['/admin/', '/mantenimiento/'],
}

const INFORMES_ACL_EXCLUSIONS = new Set(['/informes/cierre-gerencial'])

const EXACT_MENU_ROUTES = new Set([
  '/admin/usuarios',
  '/admin/roles',
  '/mantenimiento/oficinas',
  '/mantenimiento/cajas',
  '/caja/maestro',
  '/caja/asignar',
  '/caja/saldos',
  '/caja/verificar-pagos',
  '/credito/aprobar',
  '/credito/parametros-simulador',
  '/credito/prendario',
  '/credito/prendario/nuevo',
])

function normalizePath(path: string): string {
  const clean = path.split('?')[0]?.split('#')[0] ?? '/'
  if (clean.length > 1 && clean.endsWith('/')) {
    return clean.slice(0, -1)
  }
  return clean
}

function hasPathAccess(path: string, allowed: string, exactAllowedPaths: Set<string>): boolean {
  if (path === allowed) {
    return true
  }

  if (EXACT_MENU_ROUTES.has(path) && !exactAllowedPaths.has(path)) {
    return false
  }

  const children = HUB_CHILDREN[allowed] ?? []
  return children.some((prefix) => {
    if (!path.startsWith(prefix)) {
      return false
    }
    if (prefix === '/informes/' && INFORMES_ACL_EXCLUSIONS.has(path)) {
      return false
    }
    return true
  })
}

/** Índice `/admin` si el menú tiene al menos un acceso de seguridad/mantenimiento. */
function adminHubIndexAccess(path: string, allowedPaths: Set<string>): boolean | null {
  if (path !== '/admin') {
    return null
  }
  if (allowedPaths.has('/admin')) {
    return true
  }
  return ADMIN_HUB_ENTRY_PATHS.some((leaf) => allowedPaths.has(leaf))
}

export function hasMenuRouteAccess(
  pathname: string,
  menuItems: MenuItemDto[],
  extraAllowedPaths: string[] = [],
): boolean {
  const path = normalizePath(pathname)
  if (ALWAYS_ALLOWED.some((prefix) => path === prefix || path.startsWith(`${prefix}/`))) {
    return true
  }

  if (
    extraAllowedPaths.some((extra) => {
      const e = normalizePath(extra)
      return path === e || path.startsWith(`${e}/`)
    })
  ) {
    return true
  }

  const allowedPaths = new Set<string>()
  for (const item of menuItems) {
    const spaPath =
      resolveSpaPathFromMenuItem(item.url, item.denominacion, item.modulo) ??
      resolveSpaPathFromModulo(item.modulo)
    if (spaPath) {
      allowedPaths.add(normalizePath(spaPath))
    }
  }

  // Alias: maestro de cajas puede venir como /caja/maestro o /mantenimiento/cajas.
  if (allowedPaths.has('/caja/maestro')) {
    allowedPaths.add('/mantenimiento/cajas')
  }
  if (allowedPaths.has('/mantenimiento/cajas')) {
    allowedPaths.add('/caja/maestro')
  }

  const adminHub = adminHubIndexAccess(path, allowedPaths)
  if (adminHub !== null) {
    return adminHub
  }

  const prendario = prendarioAccess(path, allowedPaths)
  if (prendario !== null) {
    return prendario
  }

  return [...allowedPaths].some((allowed) => hasPathAccess(path, allowed, allowedPaths))
}

function prendarioAccess(path: string, allowedPaths: Set<string>): boolean | null {
  if (path !== '/credito/prendario' && !path.startsWith('/credito/prendario/')) {
    return null
  }

  const hasListado = allowedPaths.has('/credito/prendario')
  const hasNuevo = allowedPaths.has('/credito/prendario/nuevo')
  if (path === '/credito/prendario/nuevo') {
    return hasNuevo
  }
  if (path === '/credito/prendario') {
    return hasListado
  }
  return hasListado || hasNuevo
}
