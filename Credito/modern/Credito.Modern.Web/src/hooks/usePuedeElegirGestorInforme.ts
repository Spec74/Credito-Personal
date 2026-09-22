import { useAuth } from '../auth/useAuth'
import { canViewReporteCredito } from '../utils/reporteCreditoAccess'

/** ADMIN / APROBADOR / REPORTEPARCIAL (PARCIAL MVC) pueden elegir gestor TODOS en informes. */
export function usePuedeElegirGestorInforme(): boolean {
  const { session } = useAuth()
  return canViewReporteCredito(session?.roles ?? [])
}
