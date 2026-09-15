import { apiFetch } from './client'

export interface CondonacionPendiente {
  id: number
  creditoId: number
  personaId: number
  nombreCliente: string
  nombreUsuario: string
  montoCredito: number
  moraCondonacion: number
  totalPago: number
  fecha: string
  cajaDiarioId: number
}

export interface CondonacionPendienteCredito {
  tienePendiente: boolean
  moraCondonacion: number
  totalPago: number
  id: number | null
}

function postJson<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export function fetchCondonacionesPendientes(): Promise<CondonacionPendiente[]> {
  return apiFetch<CondonacionPendiente[]>('/credito/condonaciones-pendientes')
}

export function fetchCondonacionPendienteCredito(
  creditoId: number,
): Promise<CondonacionPendienteCredito> {
  return apiFetch<CondonacionPendienteCredito>(
    `/credito/condonacion-pendiente?creditoId=${creditoId}`,
  )
}

export function solicitarCondonacion(body: {
  oficinaId: number
  cajaDiarioId: number
  creditoId: number
  moraCondonacion: number
}) {
  return postJson<{ success: boolean; mensaje: string | null }>(
    '/credito/solicitar-condonacion',
    body,
  )
}

export function eliminarCondonacionPendiente(id: number): Promise<void> {
  return apiFetch<void>(`/credito/condonaciones-pendientes/${id}`, { method: 'DELETE' })
}
