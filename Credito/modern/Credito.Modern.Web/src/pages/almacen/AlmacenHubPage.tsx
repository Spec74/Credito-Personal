import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'
import { useAclHubSections } from '../../hooks/useAclHubSections'

const SECTIONS = [
  {
    title: 'Operaciones',
    links: [
      { to: '/almacen/entrada', label: 'Entrada de almacén' },
      { to: '/almacen/salida', label: 'Salida de almacén' },
      { to: '/almacen/transferencia', label: 'Transferencia entre almacenes' },
      { to: '/almacen/movimiento', label: 'Movimiento por ID' },
    ],
  },
  {
    title: 'Informes y utilidades',
    links: [
      { to: '/informes/reporte-stock', label: 'Stock por almacén' },
      { to: '/informes/stock-anulados', label: 'Stock anulados' },
      { to: '/almacen/codigo-barras', label: 'Códigos de barras' },
      { to: '/almacen/constancia', label: 'Constancia de movimiento' },
      { to: '/almacen/kardex', label: 'Kardex de artículo' },
    ],
  },
]

export function AlmacenHubPage() {
  const sections = useAclHubSections(SECTIONS)

  return (
    <CredixModuleHubPage
      title="Almacén"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Almacén' },
      ]}
      searchable
      sections={sections}
    />
  )
}
