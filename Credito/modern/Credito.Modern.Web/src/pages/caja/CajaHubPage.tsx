import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'

const SECTIONS = [
  {
    title: 'Operativa',
    links: [
      { to: '/caja/diario', label: 'Caja diario', description: 'Cobranzas y arqueo' },
      { to: '/caja/asignar', label: 'Asignar caja' },
      { to: '/caja/verificar-pagos', label: 'Verificar pagos' },
      { to: '/caja/chica', label: 'Caja chica' },
      { to: '/caja/saldos', label: 'Saldos y cierres' },
    ],
  },
  {
    title: 'Informes',
    links: [
      { to: '/informes/cajas-asignadas', label: 'Cajas asignadas' },
      { to: '/informes/caja-diario', label: 'Informe caja diario' },
      {
        to: '/informes/saldo-cartera-caja-diario',
        label: 'Saldo cartera por caja',
      },
      { to: '/informes/movimientos-caja-anulados', label: 'Mov. caja anulados' },
    ],
  },
  {
    title: 'Maestro',
    links: [
      {
        to: '/caja/maestro',
        label: 'Administrar cajas',
        description: 'Oficina, denominación, gestor, activo',
      },
    ],
  },
]

export function CajaHubPage() {
  return (
    <CredixModuleHubPage
      moduleId="caja"
      title="Caja"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Caja' },
      ]}
      intro={
        <>
          Operaciones de caja diaria, chica y cierres — mismo alcance que el menú <strong>Caja</strong>{' '}
          del sistema anterior. Use las tarjetas para abrir cada pantalla.
        </>
      }
      sections={SECTIONS}
    />
  )
}
