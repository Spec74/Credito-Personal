import { Link } from 'react-router-dom'
import { Button, Dropdown, Space, Typography } from 'antd'
import type { MenuProps } from 'antd'
import {
  CalendarOutlined,
  DeleteOutlined,
  HistoryOutlined,
  MoreOutlined,
  ReloadOutlined,
  WalletOutlined,
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

/** Acciones primarias del crédito: cobrar primero; ciclo en menú secundario. */
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
  const cicloItems: NonNullable<MenuProps['items']> = [
    ...(puedeAnular
      ? ([
          {
            key: 'anular',
            danger: true,
            icon: <DeleteOutlined />,
            label: 'Anular crédito',
            disabled: oficinaId < 1,
            onClick: onAnular,
          },
        ] satisfies NonNullable<MenuProps['items']>)
      : []),
    ...(puedeProrrogar
      ? ([
          {
            key: 'prorrogar',
            icon: <CalendarOutlined />,
            label: 'Prorrogar',
            disabled: oficinaId < 1,
            onClick: onProrrogar,
          },
        ] satisfies NonNullable<MenuProps['items']>)
      : []),
    ...(puedeReprogramar
      ? ([
          {
            key: 'reprogramar',
            icon: <ReloadOutlined />,
            label: 'Reprogramar',
            disabled: oficinaId < 1,
            onClick: onReprogramar,
          },
        ] satisfies NonNullable<MenuProps['items']>)
      : []),
  ]

  const hayCiclo = cicloItems.length > 0

  if (!puedeCobrar && !puedeMora && !hayCiclo) {
    return null
  }

  return (
    <section className="credito-consulta-acciones" aria-label="Acciones del crédito">
      <Text type="secondary" className="credito-consulta-acciones__label">
        Operaciones
      </Text>
      <Space wrap size={[8, 8]} className="credito-consulta-acciones__btns">
        {puedeCobrar ? (
          <Link to={`/caja/diario?creditoId=${creditoId}`}>
            <Button type="primary" icon={<WalletOutlined />}>
              Cobrar en caja
            </Button>
          </Link>
        ) : null}
        {puedeMora ? (
          <Button icon={<HistoryOutlined />} onClick={onMora}>
            Mora
          </Button>
        ) : null}
        {hayCiclo ? (
          <Dropdown menu={{ items: cicloItems }} trigger={['click']} placement="bottomRight">
            <Button icon={<MoreOutlined />}>Más acciones</Button>
          </Dropdown>
        ) : null}
      </Space>
    </section>
  )
}
