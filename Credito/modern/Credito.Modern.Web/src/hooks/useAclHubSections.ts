import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchMenu } from '../api/menu'
import { useAuth } from '../auth/useAuth'
import type { CredixHubSection } from '../components/credix/CredixHubGrid'
import { puedeAsignarCajaUi, puedeCobroBloqueUi } from '../utils/cajaSaldosPermisos'
import { filterHubSectionsByMenu } from '../utils/filterHubSectionsByMenu'

/** Filtra tarjetas del hub por ACL de menú (solo módulos asignados). */
export function useAclHubSections(sections: CredixHubSection[]): CredixHubSection[] {
  const { session } = useAuth()
  const menuQuery = useQuery({
    queryKey: ['menu', session?.oficinaId, session?.usuarioId],
    queryFn: fetchMenu,
    enabled: (session?.oficinaId ?? 0) > 0 && (session?.usuarioId ?? 0) > 0,
    staleTime: 5 * 60_000,
  })

  const extraAllowedPaths = useMemo(() => {
    const paths: string[] = []
    const roles = session?.roles ?? []
    if (puedeAsignarCajaUi(roles)) paths.push('/caja/asignar')
    if (puedeCobroBloqueUi(roles)) paths.push('/caja/cobro-bloque')
    return paths
  }, [session?.roles])

  return useMemo(() => {
    if (!menuQuery.isSuccess) {
      return []
    }
    return filterHubSectionsByMenu(sections, menuQuery.data ?? [], {
      includeAlwaysAllowed: false,
      extraAllowedPaths,
    })
  }, [sections, menuQuery.isSuccess, menuQuery.data, extraAllowedPaths])
}
