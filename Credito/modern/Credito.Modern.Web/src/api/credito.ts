import { apiDownload, apiFetch } from './client'
import type { ListarSaldoCarteraRow } from '../types/api'

export interface SaldoCarteraParams {
  anio: number
  mes: number
  oficinaId: number
  usuarioId: number
}

function querySaldoCartera(p: SaldoCarteraParams): string {
  const q = new URLSearchParams({
    anio: String(p.anio),
    mes: String(p.mes),
    oficinaId: String(p.oficinaId),
    usuarioId: String(p.usuarioId),
  })
  return q.toString()
}

export function fetchSaldoCartera(
  params: SaldoCarteraParams,
): Promise<ListarSaldoCarteraRow[]> {
  return apiFetch<ListarSaldoCarteraRow[]>(
    `/credito/listar-saldo-cartera?${querySaldoCartera(params)}`,
  )
}

export function downloadSaldoCarteraCsv(params: SaldoCarteraParams): Promise<void> {
  return apiDownload(
    `/credito/listar-saldo-cartera-csv?${querySaldoCartera(params)}`,
    'listar-saldo-cartera.csv',
  )
}

export function downloadSaldoCarteraPdf(params: SaldoCarteraParams): Promise<void> {
  return apiDownload(
    `/credito/listar-saldo-cartera-pdf?${querySaldoCartera(params)}`,
    'listar-saldo-cartera.pdf',
  )
}
