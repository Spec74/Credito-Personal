import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'

const SECTIONS = [
  {
    title: 'Operaciones',
    links: [
      { to: '/almacen/entrada', label: 'Entrada de almacén', description: 'Wizard de ingreso' },
      { to: '/almacen/salida', label: 'Salida de almacén' },
      { to: '/almacen/transferencia', label: 'Transferencia entre almacenes' },
      { to: '/almacen/movimiento', label: 'Movimiento por ID (avanzado)' },
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
  return (
    <CredixModuleHubPage
      moduleId="almacen"
      title="Almacén"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Almacén' },
      ]}
      intro={
        <>
          Indicadores y movimientos de almacén (paridad con <strong>Indicadores de Almacén</strong> y
          reportes de stock del MVC). Operaciones primero; informes al final.
        </>
      }
      sections={SECTIONS}
    />
  )
}
