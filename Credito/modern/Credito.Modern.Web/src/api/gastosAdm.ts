import { apiFetch } from './client'

export type CalcularGastosAdmResponse = {
  gastosAdm: number
}

/** Paridad CreditoController.CalcularGastosAdm / GastosAdmBL. */
export function calcularGastosAdm(
  monto: number,
  incluyeCentralRiesgo: boolean,
): Promise<CalcularGastosAdmResponse> {
  const q = new URLSearchParams({
    monto: String(monto),
    incluyeCentralRiesgo: incluyeCentralRiesgo ? 'true' : 'false',
  })
  return apiFetch<CalcularGastosAdmResponse>(`/credito/calcular-gastos-adm?${q}`)
}
