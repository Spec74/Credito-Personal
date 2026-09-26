import { Link } from 'react-router-dom'
import { PercentageOutlined } from '@ant-design/icons'
import { Typography } from 'antd'
import { CredixCrudPage } from '../../components/credix'

const { Paragraph } = Typography

export function ComisionesPage() {
  return (
    <CredixCrudPage
      title="Comisiones"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/admin">Administración</Link> },
        { title: 'Comisiones' },
      ]}
      panelTitle="Comisiones"
    >
      <div className="credix-module-reserved">
        <span className="credix-module-reserved__icon" aria-hidden>
          <PercentageOutlined />
        </span>
        <Paragraph style={{ marginBottom: 0 }}>
          Módulo de comisiones sin operaciones configuradas en el menú actual.
        </Paragraph>
      </div>
    </CredixCrudPage>
  )
}
