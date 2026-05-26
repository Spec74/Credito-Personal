import { Link } from 'react-router-dom'
import { CredixModuleHubPage } from '../../components/credix'

const SECTIONS = [
  {
    title: 'Operaciones',
    links: [
      { to: '/tesoreria/boveda', label: 'Bóveda' },
      { to: '/tesoreria/movimiento-boveda', label: 'Informe movimiento bóveda' },
    ],
  },
  {
    title: 'Relacionado',
    links: [
      { to: '/caja', label: 'Módulo caja' },
      { to: '/informes/saldo-cartera-caja-diario', label: 'Saldo cartera caja diario' },
    ],
  },
]

export function TesoreriaHubPage() {
  return (
    <CredixModuleHubPage
      moduleId="tesoreria"
      title="Tesorería"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Tesorería' },
      ]}
      intro={<>Bóveda operativa y movimientos. Exportación de informes vía API.</>}
      sections={SECTIONS}
    />
  )
}
