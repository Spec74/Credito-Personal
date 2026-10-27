import { Link } from 'react-router-dom'
import { Alert } from 'antd'
import { CredixPage } from '../../components/credix'

/** Gerencia: caja chica no se usa. El menú se retira; esta ruta queda informativa. */
export function CajaChicaPage() {
  return (
    <CredixPage
      title="Caja chica"
      subtitle="Este módulo ya no forma parte de la operación."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: 'Caja chica' },
      ]}
    >
      <Alert
        type="info"
        showIcon
        message="Caja chica está retirada"
        description={
          <>
            Gerencia decidió no usarla. Use <Link to="/caja/diario">caja diario</Link> para
            cobranzas y <Link to="/caja/saldos">saldos</Link> para el cierre. El historial
            queda en base; no se opera desde aquí.
          </>
        }
      />
    </CredixPage>
  )
}
