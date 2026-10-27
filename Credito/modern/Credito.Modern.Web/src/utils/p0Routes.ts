/** Rutas P0 del spec (T-A9). RoleHome solo compone las que el menú ya autoriza. */
export const P0_ROUTES = [
  '/login',
  '/inicio',
  '/clientes',
  '/credito/consulta',
  '/credito/nuevo',
  '/credito/simulador',
  '/credito/aprobar',
  '/caja/diario',
  '/caja/cobro',
  '/caja/verificar-pagos',
  '/tesoreria/boveda',
  '/credito/prendario',
  '/credito/prendario/nuevo',
] as const

export const P0_ROLE_HOMES = {
  cajero: ['/caja/diario', '/caja/cobro'],
  analista: ['/credito/nuevo', '/credito/consulta', '/clientes'],
  aprobador: ['/credito/aprobar'],
  encargado: ['/caja/saldos'],
  admin: ['/credito/aprobar', '/tesoreria/red'],
} as const
