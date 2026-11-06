import type { CredixHubSection } from '../components/credix/CredixHubGrid'

/** Índice Informes — cartera, crédito, caja y almacén. */
export const INFORMES_HUB_SECTIONS: CredixHubSection[] = [
  {
    title: 'Cartera y cobranza',
    links: [
      {
        to: '/reportes/cobranza',
        label: 'Cobranza pagos por gestor',
        description: 'Cuotas pagadas del periodo (Excel coloreado)',
      },
      {
        to: '/informes/saldo-cartera',
        label: 'Saldo cartera',
        description: 'Cierre mensual por gestor: desembolsos, saldo y mora',
      },
      {
        to: '/informes/cobro-diario',
        label: 'Cobro diario (gestor)',
        description: 'Cartera del día a cobrar; active «Solo mora» si necesita solo atrasados',
      },
      {
        to: '/informes/cobro-diario-detalle',
        label: 'Cobro diario detalle',
        description: 'Detalle de cuotas y pagos del cobro diario',
      },
      {
        to: '/informes/cobro-diario?soloMora=1',
        label: 'Cobro diario · solo mora',
        description: 'Misma cartera del día, filtrada a créditos con mora',
      },
      {
        to: '/informes/credito-vencido',
        label: 'Crédito vencido por franja',
        description: 'Vencidos menor/mayor a 60 días e irrecuperables por gestor',
      },
      {
        to: '/informes/credito-morosidad',
        label: 'Morosidad por días de atraso',
        description: 'Corte oficial por rango de días (reporte de mora)',
      },
      {
        to: '/informes/saldo-cartera-caja-diario',
        label: 'Saldo cartera caja diario',
        description: 'Comparativo inicio/fin de mes por caja',
      },
      {
        to: '/informes/cierre-gerencial',
        label: 'Cierre y metas gerenciales',
        description: 'Avance oficial / no oficial por cartera',
      },
      {
        to: '/informes/morosos',
        label: 'Morosos empresariales',
        description: 'Clientes en mora de la red (semáforo de pago)',
      },
    ],
  },
  {
    title: 'Créditos',
    links: [
      { to: '/informes/reporte-creditos', label: 'Reporte créditos', description: 'Filtros por estado y fechas' },
      { to: '/informes/creditos-activos', label: 'Créditos activos', description: 'Cartera vigente con saldos' },
      { to: '/informes/creditos-cierres', label: 'Créditos cierres', description: 'Cierres en el periodo' },
      { to: '/informes/creditos-morosos-pagados', label: 'Morosos pagados', description: 'Recuperación de mora' },
      { to: '/informes/credito-rentabilidad', label: 'Rentabilidad créditos', description: 'Interés, mora y pagos' },
      { to: '/informes/credito-aprobacion', label: 'Créditos aprobados', description: 'Aprobaciones del día' },
      { to: '/informes/creditos-observados', label: 'Créditos observados', description: 'Observados en cartera' },
      { to: '/informes/credito-condonado', label: 'Créditos condonados', description: 'Condonaciones' },
      { to: '/informes/central-riesgo', label: 'Central de riesgo', description: 'CSV, PDF y TXT regulatorio' },
      { to: '/informes/pla-uif', label: 'PLA / UIF', description: 'RO legal y bitácora ROS' },
      { to: '/informes/aval-persona', label: 'Aval por persona', description: 'Avales vinculados a la persona' },
      { to: '/informes/plan-pagos', label: 'Plan de pagos', description: 'Por número de crédito' },
      { to: '/informes/estado-credito', label: 'Estado de crédito', description: 'Cabecera y cuotas' },
      { to: '/informes/credito-tarea', label: 'Tareas de crédito', description: 'Bandeja de tareas' },
    ],
  },
  {
    title: 'Clientes',
    links: [
      { to: '/informes/clientes-inactivos', label: 'Clientes inactivos', description: 'Sin movimiento reciente' },
      { to: '/informes/clientes-bloqueados', label: 'Clientes bloqueados', description: 'Bloqueo administrativo' },
      { to: '/informes/clientes-tope-credito', label: 'Tope de crédito', description: 'Límites por cliente' },
      {
        to: '/informes/clientes-nuevos-mes',
        label: 'Primeros desembolsos del periodo',
        description: 'Clientes con primer desembolso en el rango de fechas',
      },
      {
        to: '/informes/reporte-cliente',
        label: 'Ficha de cliente',
        description: 'Datos de persona y avales (por personaId)',
      },
    ],
  },
  {
    title: 'Caja y almacén',
    links: [
      { to: '/informes/cajas-asignadas', label: 'Cajas asignadas', description: 'Asignación y saldos por caja' },
      { to: '/informes/caja-diario', label: 'Informe caja diario', description: 'Sesiones con hora inicio/fin' },
      {
        to: '/informes/comprobantes-caja-chica',
        label: 'Comprobantes caja chica',
        description: 'Gastos documentados de caja chica',
      },
      { to: '/informes/movimientos-caja-anulados', label: 'Mov. caja anulados', description: 'Anulaciones con fecha/hora' },
      { to: '/informes/reporte-stock', label: 'Stock almacén', description: 'Existencias' },
      { to: '/informes/stock-anulados', label: 'Stock anulados', description: 'Movimientos anulados' },
      { to: '/almacen/kardex', label: 'Kardex', description: 'Kardex por artículo' },
      { to: '/informes/rentabilidad-venta', label: 'Rentabilidad ventas', description: 'Margen por venta' },
      { to: '/ventas/informe-lista-precios', label: 'Lista de precios', description: 'Precios vigentes' },
    ],
  },
]
