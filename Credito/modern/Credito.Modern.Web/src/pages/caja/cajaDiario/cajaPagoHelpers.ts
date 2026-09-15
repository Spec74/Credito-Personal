import { message } from 'antd'
import {
  downloadMovimientoCajaTicketPdf,
  fetchTieneCxcPendiente,
} from '../../../api/cajaDiario'

/** Descarga ticket PDF si el proc devolvió un MovimientoCajaId (paridad ImprimirMovCaja). */
export async function maybeDownloadCajaTicket(
  oficinaId: number,
  resultId: number | null | undefined,
): Promise<void> {
  if (!resultId || resultId < 1) {
    return
  }
  try {
    await downloadMovimientoCajaTicketPdf(oficinaId, resultId)
  } catch {
    message.warning('Pago registrado; no se pudo descargar el ticket automáticamente.')
  }
}

export async function assertSinCxcPendiente(creditoId: number): Promise<boolean> {
  const tiene = await fetchTieneCxcPendiente(creditoId)
  if (tiene) {
    message.error('Tiene cuentas por cobrar pendientes')
    return false
  }
  return true
}
