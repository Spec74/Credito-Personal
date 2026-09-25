import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Alert } from 'antd'
import { CredixModuleHubPage } from '../../components/credix'
import {
  ADMIN_HUB_QUICK_ACCESS,
  ADMIN_HUB_SECTIONS,
} from '../../config/adminHubSections'
import { fetchMenu } from '../../api/menu'
import { useAuth } from '../../auth/useAuth'
import { hasMenuRouteAccess } from '../../utils/menuRouteAccess'

export function AdminHubPage() {
  const { session } = useAuth()
  const menuQuery = useQuery({
    queryKey: ['menu', session?.oficinaId, session?.usuarioId],
    queryFn: fetchMenu,
    enabled: (session?.oficinaId ?? 0) > 0 && (session?.usuarioId ?? 0) > 0,
    staleTime: 5 * 60_000,
  })

  const menu = menuQuery.data ?? []

  const sections = useMemo(
    () =>
      ADMIN_HUB_SECTIONS.map((section) => ({
        ...section,
        links: section.links.filter((link) => hasMenuRouteAccess(link.to, menu)),
      })).filter((section) => section.links.length > 0),
    [menu],
  )

  const quickAccess = useMemo(
    () => ADMIN_HUB_QUICK_ACCESS.filter((link) => hasMenuRouteAccess(link.to, menu)),
    [menu],
  )

  const sinAccesos = !menuQuery.isLoading && sections.length === 0

  return (
    <>
      {sinAccesos ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Sin opciones de administración en su menú"
          description="Su rol no tiene usuarios, roles, oficinas, cajas o comisiones asignadas. Solicite acceso al administrador."
        />
      ) : null}
      <CredixModuleHubPage
        moduleId="admin"
        title="Administración"
        searchable
        searchPlaceholder="Buscar oficina, usuario, rol, comisión…"
        breadcrumb={[
          { title: <Link to="/inicio">Inicio</Link> },
          { title: 'Administración' },
        ]}
        intro={
          <>
            Centro de <strong>mantenimiento</strong> y <strong>seguridad</strong>: oficinas, cajas,
            usuarios, roles y comisiones. Solo se muestran las pantallas habilitadas en su menú
            (misma regla que el sistema anterior).
          </>
        }
        quickAccess={quickAccess}
        sections={sections}
      />
    </>
  )
}
