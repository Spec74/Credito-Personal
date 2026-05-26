import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchBovedaAbierta } from '../api/boveda'
import { fetchCreditosPorAprobar } from '../api/creditoAprobar'
import { fetchMarcasGestion } from '../api/maestrosCrud'
import { fetchCatalogoCobertura } from '../api/reportes'
import { fetchUsuariosGestion } from '../api/usuariosAdmin'
import type { CredixStatItem } from '../components/credix'
import { useAuth } from '../auth/useAuth'
import { getLoginProfile } from '../auth/sessionProfile'
import { countHubLinks } from '../utils/credixVisualStats'
import type { CredixHubSection } from '../components/credix/CredixHubGrid'

export type ModuleHubId =
  | 'inicio'
  | 'caja'
  | 'credito'
  | 'ventas'
  | 'almacen'
  | 'informes'
  | 'maestros'
  | 'admin'
  | 'tesoreria'

export function useModuleHubStats(
  moduleId: ModuleHubId,
  moduleLabel: string,
  sections: CredixHubSection[],
) {
  const { session } = useAuth()
  const profile = getLoginProfile()
  const oficinaId = session?.oficinaId ?? 0
  const linkCount = countHubLinks(sections)

  const aprobar = useQuery({
    queryKey: ['hub-stats-aprobar', oficinaId],
    queryFn: () => fetchCreditosPorAprobar({ page: 1, pageSize: 1 }),
    enabled: moduleId === 'credito' && oficinaId > 0,
    staleTime: 60_000,
  })

  const cobertura = useQuery({
    queryKey: ['hub-stats-cobertura'],
    queryFn: fetchCatalogoCobertura,
    enabled: moduleId === 'informes',
    staleTime: 120_000,
  })

  const marcas = useQuery({
    queryKey: ['hub-stats-marcas'],
    queryFn: () => fetchMarcasGestion(true),
    enabled: moduleId === 'maestros',
    staleTime: 120_000,
  })

  const usuarios = useQuery({
    queryKey: ['hub-stats-usuarios'],
    queryFn: () => fetchUsuariosGestion({ page: 1, pageSize: 1, incluirInactivos: true }),
    enabled: moduleId === 'admin',
    staleTime: 120_000,
  })

  const boveda = useQuery({
    queryKey: ['hub-stats-boveda', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: moduleId === 'tesoreria' && oficinaId > 0,
    retry: false,
    staleTime: 60_000,
  })

  return useMemo((): CredixStatItem[] => {
    const base: CredixStatItem[] = [
      { value: moduleLabel, label: 'Módulo' },
      { value: linkCount, label: 'Accesos en pantalla' },
      {
        value: profile.oficinaLabel ?? (oficinaId > 0 ? oficinaId : '—'),
        label: 'Oficina',
      },
      {
        value: profile.nombreUsuario ?? session?.usuarioId ?? '—',
        label: 'Usuario',
      },
    ]

    switch (moduleId) {
      case 'credito':
        return [
          ...base,
          {
            value: aprobar.data?.total ?? (aprobar.isLoading ? '…' : 0),
            label: 'Créditos por aprobar',
            tone: (aprobar.data?.total ?? 0) > 0 ? 'green' : 'default',
          },
          { value: 'Operación', label: 'Área', tone: 'green' },
        ]
      case 'informes':
        return [
          ...base,
          {
            value: cobertura.data?.completoDatosJsonCsvPdf ?? '…',
            label: 'Informes con datos API',
          },
          {
            value: cobertura.data?.totalCatalogo ?? '…',
            label: 'Catálogo total',
          },
        ]
      case 'maestros':
        return [
          ...base,
          {
            value: marcas.data?.length ?? '…',
            label: 'Marcas en catálogo',
          },
          { value: 'Mantenimiento', label: 'Área' },
        ]
      case 'admin':
        return [
          ...base,
          {
            value: usuarios.data?.totalRecords ?? '…',
            label: 'Usuarios registrados',
          },
          { value: 'Seguridad', label: 'Área' },
        ]
      case 'tesoreria':
        return [
          ...base,
          {
            value: boveda.data?.bovedaId ?? (boveda.isError ? '—' : '…'),
            label: 'Bóveda activa',
          },
          {
            value: boveda.data
              ? boveda.data.indCierre
                ? 'Cerrada'
                : 'Abierta'
              : boveda.isError
                ? 'Sin bóveda'
                : '…',
            label: 'Estado bóveda',
            tone: boveda.data && !boveda.data.indCierre ? 'green' : 'default',
          },
        ]
      case 'caja':
        return [
          ...base,
          { value: 'Diaria', label: 'Caja principal' },
          { value: 'Cierres', label: 'Saldos', tone: 'green' },
        ]
      case 'ventas':
        return [
          ...base,
          { value: 'Venta rápida', label: 'Operación principal', tone: 'green' },
          { value: 'OV + LP', label: 'Pedidos y precios' },
        ]
      case 'almacen':
        return [
          ...base,
          { value: 'Entrada / Salida', label: 'Movimientos' },
          { value: 'Stock', label: 'Informes', tone: 'green' },
        ]
      case 'inicio':
      default:
        return [
          {
            value: profile.nombreUsuario ?? session?.usuarioId ?? '—',
            label: 'Usuario',
          },
          {
            value: profile.oficinaLabel ?? session?.oficinaId ?? '—',
            label: 'Oficina',
          },
          { value: session?.roles.length ?? 0, label: 'Roles' },
          { value: linkCount, label: 'Accesos rápidos' },
          { value: 'Activa', label: 'Sesión', tone: 'green' },
        ]
    }
  }, [
    moduleId,
    moduleLabel,
    linkCount,
    profile,
    session,
    oficinaId,
    aprobar.data,
    aprobar.isLoading,
    cobertura.data,
    marcas.data,
    usuarios.data,
    boveda.data,
    boveda.isError,
  ])
}
