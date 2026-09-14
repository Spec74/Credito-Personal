import { apiFetch } from './client'

export interface SaldoCajaSesionRow {
  id: number
  caja: string
  usuario: string | null
  saldoInicial: number
  saldoFinal: number
  fechaIniOperacion: string
  fechaFinOperacion: string | null
  indCierre: boolean
  transBoveda: boolean
}

/** Página del servidor: los totales cubren todo el filtro, no solo la página visible. */
export interface SaldoCajaSesionPage {
  page: number
  pageSize: number
  totalRecords: number
  totalPages: number
  totalSaldoInicial: number
  totalSaldoFinal: number
  rows: SaldoCajaSesionRow[]
}

export interface SaldosCajaQuery {
  buscar?: string
  page?: number
  pageSize?: number
}

function paginacionQuery(params: SaldosCajaQuery): URLSearchParams {
  const q = new URLSearchParams()
  q.set('page', String(params.page ?? 1))
  q.set('pageSize', String(params.pageSize ?? 25))
  const buscar = params.buscar?.trim()
  if (buscar) {
    q.set('buscar', buscar)
  }
  return q
}

export function fetchSaldosCajaDiario(
  oficinaId: number,
  params: SaldosCajaQuery = {},
): Promise<SaldoCajaSesionPage> {
  const q = paginacionQuery(params)
  q.set('oficinaId', String(oficinaId))
  return apiFetch<SaldoCajaSesionPage>(`/credito/saldos-caja-diario?${q}`)
}

export function fetchSaldosCajaChicaDiario(
  params: SaldosCajaQuery = {},
): Promise<SaldoCajaSesionPage> {
  return apiFetch<SaldoCajaSesionPage>(
    `/credito/saldos-caja-chica-diario?${paginacionQuery(params)}`,
  )
}

export function fetchSaldosCajaDiarioBoveda(
  oficinaId: number,
  bovedaId: number,
  params: SaldosCajaQuery = {},
): Promise<SaldoCajaSesionPage> {
  const q = paginacionQuery(params)
  q.set('oficinaId', String(oficinaId))
  q.set('bovedaId', String(bovedaId))
  return apiFetch<SaldoCajaSesionPage>(`/credito/saldos-caja-diario-boveda?${q}`)
}
