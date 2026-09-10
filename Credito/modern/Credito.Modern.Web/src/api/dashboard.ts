import { apiFetch } from './client'

export interface DashboardAnalistaKpis {
  totalClientes: number
  clientesNuevosActual: number
  clientesNuevosAnterior: number
  variacionClientesNuevosPct: number | null
  creditosActual: number
  creditosAnterior: number
  variacionCreditosPct: number | null
  cobradoActual: number
  cobradoAnterior: number
  variacionCobradoPct: number | null
  saldoActual: number
  clientesMora: number
  porcentajeMora: number
  porVencerSemana: number
}

export interface DashboardProductividadPunto {
  fecha: string
  etiqueta: string
  montoCobrado: number
}

export interface DashboardRankingRow {
  usuarioId: number
  nombreCompleto: string
  totalCobrado: number
  posicion: number
  esUsuarioActual: boolean
}

export interface DashboardPodioRow {
  usuarioId: number
  nombreCompleto: string
  totalCobrado: number
  posicion: number
}

export interface DashboardInsight {
  tipo: 'success' | 'warning' | 'danger' | 'info' | string
  titulo: string
  mensaje: string
  accion: string | null
}

export interface DashboardAnalista {
  nombreAnalista: string
  fechaConsulta: string
  kpis: DashboardAnalistaKpis
  productividad: DashboardProductividadPunto[]
  ranking: DashboardRankingRow[]
  podioMesAnterior: DashboardPodioRow[]
  insights: DashboardInsight[]
}

export function fetchDashboardAnalista(): Promise<DashboardAnalista> {
  return apiFetch<DashboardAnalista>('/dashboard/analista')
}
