import { apiFetch } from './client'

export type VoucherValidacionResult = {
  ok: boolean
  canalDetectado: string | null
  monto: number | null
  fechaOperacion: string | null
  fechaOperacionIso: string | null
  /** Valor listo para datetime-local / FechaHoraDigitalField. */
  fechaOperacionLocal: string | null
  codigoOperacion: string | null
  coincideMonto: boolean
  ocrDisponible: boolean
  advertencias: string[]
  textoOcrPreview: string | null
}

export async function validarVoucherCaptura(body: {
  oficinaId: number
  montoEsperado?: number | null
  canalEsperado?: string | null
  captura: File | Blob
  fileName?: string
}): Promise<VoucherValidacionResult> {
  const form = new FormData()
  form.append('oficinaId', String(body.oficinaId))
  if (body.montoEsperado != null && body.montoEsperado > 0) {
    form.append('montoEsperado', body.montoEsperado.toFixed(2))
  }
  if (body.canalEsperado?.trim()) {
    form.append('canalEsperado', body.canalEsperado.trim())
  }
  const name = body.fileName ?? (body.captura instanceof File ? body.captura.name : 'voucher.jpg')
  form.append('captura', body.captura, name)

  return apiFetch<VoucherValidacionResult>('/credito/validar-voucher-captura', {
    method: 'POST',
    body: form,
  })
}
