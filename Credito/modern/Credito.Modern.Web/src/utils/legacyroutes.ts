/** Mapeo URL MVC → ruta SPA (orden: rutas específicas antes que genéricas). */
const LEGACY_TO_SPA: Array<{ match: RegExp; spaPath: string }> = [
  { match: /^\/home\/index$/i, spaPath: '/inicio' },
  { match: /^\/home$/i, spaPath: '/inicio' },
  { match: /^\/home\/login$/i, spaPath: '/login' },
  { match: /^\/administracion$/i, spaPath: '/admin' },
  { match: /^\/tesoreria$/i, spaPath: '/tesoreria' },
  { match: /\/entrada(\/index)?$/i, spaPath: '/almacen/entrada' },
  { match: /\/salida(\/index)?$/i, spaPath: '/almacen/salida' },
  { match: /\/transferencia(\/index)?$/i, spaPath: '/almacen/transferencia' },
  { match: /\/marca(\/index)?$/i, spaPath: '/maestros/marcas' },
  { match: /^\/marca$/i, spaPath: '/maestros/marcas' },
  { match: /\/modelo(\/index)?$/i, spaPath: '/maestros/modelos' },
  { match: /\/tipoarticulo(\/index)?$/i, spaPath: '/maestros/tipos-articulo' },
  { match: /\/articulo(\/index)?$/i, spaPath: '/maestros/articulos' },
  { match: /\/almacen(\/index)?$/i, spaPath: '/maestros/almacenes' },
  { match: /\/ventarapida(\/index)?$/i, spaPath: '/ventas/venta-rapida' },
  { match: /\/canjearpuntos(\/index)?$/i, spaPath: '/ventas/canjear-puntos' },
  { match: /\/ordenventa\/ordenesventa/i, spaPath: '/ventas/orden-venta' },
  { match: /\/ordenventa(\/index)?$/i, spaPath: '/ventas/orden-venta' },
  { match: /reporteavanceventa|rentabilidadventa/i, spaPath: '/informes/rentabilidad-venta' },
  { match: /\/listaprecio(\/index)?$/i, spaPath: '/ventas/lista-precios' },
  {
    match: /reportelistaprecio|listaprecioinforme/i,
    spaPath: '/ventas/informe-lista-precios',
  },
  { match: /reportekardex|generarkardex/i, spaPath: '/almacen/kardex' },
  { match: /^\/entrada(\/index)?$/i, spaPath: '/almacen' },
  { match: /^\/salida(\/index)?$/i, spaPath: '/almacen' },
  { match: /^\/transferencia(\/index)?$/i, spaPath: '/almacen' },
  { match: /reporteaval|rptaval|avalpersona/i, spaPath: '/informes/aval-persona' },
  {
    match: /reportecentrarriegotxt|centralriesgo/i,
    spaPath: '/informes/central-riesgo',
  },
  { match: /\/cliente(\/index)?$/i, spaPath: '/clientes' },
  { match: /^\/cliente\/mantener/i, spaPath: '/clientes' },
  { match: /^\/oficina(\/index)?$/i, spaPath: '/mantenimiento/oficinas' },
  { match: /^\/usuario(\/index)?$/i, spaPath: '/admin/usuarios' },
  { match: /^\/rol(\/index)?$/i, spaPath: '/admin/roles' },
  { match: /^\/comision(\/index)?$/i, spaPath: '/admin/comisiones' },
  { match: /^\/administracion$/i, spaPath: '/admin' },
  { match: /^\/informes?$/i, spaPath: '/informes' },
  { match: /\/reporte\/credito$/i, spaPath: '/reportes/credito' },
  { match: /\/reporte\/almacen$/i, spaPath: '/reportes/almacen' },
  { match: /\/reporte\/cobranzapagos$/i, spaPath: '/reportes/cobranza' },
  { match: /\/reporte\/venta$/i, spaPath: '/reportes/venta' },
  { match: /^\/tareas(\/index)?$/i, spaPath: '/credito/tareas' },
  { match: /^\/credito\/tareas/i, spaPath: '/credito/tareas' },
  { match: /^\/credito$/i, spaPath: '/credito' },
  { match: /^\/caja\/asignar/i, spaPath: '/caja/asignar' },
  { match: /reportestock(?!anulado)/i, spaPath: '/informes/reporte-stock' },
  { match: /reportestockanulado|stockanulado/i, spaPath: '/informes/stock-anulados' },
  { match: /listarsaldocartera|saldo-cartera/i, spaPath: '/informes/saldo-cartera' },
  { match: /\/credito\/cajadiario/i, spaPath: '/caja/diario' },
  { match: /^\/caja(\/index)?$/i, spaPath: '/mantenimiento/cajas' },
  { match: /\/cajadiario(\/index)?$/i, spaPath: '/caja/diario' },
  { match: /\/verificarpagos(\/index)?$/i, spaPath: '/caja/verificar-pagos' },
  { match: /\/saldos(\/index)?$/i, spaPath: '/caja/saldos' },
  { match: /\/cajachica(\/index)?$/i, spaPath: '/caja/chica' },
  { match: /\/boveda(\/index)?$/i, spaPath: '/tesoreria/boveda' },
  { match: /\/cliente\/boveda/i, spaPath: '/tesoreria/boveda' },
  {
    match: /reportemovimientoboveda|movimientoboveda/i,
    spaPath: '/tesoreria/movimiento-boveda',
  },
  { match: /\/credito\/simulador/i, spaPath: '/credito/simulador' },
  {
    match: /\/credito\/parametrossimulador/i,
    spaPath: '/credito/parametros-simulador',
  },
  { match: /\/credito\/consulta|\/credito\/index/i, spaPath: '/credito/consulta' },
  { match: /\/credito\/tareas/i, spaPath: '/credito/tareas' },
  { match: /\/creditoaprobar(\/index)?$/i, spaPath: '/credito/aprobar' },
  {
    match: /clientesinactivos|reporteclientesinactivos/i,
    spaPath: '/informes/clientes-inactivos',
  },
  {
    match: /reportemorosidadgestor|morosidadgestor/i,
    spaPath: '/informes/morosidad-gestor',
  },
  {
    match: /reportecreditovencido|creditovencido/i,
    spaPath: '/informes/credito-vencido',
  },
  {
    match: /reporteclientebloqueado|clientebloqueado/i,
    spaPath: '/informes/clientes-bloqueados',
  },
  {
    match: /reporteclientetopecredito|clientetopecredito/i,
    spaPath: '/informes/clientes-tope-credito',
  },
  {
    match: /reportecreditoobservado|creditoobservado/i,
    spaPath: '/informes/creditos-observados',
  },
  {
    match: /reportecreditomorosidad|creditomorosidad/i,
    spaPath: '/informes/credito-morosidad',
  },
  {
    match: /reporteclientesnuevosmes|clientesnuevosmes/i,
    spaPath: '/informes/clientes-nuevos-mes',
  },
  {
    match: /reportecreditocondonado|creditocondonado/i,
    spaPath: '/informes/credito-condonado',
  },
  {
    match: /reportecajasasignadas|cajasasignadas/i,
    spaPath: '/informes/cajas-asignadas',
  },
  {
    match: /reportecajadiario(?!informe)/i,
    spaPath: '/informes/caja-diario',
  },
  {
    match: /reportecreditorentabilidad|creditorentabilidad/i,
    spaPath: '/informes/credito-rentabilidad',
  },
  {
    match: /reportecreditoaprobado|creditoaprobacion/i,
    spaPath: '/informes/credito-aprobacion',
  },
  {
    match: /reportecreditoactivo|creditosactivos/i,
    spaPath: '/informes/creditos-activos',
  },
  {
    match: /reportecreditocierre|creditoscierres/i,
    spaPath: '/informes/creditos-cierres',
  },
  {
    match: /reportecreditomorosopagado|creditosmorosospagados/i,
    spaPath: '/informes/creditos-morosos-pagados',
  },
  {
    match: /reportecomprobantescajaanulados|movimientocajaanulado/i,
    spaPath: '/informes/movimientos-caja-anulados',
  },
  {
    match: /reportecredito(?!activo|aprob|rentabil|observ|condon|moros|vencido|cierre)/i,
    spaPath: '/informes/reporte-creditos',
  },
  {
    match: /reportesaldocarteracajadiario|saldocarteracaja/i,
    spaPath: '/informes/saldo-cartera-caja-diario',
  },
  {
    match: /cobrodiariodetalle|rptcobrodiariodetalle/i,
    spaPath: '/informes/cobro-diario-detalle',
  },
  {
    match: /reporteplanpagos|planpagos/i,
    spaPath: '/informes/plan-pagos',
  },
  {
    match: /reporteestadocredito|estadocredito/i,
    spaPath: '/informes/estado-credito',
  },
  {
    match: /reportecreditotarea|creditotarea/i,
    spaPath: '/informes/credito-tarea',
  },
  {
    match: /reportecomprobantescajachica/i,
    spaPath: '/informes/comprobantes-caja-chica',
  },
  {
    match: /reportecliente(?!s|tope|bloqueado|nuevos|inactivo)/i,
    spaPath: '/informes/reporte-cliente',
  },
  {
    match: /reportecodbarras|codigobarras/i,
    spaPath: '/almacen/codigo-barras',
  },
  {
    match: /constanciaalmacen/i,
    spaPath: '/almacen/constancia',
  },
  {
    match: /reportesimuladorplanpagos|simuladorplanpagos/i,
    spaPath: '/credito/simulador',
  },
  { match: /\/credito\/creditos/i, spaPath: '/credito/persona' },
  { match: /\/canjearpuntos/i, spaPath: '/ventas/canjear-puntos' },
  { match: /reportecobrodiario/i, spaPath: '/informes/cobro-diario' },
  { match: /reportecobrodiariodetalle/i, spaPath: '/informes/cobro-diario-detalle' },
]

function mapQueryToSpaPath(pathOnly: string, query: URLSearchParams): string | null {
  if (/\/credito\/creditos/i.test(pathOnly)) {
    const pid = query.get('pPersonaId') ?? query.get('personaId')
    if (pid) return `/credito/persona/${pid}`
  }
  if (/\/credito\/(consulta|index)/i.test(pathOnly)) {
    const cid = query.get('pCreditoId') ?? query.get('creditoId')
    if (cid) return `/credito/consulta?creditoId=${cid}`
  }
  if (/constanciaalmacen/i.test(pathOnly)) {
    const mid = query.get('pMovimientoId') ?? query.get('movimientoId')
    if (mid) return `/almacen/constancia?movimientoId=${mid}`
  }
  if (/reportecliente/i.test(pathOnly)) {
    const pid = query.get('pPersonaId') ?? query.get('personaId')
    if (pid) return `/informes/reporte-cliente?personaId=${pid}`
  }
  return null
}

export function resolveSpaPathFromLegacyUrl(legacyUrl: string): string | null {
  const trimmed = legacyUrl.trim().replace(/^~/, '')
  const qIndex = trimmed.indexOf('?')
  let pathOnly = (qIndex >= 0 ? trimmed.slice(0, qIndex) : trimmed).trim()
  const queryString = qIndex >= 0 ? trimmed.slice(qIndex + 1) : ''
  if (!pathOnly.startsWith('/')) {
    pathOnly = `/${pathOnly}`
  }

  const query = new URLSearchParams(queryString)
  const fromQuery = mapQueryToSpaPath(pathOnly, query)
  if (fromQuery) {
    return fromQuery
  }

  for (const { match, spaPath } of LEGACY_TO_SPA) {
    if (match.test(pathOnly)) {
      return spaPath
    }
  }
  return null
}

/** Hub SPA cuando el ítem de menú no trae URL mapeable (paridad módulo MVC). */
const MODULO_HUB: Record<string, string> = {
  CREDITO: '/credito',
  REPORTES: '/informes',
  REPORTE: '/informes',
  CAJA: '/caja',
  VENTAS: '/ventas',
  ALMACEN: '/almacen',
  ADMINISTRACION: '/admin',
  TESORERIA: '/tesoreria',
  CLIENTE: '/clientes',
  CLIENTES: '/clientes',
  MAESTRO: '/maestros',
  MAESTROS: '/maestros',
}

export function resolveSpaPathFromModulo(modulo: string | null | undefined): string | null {
  if (!modulo?.trim()) {
    return null
  }
  const key = modulo
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
  return MODULO_HUB[key] ?? null
}

