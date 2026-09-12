import { apiDownload, apiFetch } from './client'
import { guardarPrendas, type PrendaItem } from './creditoGestion'

export type PrendarioCategoria =
  | 'SinBienes'
  | 'Otro'
  | 'Rematado'
  | 'Vencido'
  | 'PorVencer'
  | 'Vigente'

export interface PrendarioResumen {
  total: number
  porVencer: number
  vencidos: number
  rematados: number
}

export interface PrendarioCreditoRow {
  creditoId: number
  personaId: number
  numeroDocumento: string | null
  nombreCompleto: string | null
  celular: string | null
  numeroContratoPrendario: string | null
  montoTasacion: number
  montoCredito: number
  fechaVencimiento: string
  fechaRemate: string | null
  estado: string
  esPrendario: boolean
  bienes: number
  categoria: PrendarioCategoria
  diasParaVencer: number
}

export interface PrendarioListaPage {
  items: PrendarioCreditoRow[]
  total: number
  page: number
  pageSize: number
}

export function fetchPrendarioResumen(oficinaId: number): Promise<PrendarioResumen> {
  return apiFetch<PrendarioResumen>(`/prendario/resumen?oficinaId=${oficinaId}`)
}

export function fetchPrendarioCreditos(params: {
  oficinaId: number
  buscar?: string
  page?: number
  pageSize?: number
}): Promise<PrendarioListaPage> {
  const q = new URLSearchParams({ oficinaId: String(params.oficinaId) })
  if (params.buscar?.trim()) {
    q.set('buscar', params.buscar.trim())
  }
  q.set('page', String(params.page ?? 1))
  q.set('pageSize', String(params.pageSize ?? 20))
  return apiFetch<PrendarioListaPage>(`/prendario/creditos?${q}`)
}

export function crearSolicitudPrendaria(body: {
  oficinaId: number
  personaId: number
}): Promise<{ solicitudCreditoId: number }> {
  return apiFetch('/prendario/crear-solicitud', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function guardarBienesPrendario(body: {
  oficinaId: number
  creditoId: number
  prendas: PrendaItem[]
  fechaRemate?: string | null
}) {
  return guardarPrendas(body)
}

export function downloadContratoPrendarioPdf(oficinaId: number, creditoId: number, numeroContrato: string) {
  const q = new URLSearchParams({ oficinaId: String(oficinaId), creditoId: String(creditoId) })
  return apiDownload(`/prendario/contrato-pdf?${q}`, `ContratoPrendario_${numeroContrato}.pdf`)
}

export function downloadActaEntregaPrendarioPdf(oficinaId: number, creditoId: number, numeroContrato: string) {
  const q = new URLSearchParams({ oficinaId: String(oficinaId), creditoId: String(creditoId) })
  return apiDownload(`/prendario/acta-entrega-pdf?${q}`, `ActaEntregaPrendario_${numeroContrato}.pdf`)
}

export interface PrendarioAvisoVencimiento {
  creditoId: number
  oficinaId: number
  nombreCliente: string
  celular: string | null
  fechaVencimiento: string
  montoCredito: number
  interes: number
  montoCancelar: number
}

export interface PrendarioAvisoEnvioResumen {
  enviados: number
  fallidos: number
  omitidos: number
  detalle: { creditoId: number; exito: boolean; mensaje: string }[]
  advertencia?: string | null
}

export function fetchPrendarioAvisosVencimiento(
  oficinaId: number,
  diasAntes = 3,
): Promise<PrendarioAvisoVencimiento[]> {
  const q = new URLSearchParams({
    oficinaId: String(oficinaId),
    diasAntes: String(diasAntes),
  })
  return apiFetch<PrendarioAvisoVencimiento[]>(`/prendario/avisos-vencimiento?${q}`)
}

export function enviarAvisosVencimientoPrendario(body: {
  oficinaId: number
  diasAntes?: number
  creditoId?: number
}): Promise<PrendarioAvisoEnvioResumen> {
  return apiFetch<PrendarioAvisoEnvioResumen>('/prendario/avisos-vencimiento/enviar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export interface PrendarioWhatsAppPasada {
  fecha: string
  origen: string
  enviados: number
  fallidos: number
  omitidos: number
  advertencia: string | null
}

export interface PrendarioWhatsAppEstado {
  enabled: boolean
  configurado: boolean
  automaticoActivo: boolean
  diasAntes: number
  dailyHourLocal: number
  proximaCorrida: string
  correAlArrancar: boolean
  ultimaPasada: PrendarioWhatsAppPasada | null
}

export function fetchPrendarioAvisosEstado(): Promise<PrendarioWhatsAppEstado> {
  return apiFetch<PrendarioWhatsAppEstado>('/prendario/avisos-vencimiento/estado')
}
