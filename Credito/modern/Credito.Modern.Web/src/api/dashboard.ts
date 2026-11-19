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
  cobradoHoy: number
  cobradoAyer: number
  saldoActual: number
  montoMora: number
  clientesMora: number
  clientesMoraSinPago: number
  clientesMoraNuncaPagaron: number
  clientesMoraDejaronPagar: number
  clientesMoraPagandoConAtraso: number
  porcentajeMora: number
  porVencerSemana: number
}

export type DashboardMoraTipo =
  | 'TODOS'
  | 'SIN_PAGO'
  | 'NUNCA_PAGO'
  | 'DEJO_PAGAR'
  | 'PAGA_CON_ATRASO'

export interface DashboardClienteMoraRow {
  personaId: number
  nombreCompleto: string
  creditosMora: number
  saldoMora: number
  primeraCuotaVencida: string | null
  fechaUltimoPago: string | null
  diasAtraso: number
  codigoClasificacion: string
  clasificacion: string
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

export interface DashboardAnalistaMetaMes {
  metaConfigurada: boolean
  metaCapitalCierre: number | null
  capitalActual: number
  cumplimientoCapitalPct: number | null
  estadoCapital: string
  metaClientesActivosCierre: number | null
  clientesActivosActual: number
  cumplimientoClientesPct: number | null
  estadoClientes: string
  montoCobradoMes: number
  desembolsosMes: number
  clientesNuevosMes: number
}

export interface DashboardAnalista {
  nombreAnalista: string
  fechaConsulta: string
  kpis: DashboardAnalistaKpis
  productividad: DashboardProductividadPunto[]
  ranking: DashboardRankingRow[]
  podioMesAnterior: DashboardPodioRow[]
  insights: DashboardInsight[]
  metaMes?: DashboardAnalistaMetaMes | null
}

export function fetchDashboardAnalista(): Promise<DashboardAnalista> {
  return apiFetch<DashboardAnalista>('/dashboard/analista')
}

export function fetchDashboardClientesMora(
  tipo: DashboardMoraTipo = 'TODOS',
): Promise<DashboardClienteMoraRow[]> {
  const q = new URLSearchParams({ tipo })
  return apiFetch<DashboardClienteMoraRow[]>(`/dashboard/analista/clientes-mora?${q}`)
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

function dashboardSedeQuery(sedeId?: number) {
  return sedeId != null && sedeId > 0 ? `?sedeId=${sedeId}` : ''
}

export function fetchDashboardAdmin(sedeId?: number): Promise<DashboardAdmin> {
  return apiFetch<DashboardAdmin>(`/dashboard/admin${dashboardSedeQuery(sedeId)}`)
}

export function fetchDashboardAdminShell(sedeId?: number): Promise<DashboardAdminShell> {
  return apiFetch<DashboardAdminShell>(`/dashboard/admin/shell${dashboardSedeQuery(sedeId)}`)
}

export function fetchDashboardAdminDetalle(sedeId?: number): Promise<DashboardAdminDetalle> {
  return apiFetch<DashboardAdminDetalle>(`/dashboard/admin/detalle${dashboardSedeQuery(sedeId)}`)
}

export type DashboardAdminSemaforo = 'verde' | 'ambar' | 'rojo' | string

export interface DashboardAdminRedSedeRow {
  oficinaId: number
  nombreOficina: string
  cobradoHoy: number
  cobradoAyer: number
  variacionCobradoHoyPct: number | null
  desembolsoHoy: number
  flujoNetoHoy: number
  porcentajeMora: number
  clientesMora: number
  creditosPorVencerSemana: number
  totalAnalistas: number
  totalClientes: number
  semaforo: DashboardAdminSemaforo
  motivoSemaforo: string
}

export interface DashboardAdminRed {
  fechaConsulta: string
  totalSedes: number
  cobradoHoy: number
  cobradoAyer: number
  variacionCobradoHoyPct: number | null
  desembolsoHoy: number
  desembolsoAyer: number
  flujoNetoHoy: number
  flujoNetoAyer: number
  porcentajeMoraRed: number
  clientesMora: number
  creditosPorVencerSemana: number
  totalAnalistas: number
  totalClientes: number
  sedesEnRojo: number
  sedesEnAmbar: number
  sedesEnVerde: number
  sedes: DashboardAdminRedSedeRow[]
}

export function fetchDashboardAdminRed(): Promise<DashboardAdminRed> {
  return apiFetch<DashboardAdminRed>('/dashboard/admin/red')
}
