import { Link } from 'react-router-dom'
import { Alert } from 'antd'
import { CredixModuleHubPage } from '../../components/credix'
import { useAclHubSections } from '../../hooks/useAclHubSections'

const SECTIONS = [
  {
    title: 'Consulta de inventario',
    links: [
      {
        to: '/almacen/kardex',
        label: 'Kardex de artículo',
        description: 'Movimientos por serie',
      },
      {
        to: '/informes/reporte-stock',
        label: 'Stock por almacén',
        description: 'Series EN_ALMACEN',
      },
      {
        to: '/informes/stock-anulados',
        label: 'Stock anulados',
        description: 'Series dadas de baja',
      },
      {
        to: '/almacen/codigo-barras',
        label: 'Códigos de barras',
        description: 'Listado para etiquetas',
      },
    ],
  },
  {
    title: 'Operaciones',
    links: [
      {
        to: '/almacen/salida',
        label: 'Salida de almacén',
        description: 'Egreso manual de series',
      },
      {
        to: '/almacen/transferencia',
        label: 'Transferencia',
        description: 'Entre almacenes de la red',
      },
      {
        to: '/almacen/constancia',
        label: 'Constancia',
        description: 'Comprobante de movimiento',
      },
      {
        to: '/almacen/movimiento',
        label: 'Movimiento por ID',
        description: 'Avanzado / soporte',
      },
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
      intro={
        <Alert
          type="info"
          showIcon
          message="Sin entrada por compra"
          description={
            <>
              El ingreso de mercadería de proveedores no opera en Credix. El stock de giro nace al
              rematar y se liquida en <Link to="/ventas/venta-rapida">venta rápida</Link>. Use
              kardex y stock para controlar existencias.
            </>
          }
        />
      }
    />
  )
}
