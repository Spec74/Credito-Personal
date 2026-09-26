import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'
import { useAclHubSections } from '../../hooks/useAclHubSections'

const SECTIONS = [
  {
    title: 'Operaciones',
    links: [
      { to: '/ventas/venta-rapida', label: 'Venta rápida' },
      { to: '/ventas/orden-venta', label: 'Orden de venta' },
      { to: '/ventas/canjear-puntos', label: 'Canjear puntos' },
    ],
  },
  {
    title: 'Catálogo e informes',
    links: [
      { to: '/ventas/lista-precios', label: 'Lista de precios' },
      { to: '/ventas/informe-lista-precios', label: 'Informe lista de precios' },
      { to: '/informes/rentabilidad-venta', label: 'Rentabilidad ventas' },
      { to: '/reportes/venta', label: 'Reportes de venta' },
    ],
  },
]

export function VentasHubPage() {
  const sections = useAclHubSections(SECTIONS)

  return (
    <CredixModuleHubPage
      title="Ventas"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Ventas' },
      ]}
      sections={sections}
    />
  )
}
