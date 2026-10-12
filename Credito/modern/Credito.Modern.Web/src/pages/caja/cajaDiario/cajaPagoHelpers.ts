import { fetchTieneCxcPendiente } from '../../../api/cajaDiario'
import { cajaToastError } from './cajaFeedback'

export async function assertSinCxcPendiente(creditoId: number): Promise<boolean> {
  const tiene = await fetchTieneCxcPendiente(creditoId)
  if (tiene) {
    cajaToastError('Tiene cuentas por cobrar pendientes', 'caja-cxc-pend')
    return false
  }
  return true
}
