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
