import type { CredixHubLink } from '../components/credix/CredixHubGrid'

/** Accesos sugeridos cuando un ítem de menú no tiene pantalla SPA dedicada. */
const SUGGESTIONS: Record<string, CredixHubLink[]> = {
  CREDITO: [
    {
      to: '/credito/consulta',
      label: 'Créditos',
      description: 'Buscar cliente, estado, cuotas y gestión',
    },
    {
      to: '/credito/aprobar',
      label: 'Aprobar créditos',
      description: 'Bandeja de aprobación',
    },
    {
      to: '/credito/simulador',
      label: 'Simulador',
      description: 'Plan de pagos y solicitud',
    },
  ],
  CAJA: [
    { to: '/caja/diario', label: 'Caja diario', description: 'Cobros y arqueo' },
    { to: '/caja/saldos', label: 'Saldos y cierres', description: 'Cajas asignadas' },
    { to: '/caja/chica', label: 'Caja chica', description: 'Gastos y rendiciones' },
  ],
  REPORTES: [
    { to: '/informes', label: 'Informes', description: 'Catálogo completo' },
    { to: '/reportes/credito', label: 'Reportes crédito', description: 'Exportaciones cartera' },
    { to: '/reportes/cobranza', label: 'Cobranza pagos', description: 'Detalle por gestor' },
  ],
  REPORTE: [
    { to: '/informes', label: 'Informes', description: 'Catálogo completo' },
    { to: '/reportes/credito', label: 'Reportes crédito', description: 'Exportaciones cartera' },
  ],
  VENTAS: [
    { to: '/ventas/venta-rapida', label: 'Venta rápida', description: 'Mostrador' },
    { to: '/ventas/orden-venta', label: 'Orden de venta', description: 'Pedidos' },
    { to: '/ventas/lista-precios', label: 'Lista de precios', description: 'Mantenimiento' },
  ],
  ALMACEN: [
    { to: '/almacen/entrada', label: 'Entrada almacén', description: 'Ingresos' },
    { to: '/almacen/salida', label: 'Salida almacén', description: 'Egresos' },
    { to: '/almacen/kardex', label: 'Kardex', description: 'Movimientos por artículo' },
  ],
  ADMINISTRACION: [
    { to: '/admin/usuarios', label: 'Usuarios', description: 'Accesos y oficinas' },
    { to: '/admin/roles', label: 'Roles', description: 'Permisos' },
    { to: '/admin/oficinas', label: 'Oficinas', description: 'Sedes' },
  ],
  TESORERIA: [
    { to: '/tesoreria/boveda', label: 'Bóveda', description: 'Saldo y movimientos' },
    {
      to: '/tesoreria/movimiento-boveda',
      label: 'Movimiento bóveda',
      description: 'Historial',
    },
  ],
  CLIENTE: [
    { to: '/clientes', label: 'Clientes', description: 'Búsqueda y ficha' },
    { to: '/clientes/nuevo', label: 'Nuevo cliente', description: 'Alta de persona' },
  ],
  CLIENTES: [
    { to: '/clientes', label: 'Clientes', description: 'Búsqueda y ficha' },
    { to: '/clientes/nuevo', label: 'Nuevo cliente', description: 'Alta de persona' },
  ],
  MAESTRO: [
    { to: '/maestros/articulos', label: 'Artículos', description: 'Catálogo' },
    { to: '/maestros/almacenes', label: 'Almacenes', description: 'Puntos de stock' },
    { to: '/maestros/marcas', label: 'Marcas', description: 'Fabricantes' },
  ],
  MAESTROS: [
    { to: '/maestros/articulos', label: 'Artículos', description: 'Catálogo' },
    { to: '/maestros/almacenes', label: 'Almacenes', description: 'Puntos de stock' },
    { to: '/maestros/marcas', label: 'Marcas', description: 'Fabricantes' },
  ],
}

function normalizeModuloKey(modulo: string | null | undefined): string | null {
  if (!modulo?.trim()) {
    return null
  }
  return modulo
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
}

export function moduleHubSuggestions(
  modulo: string | null | undefined,
): CredixHubLink[] {
  const key = normalizeModuloKey(modulo)
  if (key && SUGGESTIONS[key]) {
    return SUGGESTIONS[key]
  }
  return [
    { to: '/inicio', label: 'Inicio', description: 'Panel principal' },
    { to: '/informes', label: 'Informes', description: 'Reportes y exportaciones' },
    { to: '/credito', label: 'Crédito', description: 'Operaciones de cartera' },
    { to: '/caja', label: 'Caja', description: 'Cobranza y cierres' },
  ]
}
