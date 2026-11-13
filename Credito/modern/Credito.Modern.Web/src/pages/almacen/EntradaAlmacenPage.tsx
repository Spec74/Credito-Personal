import { Link } from 'react-router-dom'
import { Alert, Button, Space } from 'antd'
import { CredixPage } from '../../components/credix'

/**
 * Compra a proveedores quedó fuera de Credix (gerencia).
 * Stock de giro = remate prendario → serie EN_ALMACEN → venta rápida.
 */
export function EntradaAlmacenPage() {
  return (
    <CredixPage
      className="almacen-ops"
      title="Entrada de almacén"
      subtitle="Módulo retirado · no opera en Credix"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/almacen">Almacén</Link> },
        { title: 'Entrada' },
      ]}
    >
      <Alert
        type="warning"
        showIcon
        message="La compra a proveedores no forma parte de Credix"
        description={
          <>
            No registre mercadería de terceros aquí. El inventario de giro entra al{' '}
            <strong>rematar una prenda</strong> (artículo PRM + serie en almacén) y se vende en el{' '}
            <strong>punto de venta</strong> (venta rápida). Consulte existencias en stock o kardex.
          </>
        }
        action={
          <Space direction="vertical" size="small">
            <Link to="/ventas/venta-rapida">
              <Button type="primary" size="small">
                Ir al punto de venta
              </Button>
            </Link>
            <Link to="/informes/reporte-stock">
              <Button size="small">Ver stock</Button>
            </Link>
            <Link to="/almacen/kardex">
              <Button size="small">Kardex</Button>
            </Link>
            <Link to="/credito/prendario">
              <Button size="small">Cartera prendaria</Button>
            </Link>
          </Space>
        }
      />
    </CredixPage>
  )
}
