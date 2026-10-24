import { Modal, Typography } from 'antd'
import { formatMoney } from '../../utils/formatMoney'

const { Text } = Typography

/** Confirmación de importe irreversible (desembolso, fondeo, cierre). */
export function confirmMoney(options: {
  title: string
  amount: number
  officeLabel?: string
  consequence: string
  okText?: string
  onOk: () => void | Promise<unknown>
}): void {
  Modal.confirm({
    title: options.title,
    okText: options.okText ?? 'Confirmar',
    okButtonProps: { danger: true },
    cancelText: 'Cancelar',
    modalRender: (node) => <div translate="no">{node}</div>,
    content: (
      <div className="credix-money-confirm" translate="no">
        <p className="credix-money-confirm__amount">S/ {formatMoney(options.amount)}</p>
        {options.officeLabel ? (
          <Text type="secondary" style={{ display: 'block' }}>
            {options.officeLabel}
          </Text>
        ) : null}
        <Text>{options.consequence}</Text>
      </div>
    ),
    onOk: options.onOk,
  })
}
