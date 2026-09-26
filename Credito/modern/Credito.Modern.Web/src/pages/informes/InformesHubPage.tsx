import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { CredixModuleHubPage } from '../../components/credix'
import { INFORMES_HUB_SECTIONS } from '../../config/informesHubSections'
import { fetchCierreGerencialPermisos } from '../../api/cierreGerencial'
import { useAclHubSections } from '../../hooks/useAclHubSections'

export function InformesHubPage() {
  const cierrePermisos = useQuery({
    queryKey: ['cierre-gerencial-permisos'],
    queryFn: fetchCierreGerencialPermisos,
    staleTime: 5 * 60_000,
  })

  const baseSections = useMemo(() => {
    if (cierrePermisos.data?.puedeConsultar) {
      return INFORMES_HUB_SECTIONS
    }
    return INFORMES_HUB_SECTIONS.map((section) => ({
      ...section,
      links: section.links.filter((link) => link.to !== '/informes/cierre-gerencial'),
    })).filter((section) => section.links.length > 0)
  }, [cierrePermisos.data?.puedeConsultar])

  const sections = useAclHubSections(baseSections)

  return (
    <CredixModuleHubPage
      title="Informes"
      searchable
      searchPlaceholder="Buscar informe…"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Informes' },
      ]}
      sections={sections}
    />
  )
}
