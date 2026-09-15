import { resolveSpaPathFromLegacyUrl, resolveSpaPathFromModulo } from './legacyRoutes'

/** Rutas directas del menú Crédito (legacy: cada ítem tenía URL propia, no un hub). */
function resolveCreditoOperacionFromLabel(denominacion: string | null | undefined): string | null {
  const label = normalizeLabel(denominacion ?? '')
  if (!label) {
    return null
  }

  if (label === 'tareas' || label.includes('tarea')) {
    return '/credito/tareas'
  }
  if (label === 'cliente' || label === 'clientes') {
    return '/clientes'
  }
  if (label === 'creditos' || label.includes('consulta de credito')) {
    return '/credito/consulta'
  }
  if (label.includes('prendario') || label.includes('predario')) {
    if (label.includes('nuevo')) {
      return '/credito/prendario/nuevo'
    }
    return '/credito/prendario'
  }
  if (
    (label.includes('simulador') ||
      label.includes('simular') ||
      label.includes('simulacion')) &&
    !label.includes('parametro')
  ) {
    return '/credito/simulador'
  }
  if (label.includes('aprobacion') || label.includes('aprobar')) {
    return '/credito/aprobar'
  }
  if (label.includes('caja diario')) {
    return '/caja/diario'
  }
  if (label.includes('condonacion')) {
    return '/credito/condonaciones'
  }
  if (label.includes('movimiento') && label.includes('boveda')) {
    return '/tesoreria/movimiento-boveda'
  }
  if (label.includes('boveda')) {
    return '/tesoreria/boveda'
  }
  if (label.includes('verificar pago')) {
    return '/caja/verificar-pagos'
  }
  if (label === 'saldos caja' || label.includes('saldos')) {
    return '/caja/saldos'
  }
  if (label.includes('cajachica') || label.includes('caja chica')) {
    return '/caja/chica'
  }
  if (label.includes('parametro') && label.includes('simulador')) {
    return '/credito/parametros-simulador'
  }

  return null
}

function resolveCreditoOperacionFromUrl(url: string | null | undefined): string | null {
  const normalized = normalizeLabel(url ?? '').replace(/^~\//, '/')
  if (!normalized) {
    return null
  }

  if (normalized.includes('/credito/tareas') || normalized.includes('/tareas')) {
    return '/credito/tareas'
  }
  if (
    normalized.includes('/prendario/create') ||
    normalized.endsWith('prendario/create')
  ) {
    return '/credito/prendario/nuevo'
  }
  if (
    normalized.includes('/credito/prendario') ||
    normalized.includes('/credito/predario') ||
    normalized.includes('creditoprendario') ||
    normalized.includes('creditopredario') ||
    normalized.includes('/prendario')
  ) {
    return '/credito/prendario'
  }
  if (normalized.includes('/condonacion')) {
    return '/credito/condonaciones'
  }
  if (normalized.includes('/credito/simulador') || normalized.includes('reportesimuladorplanpagos')) {
    return '/credito/simulador'
  }
  if (normalized.includes('/credito/parametrossimulador')) {
    return '/credito/parametros-simulador'
  }

  return null
}

function normalizeLabel(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
}

/**
 * Resuelve ruta SPA desde ítem de menú (URL legacy o etiqueta del menú Reportes).
 */
export function resolveSpaPathFromMenuItem(
  url: string | null | undefined,
  denominacion: string | null | undefined,
  modulo: string | null | undefined,
): string | null {
  const label = normalizeLabel(denominacion ?? '')
  const mod = (modulo ?? '')
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')

  if (url?.includes('?')) {
    const fromUrlWithQuery = resolveSpaPathFromLegacyUrl(url)
    if (fromUrlWithQuery?.includes('?')) {
      return fromUrlWithQuery
    }
  }

  const creditoOperacionDirecta = resolveCreditoOperacionFromLabel(denominacion)
  if (creditoOperacionDirecta && (mod.includes('CREDITO') || isCreditoOperacionLabel(label))) {
    return creditoOperacionDirecta
  }

  const creditoOperacionUrl = resolveCreditoOperacionFromUrl(url)
  if (creditoOperacionUrl) {
    return creditoOperacionUrl
  }

  if (url?.trim() && /credito\/creditos/i.test(url)) {
    const fromCreditoUrl = resolveSpaPathFromLegacyUrl(url)
    if (fromCreditoUrl) {
      return fromCreditoUrl
    }
  }

  if (url?.trim()) {
    const fromUrl = resolveSpaPathFromLegacyUrl(url)
    if (fromUrl) {
      return fromUrl
    }
  }

  if (label.includes('cobranza')) {
    return '/reportes/cobranza'
  }

  if (
    label === 'dashboard' ||
    label.includes('dashboard admin') ||
    label.includes('dashboard gestor')
  ) {
    return '/inicio'
  }

  if (
    (label === 'credito' || label.includes('reporte credito') || label.includes('creditos')) &&
    (mod.includes('REPORTE') || mod === 'REPORTES')
  ) {
    return '/reportes/credito'
  }

  if (
    (label === 'almacen' || label.includes('almacen') || label.includes('stock')) &&
    (mod.includes('REPORTE') || mod === 'REPORTES')
  ) {
    return '/reportes/almacen'
  }

  if (
    (label === 'venta' || label.includes('reporte venta')) &&
    (mod.includes('REPORTE') || mod === 'REPORTES')
  ) {
    return '/reportes/venta'
  }

  if (mod.includes('MANTENIMIENTO')) {
    if (label === 'oficina' || label.includes('oficinas')) {
      return '/mantenimiento/oficinas'
    }
    if (label === 'caja' || label.includes('cajas')) {
      return '/mantenimiento/cajas'
    }
  }

  if (mod.includes('MAESTRO')) {
    if (label === 'oficina' || label.includes('oficinas')) {
      return '/mantenimiento/oficinas'
    }
  }

  const catalogo = resolveCatalogoOperacionFromLabel(label, mod)
  if (catalogo) {
    return catalogo
  }

  return resolveSpaPathFromModulo(modulo)
}

/** Hijos de maestros / ventas / almacén / seguridad. No mapear padres (CREDITO, SEGURIDAD): el SP los une al menú y el hub ampliaría permisos. */
function resolveCatalogoOperacionFromLabel(label: string, mod: string): string | null {
  if (mod.includes('SEGURIDAD') || mod.includes('ADMINISTRACION')) {
    if (label === 'usuario' || label === 'usuarios') {
      return '/admin/usuarios'
    }
    if (label === 'rol' || label === 'roles') {
      return '/admin/roles'
    }
    if (label.includes('comision')) {
      return '/admin/comisiones'
    }
  }

  if (label === 'marca' || label === 'marcas') {
    return '/maestros/marcas'
  }
  if (label === 'modelo' || label === 'modelos') {
    return '/maestros/modelos'
  }
  if (label.includes('tipo articulo') || label === 'tipoarticulo') {
    return '/maestros/tipos-articulo'
  }
  if (label === 'articulo' || label === 'articulos') {
    return '/maestros/articulos'
  }
  if (label === 'almacenes' || (label === 'almacen' && mod.includes('MAESTRO'))) {
    return '/maestros/almacenes'
  }

  if (label.includes('venta rapida')) {
    return '/ventas/venta-rapida'
  }
  if (label.includes('orden venta') || label.includes('ordenes de venta')) {
    return '/ventas/orden-venta'
  }
  if (label.includes('canje') && label.includes('punto')) {
    return '/ventas/canjear-puntos'
  }
  if (label.includes('lista precio') && !mod.includes('REPORTE')) {
    return '/ventas/lista-precios'
  }

  if (label.includes('kardex')) {
    return '/almacen/kardex'
  }
  if (label.includes('entrada') && label.includes('almacen')) {
    return '/almacen/entrada'
  }
  if (label.includes('salida') && label.includes('almacen')) {
    return '/almacen/salida'
  }
  if (label.includes('transferencia') && label.includes('almacen')) {
    return '/almacen/transferencia'
  }
  if (label.includes('asignar') && label.includes('caja')) {
    return '/caja/asignar'
  }

  return null
}

function isCreditoOperacionLabel(label: string): boolean {
  return (
    label === 'tareas' ||
    label.includes('tarea') ||
    label === 'cliente' ||
    label === 'clientes' ||
    label === 'creditos' ||
    label.includes('consulta de credito') ||
    label.includes('prendario') ||
    label.includes('predario') ||
    label.includes('simulador') ||
    label.includes('simular') ||
    label.includes('simulacion') ||
    label.includes('aprobacion') ||
    label.includes('aprobar') ||
    label.includes('condonacion') ||
    label.includes('boveda')
  )
}
