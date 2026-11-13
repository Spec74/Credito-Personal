import { Link } from 'react-router-dom'
import { Alert } from 'antd'
import { CredixModuleHubPage } from '../../components/credix'
import { useAclHubSections } from '../../hooks/useAclHubSections'

const SECTIONS = [
  {
    title: 'Punto de venta',
    links: [
      {
        to: '/ventas/venta-rapida',
        label: 'Venta rápida',
        description: 'Mostrador · stock de remate',
      },
      {
        to: '/ventas/orden-venta',
        label: 'Orden de venta',
        description: 'Pedidos contado / crédito',
      },
      {
        to: '/ventas/canjear-puntos',
        label: 'Canjear puntos',
        description: 'Fidelidad',
      },
    ],
  },
  {
    title: 'Precios e informes',
    links: [
      {
        to: '/ventas/lista-precios',
        label: 'Lista de precios',
        description: 'Mantenimiento',
      },
      {
        to: '/ventas/informe-lista-precios',
        label: 'Informe lista de precios',
        description: 'Catálogo vigente',
      },
      {
        to: '/informes/rentabilidad-venta',
        label: 'Rentabilidad ventas',
        description: 'Margen por artículo vendido',
      },
      {
        to: '/reportes/venta',
        label: 'Reportes de venta',
        description: 'Exportaciones rápidas',
      },
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
      searchable
      sections={sections}
      intro={
        <Alert
          type="info"
          showIcon
          message="Flujo de giro Credix"
          description={
            <>
              Remate prendario → stock en almacén → <strong>venta rápida</strong> (punto de venta).
              No hay compra a proveedores en este sistema.{' '}
              <Link to="/informes/reporte-stock">Ver stock</Link>
              {' · '}
              <Link to="/maestros/articulos">Artículos</Link>
            </>
          }
        />
      }
    />
  )
}
