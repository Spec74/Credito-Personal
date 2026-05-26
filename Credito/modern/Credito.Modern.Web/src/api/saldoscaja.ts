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

export function fetchSaldosCajaDiario(
  oficinaId: number,
): Promise<SaldoCajaSesionRow[]> {
  return apiFetch<SaldoCajaSesionRow[]>(
    `/credito/saldos-caja-diario?oficinaId=${oficinaId}`,
  )
}

export function fetchSaldosCajaChicaDiario(): Promise<SaldoCajaSesionRow[]> {
  return apiFetch<SaldoCajaSesionRow[]>('/credito/saldos-caja-chica-diario')
}

export function fetchSaldosCajaDiarioBoveda(
  oficinaId: number,
  bovedaId: number,
): Promise<SaldoCajaSesionRow[]> {
  return apiFetch<SaldoCajaSesionRow[]>(
    `/credito/saldos-caja-diario-boveda?oficinaId=${oficinaId}&bovedaId=${bovedaId}`,
  )
}
