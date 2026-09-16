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

export interface DashboardAdminResumen {
  totalAnalistas: number
  totalClientes: number
  creditosHoy: number
  creditosAyer: number
  creditosAnteayer: number
  creditosMesActual: number
  creditosMesAnteriorComparable: number
  variacionCreditosHoyPct: number | null
  variacionCreditosMesPct: number | null
  desembolsoHoy: number
  desembolsoAyer: number
  desembolsoAnteayer: number
  desembolsoMesActual: number
  desembolsoMesAnteriorComparable: number
  variacionDesembolsoHoyPct: number | null
  variacionDesembolsoMesPct: number | null
  cobradoHoy: number
  cobradoAyer: number
  cobradoAnteayer: number
  cobradoMesActual: number
  cobradoMesAnteriorComparable: number
  variacionCobradoHoyPct: number | null
  variacionCobradoMesPct: number | null
  entradasHoy: number
  salidasHoy: number
  flujoNetoHoy: number
  entradasAyer: number
  salidasAyer: number
  flujoNetoAyer: number
  entradasAnteayer: number
  salidasAnteayer: number
  flujoNetoAnteayer: number
  entradasMesActual: number
  salidasMesActual: number
  flujoNetoMesActual: number
  entradasMesAnteriorComparable: number
  salidasMesAnteriorComparable: number
  flujoNetoMesAnteriorComparable: number
  variacionFlujoHoyPct: number | null
  variacionFlujoMesPct: number | null
  saldoCartera: number
  saldoCreditos: number
  saldoMoraCartera: number
  saldoVencido: number
  saldoMorosidad: number
  clientesMora: number
  creditosPorVencerSemana: number
}

export interface DashboardAdminFlujoRow {
  operacion: string
  indEntrada: boolean
  concepto: string
  esTransferencia: boolean
  cantidadHoy: number
  importeHoy: number
  cantidadAyer: number
  importeAyer: number
  cantidadMesActual: number
  importeMesActual: number
  cantidadMesAnteriorComparable: number
  importeMesAnteriorComparable: number
}

export interface DashboardAdminHistoricoPunto {
  fecha: string
  etiqueta: string
  colocaciones: number
  desembolsado: number
  cobrado: number
  entradas: number
  salidas: number
  flujoNeto: number
  flujoOperativo: number
}

export interface DashboardAdminHistoricoMensual {
  fechaMes: string
  esMesActual: boolean
  etiqueta: string
  colocaciones: number
  desembolsado: number
  cobrado: number
  entradas: number
  salidas: number
  flujoNeto: number
  flujoOperativo: number
}

export interface DashboardAdminAnalistaRow {
  usuarioId: number
  nombreCompleto: string
  totalClientes: number
  clientesNuevosMes: number
  colocacionesHoy: number
  colocacionesMes: number
  desembolsoHoy: number
  desembolsoMes: number
  cobradoHoy: number
  cobradoMes: number
  cobradoMesAnteriorComparable: number
  variacionCobranzaPct: number | null
  clientesMora: number
  montoMora: number
  porcentajeMora: number
}

export interface DashboardAdmin {
  nombreOficina: string
  fechaConsulta: string
  resumen: DashboardAdminResumen
  flujoCaja: DashboardAdminFlujoRow[]
  historico: DashboardAdminHistoricoPunto[]
  historicoMensual: DashboardAdminHistoricoMensual[]
  analistas: DashboardAdminAnalistaRow[]
}

export interface DashboardAdminShell {
  nombreOficina: string
  fechaConsulta: string
  resumen: DashboardAdminResumen
}

export interface DashboardAdminDetalle {
  flujoCaja: DashboardAdminFlujoRow[]
  historico: DashboardAdminHistoricoPunto[]
  historicoMensual: DashboardAdminHistoricoMensual[]
  analistas: DashboardAdminAnalistaRow[]
  cartera: DashboardAdminCartera
}

export interface DashboardAdminCartera {
  totalClientes: number
  saldoCartera: number
  saldoCreditos: number
  saldoMoraCartera: number
  saldoVencido: number
  saldoMorosidad: number
  clientesMora: number
}

export function fetchDashboardAdmin(): Promise<DashboardAdmin> {
  return apiFetch<DashboardAdmin>('/dashboard/admin')
}

export function fetchDashboardAdminShell(): Promise<DashboardAdminShell> {
  return apiFetch<DashboardAdminShell>('/dashboard/admin/shell')
}

export function fetchDashboardAdminDetalle(): Promise<DashboardAdminDetalle> {
  return apiFetch<DashboardAdminDetalle>('/dashboard/admin/detalle')
}
