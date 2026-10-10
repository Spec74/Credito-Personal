import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchMenu } from '../api/menu'
import { useAuth } from '../auth/useAuth'
import type { CredixHubSection } from '../components/credix/CredixHubGrid'
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

  return useMemo(() => {
    if (!menuQuery.isSuccess) {
      return []
    }
    return filterHubSectionsByMenu(sections, menuQuery.data ?? [], {
      includeAlwaysAllowed: false,
    })
  }, [sections, menuQuery.isSuccess, menuQuery.data])
}
