import { apiDownload, apiFetch, apiOpenInTab } from './client'
import { ApiError } from './errors'
import type { RptCobroDiarioDetalleRow } from '../types/api'

export interface CobranzaPagosResumen {
  totalClientes: number
  totalCredito: number
  totalPagado: number
  totalSaldo: number
}

export interface CobranzaPagosRow extends RptCobroDiarioDetalleRow {
  pagosLista: string[]
  pagosCount: number
  impagosCount: number
}

export interface CobranzaPagosFiltrosAplicados {
  usuarioId?: number | null
  oficinaId?: number | null
}

export interface CobranzaPagosResponse {
  data: CobranzaPagosRow[]
  resumen: CobranzaPagosResumen
  filtrosAplicados?: CobranzaPagosFiltrosAplicados
}

function queryCobranza(usuarioId?: number, oficinaId?: number): string {
  const q = new URLSearchParams()
  if (usuarioId != null && usuarioId > 0) {
    q.set('usuarioId', String(usuarioId))
    q.set('pGestorid', String(usuarioId))
  }
  if (oficinaId != null && oficinaId > 0) {
    q.set('oficinaId', String(oficinaId))
    q.set('pOficinaid', String(oficinaId))
  }
  return q.toString()
}

export function fetchCobranzaPagos(params: {
  usuarioId?: number
  oficinaId?: number
}): Promise<CobranzaPagosResponse> {
  const qs = queryCobranza(params.usuarioId, params.oficinaId)
  return apiFetch<CobranzaPagosResponse>(
    `/credito/rpt-cobranza-pagos${qs ? `?${qs}` : ''}`,
  )
}

export function downloadCobranzaPagosCsv(params: {
  usuarioId?: number
  oficinaId?: number
}): Promise<void> {
  const qs = queryCobranza(params.usuarioId, params.oficinaId)
  return apiDownload(
    `/credito/rpt-cobranza-pagos-csv${qs ? `?${qs}` : ''}`,
    'cobranza-pagos.csv',
  )
}

/** Excel .xlsx con título, encabezados y colores de cuotas (paridad visual MVC). */
export async function downloadCobranzaPagosExcel(params: {
  usuarioId?: number
  oficinaId?: number
}): Promise<void> {
  const qs = queryCobranza(params.usuarioId, params.oficinaId)
  try {
    await apiDownload(
      `/credito/rpt-cobranza-pagos-excel${qs ? `?${qs}` : ''}`,
      'cobranza-pagos.xlsx',
    )
  } catch (e) {
    if (e instanceof ApiError && (e.status === 404 || e.status === 502)) {
      throw e
    }
    throw e
  }
}

/** CSV plano (uso interno / integraciones). */
export function openCobranzaPagosCsvInTab(params: {
  usuarioId?: number
  oficinaId?: number
}): Promise<void> {
  const qs = queryCobranza(params.usuarioId, params.oficinaId)
  return apiOpenInTab(`/credito/rpt-cobranza-pagos-csv${qs ? `?${qs}` : ''}`)
}

/** Excel .xlsx con título, encabezados y colores de cuotas (paridad visual MVC). */
/** CSV plano (uso interno / integraciones). */
