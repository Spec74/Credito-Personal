import { Link } from 'react-router-dom'
import { Alert } from 'antd'
import { CredixModuleHubPage } from '../../components/credix'
import { ADMIN_HUB_SECTIONS } from '../../config/adminHubSections'
import { useAclHubSections } from '../../hooks/useAclHubSections'

export function AdminHubPage() {
  const sections = useAclHubSections(ADMIN_HUB_SECTIONS)
  const sinAccesos = sections.length === 0

  return (
    <>
      {sinAccesos ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Sin opciones de administración en su menú"
          description="Su rol no tiene usuarios, roles, oficinas, cajas o comisiones asignadas."
        />
      ) : null}
      <CredixModuleHubPage
        title="Administración"
        breadcrumb={[
          { title: <Link to="/inicio">Inicio</Link> },
          { title: 'Administración' },
        ]}
        sections={sections}
      />
    </>
  )
}
