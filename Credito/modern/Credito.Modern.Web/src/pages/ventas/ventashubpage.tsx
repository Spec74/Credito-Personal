import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'

const SECTIONS = [
  {
    title: 'Operaciones',
    links: [
      { to: '/ventas/venta-rapida', label: 'Venta rápida', description: 'Operación principal' },
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
    ],
  },
]

export function VentasHubPage() {
  return (
    <CredixModuleHubPage
      moduleId="ventas"
      title="Ventas"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Ventas' },
      ]}
      intro={
        <>
          Lista de precios, venta rápida, órdenes de venta y canje de puntos — alineado a la
          operación del sistema anterior.
        </>
      }
      sections={SECTIONS}
    />
  )
}
