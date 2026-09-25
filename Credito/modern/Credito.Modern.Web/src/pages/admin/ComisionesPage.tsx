import { Link } from 'react-router-dom'
import { PercentageOutlined } from '@ant-design/icons'
import { Space, Typography } from 'antd'
import { CredixCrudPage, type CredixStatItem } from '../../components/credix'

const { Paragraph } = Typography

const comisionesStats: CredixStatItem[] = [
  { value: '0', label: 'Procedimientos usp_*' },
  { value: 'Paridad', label: 'Estado del módulo' },
]

export function ComisionesPage() {
  return (
    <CredixCrudPage
      title="Comisiones"
      subtitle="Misma pantalla que Comision/Index del sistema anterior: no hay cálculo ni liquidación."
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
          <Link to="/mantenimiento/oficinas">Oficinas</Link>
        </Space>
      }
      panelTitle="Estado"
    >
      <div className="credix-module-reserved">
        <span className="credix-module-reserved__icon" aria-hidden>
          <PercentageOutlined />
        </span>
        <Paragraph style={{ marginBottom: 8 }}>
          El MVC solo muestra el título «Comisiones». No hay tabla, procedimiento ni regla de
          negocio en la base. No se inventa un motor de liquidación aquí.
        </Paragraph>
        <Paragraph type="secondary" style={{ marginBottom: 0 }}>
          Los cargos de un crédito se gestionan en la ficha del crédito. Si más adelante el
          negocio define porcentajes, liquidación o informes, se versionan primero en SQL y
          después se cablean en esta pantalla.
        </Paragraph>
      </div>
    </CredixCrudPage>
  )
}
