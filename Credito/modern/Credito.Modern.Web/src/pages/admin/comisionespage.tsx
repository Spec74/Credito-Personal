import { Link } from 'react-router-dom'
import { PercentageOutlined } from '@ant-design/icons'
import { Space, Typography } from 'antd'
import { CredixCrudPage, type CredixStatItem } from '../../components/credix'

const { Paragraph } = Typography

const comisionesStats: CredixStatItem[] = [
  { value: '—', label: 'Reglas activas' },
  { value: 'Reservado', label: 'Estado del módulo' },
]

export function ComisionesPage() {
  return (
    <CredixCrudPage
      title="Comisiones"
      subtitle="Espacio reservado para liquidación y reportes de comisiones cuando el negocio lo defina."
      stats={comisionesStats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/admin">Administración</Link> },
        { title: 'Comisiones' },
      ]}
      actions={
        <Space wrap size="small">
          <Link to="/admin/usuarios">Usuarios</Link>
          <Link to="/admin/roles">Roles</Link>
          <Link to="/admin/oficinas">Oficinas</Link>
        </Space>
      }
      panelTitle="Estado"
    >
      <div className="credix-module-reserved">
        <span className="credix-module-reserved__icon" aria-hidden>
          <PercentageOutlined />
        </span>
        <Paragraph style={{ marginBottom: 8 }}>
          En el sistema anterior no existía lógica operativa de comisiones en esta pantalla.
          Cuando se formalice el cálculo, liquidación o informes, se implementará aquí con el
          patrón strangler: API dedicada + UI Credix responsiva.
        </Paragraph>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Mientras tanto, use Usuarios y Roles para permisos, e Informes para reportes de
          cartera y ventas relacionados.
        </Paragraph>
      </div>
    </CredixCrudPage>
  )
}
