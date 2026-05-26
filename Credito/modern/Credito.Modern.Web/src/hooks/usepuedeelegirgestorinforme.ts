import { useAuth } from '../auth/useAuth'
import { canViewReporteCredito } from '../utils/reporteCreditoAccess'

/** ADMIN / APROBADOR / PARCIAL pueden elegir gestor TODOS en informes por gestor (paridad MVC). */
export function usePuedeElegirGestorInforme(): boolean {
  const { session } = useAuth()
  return canViewReporteCredito(session?.roles ?? [])
}
