import { apiFetch } from './client'

export interface ApiPeruDniResult {
  success: boolean
  nombres: string | null
  apellidoPaterno: string | null
  apellidoMaterno: string | null
  mensaje: string | null
}

export interface ApiPeruRucResult {
  success: boolean
  razonSocial: string | null
  direccion: string | null
  mensaje: string | null
}

export function consultarDniApiPeru(dni: string): Promise<ApiPeruDniResult> {
  return apiFetch<ApiPeruDniResult>(
    `/integraciones/apiperu/dni/${encodeURIComponent(dni.trim())}`,
  )
}

export function consultarRucApiPeru(ruc: string): Promise<ApiPeruRucResult> {
  return apiFetch<ApiPeruRucResult>(
    `/integraciones/apiperu/ruc/${encodeURIComponent(ruc.trim())}`,
  )
}
