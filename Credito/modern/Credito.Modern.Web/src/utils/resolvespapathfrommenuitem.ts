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
    return '/credito/prendario'
  }
  if (label.includes('simulador') && !label.includes('parametro')) {
    return '/credito/simulador'
  }
  if (label.includes('aprobacion') || label.includes('aprobar')) {
    return '/credito/aprobar'
  }
  if (label.includes('caja diario')) {
    return '/caja/diario'
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
  if (url?.trim() && /credito\/creditos/i.test(url)) {
    const fromCreditoUrl = resolveSpaPathFromLegacyUrl(url)
    if (fromCreditoUrl) {
      return fromCreditoUrl
    }
  }

  const label = normalizeLabel(denominacion ?? '')
  const mod = (modulo ?? '')
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')

  if (mod.includes('CREDITO')) {
    const directo = resolveCreditoOperacionFromLabel(denominacion)
    if (directo) {
      return directo
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

  return resolveSpaPathFromModulo(modulo)
}
