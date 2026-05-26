import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'
import { INFORMES_HUB_QUICK_ACCESS, INFORMES_HUB_SECTIONS } from '../../config/informesHubSections'

export function InformesHubPage() {
  return (
    <CredixModuleHubPage
      moduleId="informes"
      title="Informes"
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
      sections={INFORMES_HUB_SECTIONS}
    />
  )
}
