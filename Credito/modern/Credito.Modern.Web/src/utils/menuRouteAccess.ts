import type { MenuItemDto } from '../types/api'
import { resolveSpaPathFromModulo } from './legacyRoutes'
import { resolveSpaPathFromMenuItem } from './resolveSpaPathFromMenuItem'

const ALWAYS_ALLOWED = [
  '/inicio',
  '/modulo',
  '/reportes/visor',
]

const HUB_CHILDREN: Record<string, string[]> = {
  '/reportes/credito': ['/informes/'],
  '/reportes/almacen': ['/almacen/', '/informes/reporte-stock', '/informes/stock-anulados'],
  '/reportes/cobranza': ['/reportes/cobranza'],
  '/reportes/venta': ['/reportes/venta', '/informes/rentabilidad-venta'],
  '/credito': ['/credito/'],
  '/clientes': ['/clientes/'],
  '/caja': ['/caja/'],
  '/tesoreria': ['/tesoreria/'],
  '/maestros': ['/maestros/', '/mantenimiento/'],
  '/almacen': ['/almacen/'],
  '/ventas': ['/ventas/'],
  '/admin': ['/admin/', '/mantenimiento/'],
}

const EXACT_MENU_ROUTES = new Set([
  '/admin/usuarios',
  '/admin/roles',
  '/admin/comisiones',
  '/mantenimiento/oficinas',
  '/mantenimiento/cajas',
  '/caja/maestro',
  '/caja/asignar',
  '/caja/saldos',
  '/caja/verificar-pagos',
  '/credito/aprobar',
  '/credito/parametros-simulador',
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
  return children.some((prefix) => path.startsWith(prefix))
}

export function hasMenuRouteAccess(pathname: string, menuItems: MenuItemDto[]): boolean {
  const path = normalizePath(pathname)
  if (ALWAYS_ALLOWED.some((prefix) => path === prefix || path.startsWith(`${prefix}/`))) {
    return true
  }

  const allowedPaths = new Set<string>()
  for (const item of menuItems) {
    const spaPath = resolveSpaPathFromMenuItem(item.url, item.denominacion, item.modulo)
      ?? resolveSpaPathFromModulo(item.modulo)
    if (spaPath) {
      allowedPaths.add(normalizePath(spaPath))
    }
  }

  return [...allowedPaths].some((allowed) => hasPathAccess(path, allowed, allowedPaths))
}
