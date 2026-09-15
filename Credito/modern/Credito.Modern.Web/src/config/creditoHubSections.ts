import type { CredixHubLink, CredixHubSection } from '../components/credix/CredixHubGrid'

/**
 * Índice del módulo Crédito — paridad con menú MVC Reportes → Crédito (`Reporte/Credito.cshtml`)
 * y operaciones `Credito/*`. Cada tarjeta abre la pantalla SPA con filtros y export.
 */
export const CREDITO_HUB_QUICK_ACCESS: CredixHubLink[] = [
  {
    to: '/credito/simulador',
    label: 'Simulador de crédito',
    description: 'Igual que el acceso rápido del menú lateral',
  },
  {
    to: '/credito/prendario',
    label: 'Crédito prendario',
    description: 'Listado, bienes en custodia, contrato y acta',
  },
  {
    to: '/informes/creditos-observados',
    label: 'Créditos observados',
    description: 'Acceso rápido Observados',
  },
  {
    to: '/informes/morosidad-gestor',
    label: 'Morosidad por gestor',
    description: 'Acceso rápido Vencidos / mora en ruta',
  },
  {
    to: '/informes/clientes-inactivos',
    label: 'Clientes inactivos',
    description: 'Acceso rápido del pie del menú',
  },
]

export const CREDITO_HUB_SECTIONS: CredixHubSection[] = [
  {
    title: 'Operaciones de crédito',
    links: [
      {
        to: '/credito/consulta',
        label: 'Créditos',
        description: 'Buscar cliente, elegir crédito, plan y gestión',
      },
      {
        to: '/credito/prendario',
        label: 'Crédito prendario',
        description: 'Registro directo de cliente, prenda y solicitud',
      },
      {
        to: '/credito/aprobar',
        label: 'Aprobar créditos',
        description: 'Bandeja de solicitudes pendientes',
      },
      {
        to: '/credito/simulador',
        label: 'Simulador',
        description: 'Plan de pagos y alta de solicitud',
      },
      {
        to: '/credito/tareas',
        label: 'Tareas de crédito',
        description: 'Seguimiento y subtareas',
      },
      {
        to: '/credito/parametros-simulador',
        label: 'Parámetros del simulador',
        description: 'Factores de compensación de cuota',
      },
      {
        to: '/clientes',
        label: 'Clientes',
        description: 'Búsqueda y ficha de persona',
      },
      {
        to: '/tesoreria/boveda',
        label: 'Bóveda',
        description: 'Estado de dinero, cierre y transferencias',
      },
    ],
  },
  {
    title: 'Cobro diario y mora (gestor)',
    links: [
      {
        to: '/informes/cobro-diario',
        label: 'Cobro diario',
        description: 'Cartera del día — mismo informe que MVC',
      },
      {
        to: '/informes/cobro-diario-detalle',
        label: 'Cobro diario detalle',
        description: 'Detalle de cobranza del gestor',
      },
      {
        to: '/informes/morosidad-gestor',
        label: 'Morosidad por gestor',
        description: 'Cobro diario filtrado por mora',
      },
      {
        to: '/informes/credito-vencido',
        label: 'Crédito vencido',
        description: 'Cartera vencida',
      },
      {
        to: '/informes/credito-morosidad',
        label: 'Morosidad por días de atraso',
        description: 'Reporte morosidad (rango de días)',
      },
    ],
  },
  {
    title: 'Reportes de cartera',
    links: [
      {
        to: '/informes/reporte-creditos',
        label: 'Reporte créditos',
        description: 'Filtros por oficina, gestor, estado y fechas',
      },
      {
        to: '/informes/credito-aprobacion',
        label: 'Créditos aprobados',
        description: 'Aprobaciones por fecha y gestor',
      },
      {
        to: '/informes/credito-rentabilidad',
        label: 'Rentabilidad de créditos',
        description: 'Rentabilidad PDF / Excel',
      },
      {
        to: '/informes/creditos-activos',
        label: 'Créditos activos',
        description: 'Cartera activa',
      },
      {
        to: '/informes/creditos-cierres',
        label: 'Créditos cierres',
        description: 'Cierres de crédito',
      },
      {
        to: '/informes/creditos-morosos-pagados',
        label: 'Morosos pagados',
        description: 'Morosos que pagaron',
      },
      {
        to: '/informes/credito-condonado',
        label: 'Créditos condonados',
        description: 'Condonaciones por periodo',
      },
    ],
  },
  {
    title: 'Clientes (informes)',
    links: [
      {
        to: '/informes/clientes-nuevos-mes',
        label: 'Clientes nuevos del mes',
        description: 'Altas del mes por gestor',
      },
      {
        to: '/informes/clientes-inactivos',
        label: 'Clientes inactivos',
        description: 'Sin movimiento reciente',
      },
      {
        to: '/informes/clientes-bloqueados',
        label: 'Clientes bloqueados',
        description: 'Personas bloqueadas',
      },
      {
        to: '/informes/clientes-tope-credito',
        label: 'Tope de crédito',
        description: 'Topes por cliente',
      },
    ],
  },
  {
    title: 'Consultas puntuales',
    links: [
      {
        to: '/informes/plan-pagos',
        label: 'Plan de pagos',
        description: 'Cuotas de un crédito',
      },
      {
        to: '/informes/estado-credito',
        label: 'Estado de crédito',
        description: 'Cabecera y cuotas',
      },
      {
        to: '/informes/reporte-cliente',
        label: 'Ficha cliente',
        description: 'Datos y avales de la persona',
      },
      {
        to: '/informes/central-riesgo',
        label: 'Central de riesgo',
        description: 'Export TXT por periodo',
      },
      {
        to: '/informes/aval-persona',
        label: 'Aval por persona',
        description: 'Avales registrados',
      },
      {
        to: '/informes/credito-tarea',
        label: 'Informe de tareas',
        description: 'Tareas de crédito pendientes',
      },
    ],
  },
  {
    title: 'Saldo de cartera',
    links: [
      {
        to: '/informes/saldo-cartera',
        label: 'Saldo cartera',
        description: 'Saldo agregado de cartera',
      },
      {
        to: '/informes/saldo-cartera-caja-diario',
        label: 'Saldo cartera caja diario',
        description: 'Cartera por periodo (mes/año)',
      },
    ],
  },
]
