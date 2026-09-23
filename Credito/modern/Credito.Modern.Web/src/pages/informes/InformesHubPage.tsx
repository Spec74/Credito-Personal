import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { CredixModuleHubPage } from '../../components/credix'
import { INFORMES_HUB_QUICK_ACCESS, INFORMES_HUB_SECTIONS } from '../../config/informesHubSections'
import { fetchCierreGerencialPermisos } from '../../api/cierreGerencial'

export function InformesHubPage() {
  const cierrePermisos = useQuery({
    queryKey: ['cierre-gerencial-permisos'],
    queryFn: fetchCierreGerencialPermisos,
    staleTime: 5 * 60_000,
  })

  const sections = useMemo(() => {
    if (cierrePermisos.data?.puedeConsultar) {
      return INFORMES_HUB_SECTIONS
    }
    return INFORMES_HUB_SECTIONS.map((section) => ({
      ...section,
      links: section.links.filter((link) => link.to !== '/informes/cierre-gerencial'),
    })).filter((section) => section.links.length > 0)
  }, [cierrePermisos.data?.puedeConsultar])

  return (
    <CredixModuleHubPage
      moduleId="informes"
      title="Informes"
      searchable
      searchPlaceholder="Buscar informe (cobro, mora, caja, cliente…)"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Informes' },
      ]}
      intro={
        <>
          Misma cobertura que <strong>Reportes → Crédito</strong> del sistema anterior: filtros,
          tabla con búsqueda y exportación <strong>Excel (CSV)</strong> / <strong>PDF</strong>. Las
          fechas de operación muestran hora cuando aplica. Índice con cajas:{' '}
          <Link to="/reportes/credito">Reportes de crédito</Link>. Matriz técnica:{' '}
          <Link to="/informes/cobertura">cobertura MVC vs API</Link>.
        </>
      }
      quickAccess={INFORMES_HUB_QUICK_ACCESS}
      sections={sections}
    />
  )
}
