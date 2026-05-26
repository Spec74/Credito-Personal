import { apiFetch } from './client'

export interface CajaParaAsignarRow {
  cajaId: number
  denominacion: string
}

export interface AsignarCajaResult {
  cajaDiarioId: number | null
  esCajaChica: boolean
}

export function fetchCajasParaAsignar(
  oficinaId: number,
): Promise<CajaParaAsignarRow[]> {
  return apiFetch<CajaParaAsignarRow[]>(
    `/credito/cajas-para-asignar?oficinaId=${oficinaId}`,
  )
}

export function fetchMontoBovedaAsignacion(
  oficinaId: number,
): Promise<{ monto: number }> {
  return apiFetch<{ monto: number }>(
    `/credito/monto-boveda-asignacion?oficinaId=${oficinaId}`,
  )
}

export function asignarCaja(body: {
  oficinaId: number
  cajaId: number
  saldoInicial: number
}): Promise<AsignarCajaResult> {
  return apiFetch<AsignarCajaResult>('/credito/asignar-caja', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
