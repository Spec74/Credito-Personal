import type { ReactNode } from 'react'
import { Modal, Typography } from 'antd'
import { formatMoney } from '../../utils/formatMoney'
import { credixAppModal } from './CredixStaticDialogHost'

const { Text } = Typography

const WRAP = 'credix-modal credix-modal--confirm credix-modal-operativo'

/** Confirmación de importe irreversible (desembolso, remate, fondeo, cierre). */
export function confirmMoney(options: {
  title: string
  amount: number
  officeLabel?: string
  consequence: string
  okText?: string
  onOk: () => void | Promise<unknown>
}): void {
  const api = credixAppModal()
  const confirm = api ? api.confirm.bind(api) : Modal.confirm.bind(Modal)
  confirm({
    title: options.title,
    okText: options.okText ?? 'Confirmar',
    okButtonProps: { danger: true },
    cancelText: 'Cancelar',
    centered: true,
    className: 'credix-money-confirm-modal',
    wrapClassName: WRAP,
    rootClassName: WRAP,
    modalRender: (node: ReactNode) => <div translate="no">{node}</div>,
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
