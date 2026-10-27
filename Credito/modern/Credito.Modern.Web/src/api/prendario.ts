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
  categoria?: PrendarioCategoria
  page?: number
  pageSize?: number
}): Promise<PrendarioListaPage> {
  const q = new URLSearchParams({ oficinaId: String(params.oficinaId) })
  if (params.buscar?.trim()) {
    q.set('buscar', params.buscar.trim())
  }
  if (params.categoria) {
    q.set('categoria', params.categoria)
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

export function devolverPrenda(body: { oficinaId: number; prendaId: number }) {
  return apiFetch<{ prendaId: number; creditoId: number; estado: string; fechaMod: string | null }>(
    '/prendario/devolver-prenda',
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    },
  )
}

export function downloadDevolucionPrendarioPdf(oficinaId: number, prendaId: number) {
  const q = new URLSearchParams({ oficinaId: String(oficinaId), prendaId: String(prendaId) })
  return apiDownload(`/prendario/devolucion-pdf?${q}`, `DevolucionPrendario_${prendaId}.pdf`)
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
  /** Presente desde API con webhook Meta; opcional en builds viejos. */
  webhookVerifyTokenConfigurado?: boolean
  webhookAppSecretConfigurado?: boolean
}

export function fetchPrendarioAvisosEstado(): Promise<PrendarioWhatsAppEstado> {
  return apiFetch<PrendarioWhatsAppEstado>('/prendario/avisos-vencimiento/estado')
}

export interface PrendarioCobroPreview {
  creditoId: number
  capitalPendiente: number
  interesPendiente: number
  saldoAprox: number
  fechaVencimiento: string
  fechaRemate: string | null
  fechaRemateEjecutado: string | null
  puedeRematar: boolean
  diasRenovar: number
  nuevoVencimiento: string
}

export function fetchPrendarioCobroPreview(oficinaId: number, creditoId: number) {
  const q = new URLSearchParams({ oficinaId: String(oficinaId), creditoId: String(creditoId) })
  return apiFetch<PrendarioCobroPreview>(`/prendario/cobro-preview?${q}`)
}

export function actualizarFichaPrenda(body: {
  oficinaId: number
  prendaId: number
  observaciones: string | null
}) {
  return apiFetch('/prendario/actualizar-ficha', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function subirFotoPrenda(oficinaId: number, prendaId: number, file: File) {
  const form = new FormData()
  form.append('oficinaId', String(oficinaId))
  form.append('prendaId', String(prendaId))
  form.append('archivo', file)
  return apiFetch('/prendario/prenda-foto', { method: 'POST', body: form })
}

export function renovarPrendario(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
  dias?: number
  tipoPagoId?: number
  fechaPagoTransferencia?: string
}) {
  return apiFetch('/prendario/renovar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ dias: 30, tipoPagoId: 1, ...body }),
  })
}

export function abonarPrendario(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
  importeRecibido: number
  tipoPagoId?: number
  fechaPagoTransferencia?: string
}) {
  return apiFetch('/prendario/abonar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tipoPagoId: 1, ...body }),
  })
}

export function liberarPrendario(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
  tipoPagoId?: number
  fechaPagoTransferencia?: string
}) {
  return apiFetch('/prendario/liberar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tipoPagoId: 1, ...body }),
  })
}

export function rematarPrenda(body: { oficinaId: number; prendaId: number }) {
  return apiFetch<{
    prendaId: number
    creditoId: number
    articuloId: number
    codArticulo: string
    precioVenta: number
    almacenId: number
  }>('/prendario/rematar', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
