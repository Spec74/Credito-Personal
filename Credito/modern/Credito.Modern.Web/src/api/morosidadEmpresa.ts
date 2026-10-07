import { apiDownload, apiFetch } from './client'

export interface MorosidadPermisos {
  puedeConsultar: boolean
}

export interface MorosidadEmpresaItem {
  personaId: number
  nombreCompleto: string
  numeroDocumento: string
  celular: string
  oficinaId: number | null
  oficina: string
  gestorId: number | null
  gestorUsuario: string
  gestorNombre: string
  creditosMora: number
  saldoMora: number
  primeraCuotaVencida: string | null
  fechaUltimoPago: string | null
  diasAtraso: number
  codigoClasificacion: string
  clasificacion: string
  fechaCorte: string
}

export interface MorosidadResumen {
  clientes: number
  creditos: number
  saldo: number
  nuncaPagaron: number
  dejaronPagar: number
  paganConAtraso: number
}

export interface MorosidadEmpresaResponse {
  success: boolean
  fechaCorte: string
  resumen: MorosidadResumen
  filas: MorosidadEmpresaItem[]
}

export type MorosidadTipo =
  | 'TODOS'
  | 'NUNCA_PAGO'
  | 'DEJO_PAGAR'
  | 'PAGA_CON_ATRASO'

export function fetchMorosidadPermisos(): Promise<MorosidadPermisos> {
  return apiFetch('/morosidad/permisos')
}

export function fetchMorosidadEmpresa(params?: {
  tipo?: MorosidadTipo
  oficinaId?: number | null
  gestorId?: number | null
  fechaCorte?: string | null
}): Promise<MorosidadEmpresaResponse> {
  const q = new URLSearchParams()
  if (params?.tipo && params.tipo !== 'TODOS') q.set('tipo', params.tipo)
  if (params?.oficinaId != null && params.oficinaId > 0) {
    q.set('oficinaId', String(params.oficinaId))
  }
  if (params?.gestorId != null && params.gestorId > 0) {
    q.set('gestorId', String(params.gestorId))
  }
  if (params?.fechaCorte) q.set('fechaCorte', params.fechaCorte)
  const qs = q.toString()
  return apiFetch(`/morosidad/empresa${qs ? `?${qs}` : ''}`)
}

export function downloadMorosidadExcel(fechaInicio: string, fechaFin: string): Promise<void> {
  const stamp = `${fechaInicio}_${fechaFin}`.replace(/-/g, '')
  return apiDownload(
    `/morosidad/excel?fechaInicio=${encodeURIComponent(fechaInicio)}&fechaFin=${encodeURIComponent(fechaFin)}`,
    `MOROSIDAD_EMPRESA_${stamp}.xlsx`,
  )
}
