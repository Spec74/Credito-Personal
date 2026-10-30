import { Link } from 'react-router-dom'
import { Alert } from 'antd'
import { CredixPage } from '../../components/credix'

/** Gerencia: compra a proveedores no opera en Credix. El stock de giro entra por remate. */
export function EntradaAlmacenPage() {
  return (
    <CredixPage
      title="Entrada de almacén"
      subtitle="Este módulo ya no forma parte de la operación de crédito."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/almacen">Almacén</Link> },
        { title: 'Entrada' },
      ]}
    >
      <Alert
        type="info"
        showIcon
        message="La compra a proveedores no va en Credix"
        description={
          <>
            Gerencia: las compras de mercadería se operan en <strong>ventas</strong>, no aquí. En
            esta casa el stock de giro entra al rematar una prenda y se vende en{' '}
            <Link to="/ventas/venta-rapida">venta rápida</Link>. Consulte existencias en{' '}
            <Link to="/informes/reporte-stock">stock</Link> o{' '}
            <Link to="/almacen/kardex">kardex</Link>. Las tablas de movimiento se conservan porque
            la salida por venta las usa.
          </>
        }
      />
    </CredixPage>
  )
}
