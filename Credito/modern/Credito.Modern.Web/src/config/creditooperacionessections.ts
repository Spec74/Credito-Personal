import type { CredixHubSection } from '../components/credix/CredixHubGrid'

/** Operaciones diarias de crédito (menú módulo Crédito, no Reportes). */
export const CREDITO_OPERACIONES_SECTIONS: CredixHubSection[] = [
  {
    title: 'Operaciones',
    links: [
      {
        to: '/credito/consulta',
        label: 'Créditos',
        description: 'Buscar cliente, elegir crédito, plan y gestión (paridad Creditos MVC)',
      },
      {
        to: '/credito/prendario',
        label: 'Crédito prendario',
        description: 'Registrar cliente y prenda antes de simular el crédito',
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
    ],
  },
]
