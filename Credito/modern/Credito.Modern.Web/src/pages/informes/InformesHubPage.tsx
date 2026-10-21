import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { CredixModuleHubPage } from '../../components/credix'
import { INFORMES_HUB_SECTIONS } from '../../config/informesHubSections'
import { fetchCierreGerencialPermisos } from '../../api/cierreGerencial'
import { fetchMorosidadPermisos } from '../../api/morosidadEmpresa'
import { useAclHubSections } from '../../hooks/useAclHubSections'
import { readInformesRecientes } from '../../utils/informesRecientes'

export function InformesHubPage() {
  const cierrePermisos = useQuery({
    queryKey: ['cierre-gerencial-permisos'],
    queryFn: fetchCierreGerencialPermisos,
    staleTime: 5 * 60_000,
  })
  const morosidadPermisos = useQuery({
    queryKey: ['morosidad-permisos'],
    queryFn: fetchMorosidadPermisos,
    staleTime: 5 * 60_000,
  })

  const baseSections = useMemo(() => {
    const excluded = new Set<string>()
    if (!cierrePermisos.data?.puedeConsultar) {
      excluded.add('/informes/cierre-gerencial')
    }
    if (!morosidadPermisos.data?.puedeConsultar) {
      excluded.add('/informes/morosos')
    }
    if (excluded.size === 0) {
      return INFORMES_HUB_SECTIONS
    }
    return INFORMES_HUB_SECTIONS.map((section) => ({
      ...section,
      links: section.links.filter((link) => !excluded.has(link.to)),
    })).filter((section) => section.links.length > 0)
  }, [cierrePermisos.data?.puedeConsultar, morosidadPermisos.data?.puedeConsultar])

  const sections = useAclHubSections(baseSections)
  const recientes = readInformesRecientes()

  return (
    <CredixModuleHubPage
      title="Informes"
      searchable
      searchPlaceholder="Buscar informe…"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Informes' },
      ]}
      intro={
        recientes.length > 0 ? (
          <nav className="credix-role-home-strip" aria-label="Informes recientes">
            {recientes.map((r) => (
              <Link key={r.to} to={r.to}>
                {r.label}
              </Link>
            ))}
          </nav>
        ) : undefined
      }
      sections={sections}
    />
  )
}
