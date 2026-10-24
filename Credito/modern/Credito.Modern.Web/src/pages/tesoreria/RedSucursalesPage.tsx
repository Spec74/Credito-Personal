import { Link, useNavigate } from 'react-router-dom'
import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button } from 'antd'
import { fetchBovedaRedPosicion, type BovedaRedPosicion } from '../../api/boveda'
import { useAuth } from '../../auth/useAuth'
import { CredixPage, CredixDataTable } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'
import { loginSedePath } from '../../utils/sucursalRed'

export function RedSucursalesPage() {
  const { session, logout } = useAuth()
  const navigate = useNavigate()
  const oficinaId = session?.oficinaId ?? 0

  const entrarSede = (destinoId: number) => {
    logout()
    navigate(loginSedePath(destinoId))
  }

  const red = useQuery({
    queryKey: ['boveda-red-posicion', oficinaId],
    queryFn: () => fetchBovedaRedPosicion(oficinaId),
    enabled: oficinaId > 0,
    retry: false,
  })

  const filas = red.data ?? []
  const propia = filas.find((r) => r.oficinaId === oficinaId)
  const efectivoRed = filas.reduce((s, r) => s + (r.efectivoOficina || 0), 0)
  const descuadres = filas.filter((r) => r.bovedaId && Math.abs(r.deltaMedios) > 0.01).length
  const conBoveda = filas.filter((r) => r.bovedaId).length

  const stats = useMemo(
    () =>
      oficinaId < 1
        ? undefined
        : [
            { value: String(conBoveda), label: 'Sedes con bóveda' },
            { value: formatMoney(efectivoRed), label: 'Efectivo de la red' },
            {
              value: String(descuadres),
              label: 'Descuadres',
              tone: descuadres > 0 ? ('red' as const) : ('green' as const),
            },
            {
              value: formatMoney(propia?.saldoFinal ?? 0),
              label: 'Bóveda esta sede',
            },
          ],
    [oficinaId, conBoveda, efectivoRed, descuadres, propia?.saldoFinal],
  )

  return (
    <CredixPage
      title="Red de sucursales"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/tesoreria">Tesorería</Link> },
        { title: 'Red' },
      ]}
      stats={stats}
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
    >
      <CredixDataTable<BovedaRedPosicion>
        rowKey="oficinaId"
        loading={red.isLoading}
        dataSource={filas}
        locale={{ emptyText: 'No hay oficinas activas.' }}
        columns={[
          { title: 'Sede', dataIndex: 'oficina', ellipsis: true },
          {
            title: 'Rol',
            width: 100,
            render: (_, row) => (row.indPrincipal ? 'Central' : 'Sucursal'),
          },
          {
            title: 'Saldo bóveda',
            width: 130,
            align: 'right',
            render: (_, row) => (row.bovedaId ? formatMoney(row.saldoFinal) : '—'),
          },
          {
            title: 'Σ medios',
            width: 120,
            align: 'right',
            render: (_, row) => (row.bovedaId ? formatMoney(row.sumaMedios) : '—'),
          },
          {
            title: 'Delta',
            width: 110,
            align: 'right',
            render: (_, row) => {
              if (!row.bovedaId) return '—'
              const roto = Math.abs(row.deltaMedios) > 0.01
              return (
                <span style={roto ? { color: '#9b1c1c', fontWeight: 600 } : undefined}>
                  {formatMoney(row.deltaMedios)}
                </span>
              )
            },
          },
          {
            title: 'Efectivo oficina',
            width: 140,
            align: 'right',
            render: (_, row) => formatMoney(row.efectivoOficina),
          },
          {
            title: '',
            width: 160,
            render: (_, row) =>
              row.oficinaId === oficinaId ? (
                <Link to="/tesoreria/boveda">Bóveda</Link>
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
