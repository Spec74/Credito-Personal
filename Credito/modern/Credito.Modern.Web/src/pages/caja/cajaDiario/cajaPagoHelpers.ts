import {
  downloadMovimientoCajaTicketPdf,
  fetchTieneCxcPendiente,
} from '../../../api/cajaDiario'
import { cajaToastError, cajaToastWarning } from './cajaFeedback'

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
    cajaToastWarning(
      'Pago registrado; no se pudo descargar el ticket automáticamente.',
      'caja-ticket-auto',
    )
  }
}

export async function assertSinCxcPendiente(creditoId: number): Promise<boolean> {
  const tiene = await fetchTieneCxcPendiente(creditoId)
  if (tiene) {
    cajaToastError('Tiene cuentas por cobrar pendientes', 'caja-cxc-pend')
    return false
  }
  return true
}
