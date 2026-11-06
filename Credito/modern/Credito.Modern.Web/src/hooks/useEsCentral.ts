import { useQuery } from '@tanstack/react-query'
import { fetchOficinas } from '../api/oficinas'
import { useAuth } from '../auth/useAuth'

/** Central = la oficina del JWT tiene `IndPrincipal` (o nombre PRINCIPAL). Sucursal no ve la red. */
export function useEsCentral() {
  const { session } = useAuth()
  const oficinas = useQuery({ queryKey: ['oficinas'], queryFn: fetchOficinas })
  const oficinaSesionId = session?.oficinaId ?? 0
  const propia = oficinas.data?.find((o) => o.oficinaId === oficinaSesionId)
  const esCentral =
    propia?.indPrincipal === true ||
    /PRINCIPAL/i.test(propia?.denominacion?.trim() ?? '')

  return {
    esCentral,
    oficinaSesionId,
    oficinas: oficinas.data ?? [],
    loading: oficinas.isLoading,
  }
}
