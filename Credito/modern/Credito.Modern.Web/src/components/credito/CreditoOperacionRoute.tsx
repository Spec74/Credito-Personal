import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'
import { esCreditoPerfilSoloBandeja } from '../../utils/creditoOperacionPermisos'

/**
 * Restringe simulador, consulta, tareas, etc. cuando el usuario es solo APROBADOR 1
 * (paridad menú operativo vs bandeja CreditoAprobar).
 */
export function CreditoOperacionRoute({ children }: { children: ReactNode }) {
  const { session } = useAuth()
  const roles = session?.roles ?? []

  if (esCreditoPerfilSoloBandeja(roles)) {
    return <Navigate to="/credito/aprobar" replace />
  }

  return children
}
