import { apiFetch } from './client'

export type OficinaMedioPago = {
  oficinaMedioPagoId: number
  oficinaId: number
  parentOficinaMedioPagoId: number | null
  tipoPagoId: number
  nombre: string
  marcaCodigo: string
  orden: number
  activo: boolean
  esDigital: boolean
  usaCelular: boolean
  telefono: string | null
  usaCuenta: boolean
  numeroCuenta: string | null
  cci: string | null
  usaQr: boolean
  qrEmvPayload: string | null
  qrNota: string | null
  familiaCodigo: string | null
  soloCentral: boolean
  variantes: OficinaMedioPago[]
}

export type GuardarOficinaMedioPagoBody = {
  oficinaMedioPagoId?: number | null
  oficinaId: number
  parentOficinaMedioPagoId?: number | null
  tipoPagoId?: number | null
  nombre: string
  marcaCodigo: string
  orden: number
  activo: boolean
  esDigital: boolean
  usaCelular: boolean
  telefono?: string | null
  usaCuenta: boolean
  numeroCuenta?: string | null
  cci?: string | null
  usaQr: boolean
  qrEmvPayload?: string | null
  qrNota?: string | null
  familiaCodigo?: string | null
  soloCentral: boolean
  crearTipoPagoCatalogo: boolean
}

export async function fetchOficinaMediosPago(
  oficinaId: number,
  opts?: { incluirInactivos?: boolean; soloRaices?: boolean },
): Promise<OficinaMedioPago[]> {
  const q = new URLSearchParams()
  if (opts?.incluirInactivos) q.set('incluirInactivos', 'true')
  if (opts?.soloRaices === false) q.set('soloRaices', 'false')
  const qs = q.toString()
  return apiFetch<OficinaMedioPago[]>(
    `/oficinas/${oficinaId}/medios-pago${qs ? `?${qs}` : ''}`,
  )
}

export async function guardarOficinaMedioPago(
  body: GuardarOficinaMedioPagoBody,
): Promise<{ oficinaMedioPagoId: number; tipoPagoId: number }> {
  return apiFetch(`/medios-pago/guardar`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export async function setOficinaMedioPagoActivo(
  oficinaMedioPagoId: number,
  activo: boolean,
  oficinaId: number,
): Promise<{ oficinaMedioPagoId: number; tipoPagoId: number }> {
  const q = new URLSearchParams({
    activo: activo ? 'true' : 'false',
    oficinaId: String(oficinaId),
  })
  return apiFetch(`/medios-pago/${oficinaMedioPagoId}/activar?${q}`, {
    method: 'POST',
  })
}
