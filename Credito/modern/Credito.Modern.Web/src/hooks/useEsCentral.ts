import { useQuery } from '@tanstack/react-query'
import { fetchOficinas } from '../api/oficinas'
import { useAuth } from '../auth/useAuth'

/** Central = la oficina del JWT tiene `IndPrincipal`. Sucursal no ve la red. */
export function useEsCentral() {
  const { session } = useAuth()
  const oficinas = useQuery({ queryKey: ['oficinas'], queryFn: fetchOficinas })
  const oficinaSesionId = session?.oficinaId ?? 0
  const esCentral =
    oficinas.data?.some((o) => o.oficinaId === oficinaSesionId && o.indPrincipal) === true

  return {
    esCentral,
    oficinaSesionId,
    oficinas: oficinas.data ?? [],
    loading: oficinas.isLoading,
  }
}
