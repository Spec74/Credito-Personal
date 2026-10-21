import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Button } from 'antd'
import { fetchOficinas } from '../../api/oficinas'
import { fetchBovedaAbierta, fetchBovedasDestinoTransferencia } from '../../api/boveda'
import { fetchDashboardAdminDetalle, fetchDashboardAdminShell } from '../../api/dashboard'
import { useAuth } from '../../auth/useAuth'
import { CredixPage, CredixDataTable } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import { esCreditoAdministrador } from '../../utils/creditoOperacionPermisos'
import { loginSedePath } from '../../utils/sucursalRed'
import type { OficinaListItem } from '../../types/api'

export function RedSucursalesPage() {
  const { session, logout } = useAuth()
  const navigate = useNavigate()
  const oficinaId = session?.oficinaId ?? 0
  const admin = esCreditoAdministrador(session?.roles ?? [])

  const entrarSede = (destinoId: number) => {
    logout()
    navigate(loginSedePath(destinoId))
  }

  const oficinas = useQuery({
    queryKey: ['oficinas'],
    queryFn: fetchOficinas,
  })

  const destinos = useQuery({
    queryKey: ['bovedas-destino-transferencia', oficinaId],
    queryFn: () => fetchBovedasDestinoTransferencia(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const propia = useQuery({
    queryKey: ['boveda-abierta', oficinaId],
    queryFn: () => fetchBovedaAbierta(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const kpis = useQuery({
    queryKey: ['dashboard-admin-shell', oficinaId],
    queryFn: fetchDashboardAdminShell,
    enabled: admin && oficinaId > 0,
    retry: false,
  })

  const detalle = useQuery({
    queryKey: ['dashboard-admin-detalle', oficinaId],
    queryFn: fetchDashboardAdminDetalle,
    enabled: admin && oficinaId > 0 && !!kpis.data,
    retry: false,
  })

  const saldoPorOficina = new Map(
    (destinos.data ?? []).map((d) => [d.oficinaId, d.saldoFinal]),
  )
  if (propia.data) {
    saldoPorOficina.set(oficinaId, propia.data.saldoFinal)
  }

  const r = kpis.data?.resumen
  const cartera = detalle.data?.cartera

  return (
    <CredixPage
      title="Red de sucursales"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/tesoreria">Tesorería</Link> },
        { title: 'Red' },
      ]}
      actions={
        <>
          <Link to="/tesoreria/fondear-sucursales">
            <Button type="primary">Fondear</Button>
          </Link>
          <Link to="/mantenimiento/oficinas/nueva-sucursal">
            <Button>Nueva sucursal</Button>
          </Link>
        </>
      }
      stats={
        r
          ? [
              { value: cartera ? formatMoney(cartera.saldoCartera) : '…', label: 'Cartera' },
              { value: cartera ? formatMoney(cartera.saldoMoraCartera) : '…', label: 'Mora' },
              { value: formatMoney(r.flujoNetoHoy), label: 'Caja hoy' },
              { value: formatMoney(r.desembolsoHoy), label: 'Desembolsos' },
              { value: formatMoney(propia.data?.saldoFinal ?? 0), label: 'Bóveda' },
              { value: formatMoney(r.cobradoHoy), label: 'Cobrado' },
            ]
          : undefined
      }
    >
      <CredixDataTable<OficinaListItem>
        rowKey="oficinaId"
        loading={oficinas.isLoading}
        dataSource={oficinas.data ?? []}
        columns={[
          { title: 'Sede', dataIndex: 'denominacion', ellipsis: true },
          {
            title: 'Rol',
            width: 110,
            render: (_, row) => (row.indPrincipal ? 'Central' : 'Sucursal'),
          },
          {
            title: 'Bóveda',
            width: 140,
            align: 'right',
            render: (_, row) => {
              const saldo = saldoPorOficina.get(row.oficinaId)
              return saldo == null ? '—' : formatMoney(saldo)
            },
          },
          {
            title: '',
            width: 160,
            render: (_, row) =>
              row.oficinaId === oficinaId ? (
                <span>Sede actual</span>
              ) : (
                <Button type="link" size="small" onClick={() => entrarSede(row.oficinaId)}>
                  Entrar en esa sede
                </Button>
              ),
          },
        ]}
      />
    </CredixPage>
  )
}
