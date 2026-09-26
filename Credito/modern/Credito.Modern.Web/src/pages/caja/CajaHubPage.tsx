import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'
import { useAclHubSections } from '../../hooks/useAclHubSections'

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
  const sections = useAclHubSections(SECTIONS)

  return (
    <CredixModuleHubPage
      title="Caja"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Caja' },
      ]}
      sections={sections}
    />
  )
}
