import { apiFetch } from './client'

export interface ParametrosSimulador {
  factorVariable: string
  factorFijo: string
}

export function fetchParametrosSimulador(): Promise<ParametrosSimulador> {
  return apiFetch('/credito/parametros-simulador')
}

export function actualizarParametrosSimulador(
  body: ParametrosSimulador,
): Promise<boolean> {
  return apiFetch('/credito/parametros-simulador', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
