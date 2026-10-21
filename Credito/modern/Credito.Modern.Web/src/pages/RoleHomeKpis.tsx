import { useQuery } from '@tanstack/react-query'
import { fetchDashboardAdminShell } from '../api/dashboard'
import { useAuth } from '../auth/useAuth'
import { CredixStatsBar } from '../components/credix'
import { formatMoney } from '../utils/formatMoney'
import { esCreditoAdministrador, esCreditoEncargado } from '../utils/creditoOperacionPermisos'

export function RoleHomeKpis() {
  const { session } = useAuth()
  const enabled =
    (session?.oficinaId ?? 0) > 0 &&
    (esCreditoAdministrador(session?.roles ?? []) || esCreditoEncargado(session?.roles ?? []))

  const q = useQuery({
    queryKey: ['dashboard-admin-shell', session?.oficinaId],
    queryFn: fetchDashboardAdminShell,
    enabled,
    retry: false,
    staleTime: 60_000,
  })

  const r = q.data?.resumen
  if (!r) return null

  return (
    <CredixStatsBar
      items={[
        { value: formatMoney(r.cobradoHoy), label: 'Cobrado' },
        { value: formatMoney(r.desembolsoHoy), label: 'Desembolsos' },
        { value: formatMoney(r.saldoCartera), label: 'Cartera' },
        { value: formatMoney(r.saldoMoraCartera), label: 'Mora' },
        { value: formatMoney(r.flujoNetoHoy), label: 'Caja' },
        { value: String(r.creditosHoy), label: 'Créditos hoy' },
      ]}
    />
  )
}
