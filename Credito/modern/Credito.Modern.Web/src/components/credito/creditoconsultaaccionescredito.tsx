import { Link } from 'react-router-dom'
import { Button, Space, Typography } from 'antd'
import {
  CalendarOutlined,
  DeleteOutlined,
  HistoryOutlined,
  ReloadOutlined,
} from '@ant-design/icons'

const { Text } = Typography

type Props = {
  creditoId: number
  oficinaId: number
  puedeCobrar: boolean
  puedeMora: boolean
  puedeAnular: boolean
  puedeProrrogar: boolean
  puedeReprogramar: boolean
  onAnular: () => void
  onProrrogar: () => void
  onReprogramar: () => void
  onMora: () => void
}

/** Paridad `.actions` derecha/izquierda en detalle de crédito (Creditos.cshtml). */
export function CreditoConsultaAccionesCredito({
  creditoId,
  oficinaId,
  puedeCobrar,
  puedeMora,
  puedeAnular,
  puedeProrrogar,
  puedeReprogramar,
  onAnular,
  onProrrogar,
  onReprogramar,
  onMora,
}: Props) {
  return (
    <section className="credito-consulta-acciones" aria-label="Acciones del crédito">
      <div className="credito-consulta-acciones__col credito-consulta-acciones__col--left">
        <Text type="secondary" className="credito-consulta-acciones__label">
          Cobro y mora
        </Text>
        <Space wrap>
          {puedeCobrar ? (
            <Link to={`/caja/diario?creditoId=${creditoId}`}>
              <Button type="primary">Cobrar en caja</Button>
            </Link>
          ) : null}
          {puedeMora ? (
            <Button icon={<HistoryOutlined />} onClick={onMora}>
              Crédito mora
            </Button>
          ) : null}
        </Space>
      </div>
      <div className="credito-consulta-acciones__col credito-consulta-acciones__col--right">
        <Text type="secondary" className="credito-consulta-acciones__label">
          Ciclo del crédito
        </Text>
        <Space wrap>
          {puedeAnular ? (
            <Button
              danger
              icon={<DeleteOutlined />}
              onClick={onAnular}
              disabled={oficinaId < 1}
            >
              Anular
            </Button>
          ) : null}
          {puedeProrrogar ? (
            <Button
              icon={<CalendarOutlined />}
              onClick={onProrrogar}
              disabled={oficinaId < 1}
            >
              Prorrogar
            </Button>
          ) : null}
          {puedeReprogramar ? (
            <Button
              icon={<ReloadOutlined />}
              onClick={onReprogramar}
              disabled={oficinaId < 1}
            >
              Reprogramar
            </Button>
          ) : null}
        </Space>
      </div>
    </section>
  )
}
